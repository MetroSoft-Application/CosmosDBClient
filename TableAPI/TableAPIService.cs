using System.Data;
using System.Diagnostics;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Net.Http;

namespace CosmosDBClient.TableAPI
{
    /// <summary>
    /// Azure Cosmos DB Table API操作のためのサービスクラス
    /// </summary>
    public class TableAPIService
    {
        /// <summary>
        /// CloudTableClientインスタンス
        /// </summary>
        public CloudTableClient TableClient { get; private set; }

        /// <summary>
        /// CloudTableインスタンス
        /// </summary>
        public CloudTable Table { get; private set; }

        /// <summary>
        /// システム列のリスト
        /// </summary>
        public readonly string[] systemColumns = { "PartitionKey", "RowKey", "Timestamp", "ETag" };

        /// <summary>
        /// 接続文字列（REST API用）
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// アカウント名
        /// </summary>
        private string _accountName;

        /// <summary>
        /// アカウントキー
        /// </summary>
        private string _accountKey;

        /// <summary>
        /// コンストラクタ - 接続文字列とテーブル名でサービスを初期化
        /// </summary>
        /// <param name="connectionString">ストレージアカウントの接続文字列</param>
        /// <param name="tableName">テーブル名</param>
        public TableAPIService(string connectionString, string tableName)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string is not set", nameof(connectionString));
            }

            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("Table name is not set", nameof(tableName));
            }
            // 接続文字列を保存
            _connectionString = connectionString;

            // 接続文字列からアカウント名とキーを抽出
            ParseConnectionString(connectionString);

            // ストレージアカウントへの接続
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            TableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            Table = TableClient.GetTableReference(tableName);
        }

        /// <summary>
        /// 接続テスト - 指定された接続文字列とテーブル名で接続をテストする
        /// </summary>
        /// <param name="connectionString">ストレージアカウントの接続文字列</param>
        /// <param name="tableName">テーブル名</param>
        /// <returns>接続テストの結果とメッセージ</returns>
        public static async Task<(bool Success, string Message)> TestConnectionAsync(string connectionString, string tableName)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    return (false, "Connection string is not set");
                }

                if (string.IsNullOrEmpty(tableName))
                {
                    return (false, "Table name is not set");
                }

                // ストレージアカウントへの接続
                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
                var table = tableClient.GetTableReference(tableName);

                // テーブルの存在確認
                bool exists = await table.ExistsAsync();

                if (exists)
                {
                    // 単純なクエリを実行してテスト
                    TableQuery<DynamicTableEntity> query = new TableQuery<DynamicTableEntity>().Take(1);
                    var result = await table.ExecuteQuerySegmentedAsync(query, null); return (true, $"Connection successful. Table '{tableName}' exists.");
                }
                else
                {
                    return (true, $"Connection successful, but table '{tableName}' does not exist. Table creation is required.");
                }
            }
            catch (StorageException ex)
            {
                return (false, $"Storage exception occurred: {ex.Message}\nStatus code: {ex.RequestInformation?.HttpStatusCode}\nDetails: {ex.RequestInformation?.ExtendedErrorInformation?.ErrorMessage}");
            }
            catch (Exception ex)
            {
                return (false, $"Exception occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// アカウント内のすべてのテーブル名を取得する
        /// </summary>
        /// <returns>テーブル名のリスト</returns>
        public async Task<List<string>> GetTableNamesAsync()
        {
            var tableNames = new List<string>();
            TableContinuationToken continuationToken = null;

            do
            {
                var tableSegment = await TableClient.ListTablesSegmentedAsync(continuationToken);
                continuationToken = tableSegment.ContinuationToken;

                foreach (var table in tableSegment.Results)
                {
                    tableNames.Add(table.Name);
                }
            }
            while (continuationToken != null);

            return tableNames;
        }

        /// <summary>
        /// テーブルの存在を確認する
        /// </summary>
        /// <returns>テーブルが存在する場合はtrue、存在しない場合はfalse</returns>
        public async Task<bool> TableExistsAsync()
        {
            return await Table.ExistsAsync();
        }

        /// <summary>
        /// テーブルのプロパティを取得する（Cosmos DBのTable APIでは詳細なプロパティ取得が制限されている）
        /// </summary>
        /// <returns>テーブルの基本情報を含むオブジェクト</returns>
        public async Task<dynamic> GetTablePropertiesAsync()
        {
            var exists = await TableExistsAsync();

            // テーブルが存在しない場合
            if (!exists)
            {
                return new
                {
                    Name = Table.Name,
                    Exists = false
                };
            }

            // テーブル情報を返す
            // Table APIでは詳細なプロパティの取得が制限されているため、基本情報のみ
            return new
            {
                Name = Table.Name,
                Uri = Table.Uri.ToString(),
                Exists = true
            };
        }

        /// <summary>
        /// クエリを実行し、ステータス情報を含む結果を取得する
        /// </summary>
        /// <param name="query">実行するクエリ（ODATA形式）</param>
        /// <param name="maxItemCount">取得する最大項目数</param>
        /// <returns>クエリ結果とステータス情報</returns>
        public async Task<FetchDataResult> FetchDataWithStatusAsync(string query, int maxItemCount = 1000)
        {
            var result = new FetchDataResult();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var pageCount = 0;
            var documentCount = 0;
            var totalRequestCharge = 0.0;

            try
            {
                // クエリの構築
                TableQuery<DynamicTableEntity> tableQuery;

                if (string.IsNullOrWhiteSpace(query))
                {
                    // デフォルトクエリ - すべてのエンティティを取得
                    tableQuery = new TableQuery<DynamicTableEntity>();
                }
                else
                {
                    // ODataフィルター式を使用してクエリを構築
                    tableQuery = new TableQuery<DynamicTableEntity>().Where(query);
                }

                // maxItemCountが設定されている場合は、取得数を制限
                if (maxItemCount > 0)
                {
                    tableQuery.TakeCount = maxItemCount;
                }

                // データテーブルの準備
                var dataTable = new DataTable();
                TableContinuationToken continuationToken = null;
                bool isFirstBatch = true;

                do
                {
                    // 1バッチ分のエンティティを取得
                    var queryResult = await Table.ExecuteQuerySegmentedAsync(tableQuery, continuationToken);
                    pageCount++;
                    continuationToken = queryResult.ContinuationToken;

                    // requestChargeは常にnull（Table APIでは提供されない）
                    // Table APIではRequest Unitの概念がないため、Cosmos DB固有の機能であるRequestChargeは使用できない
                    // 代わりにトランザクションカウントが課金単位となる

                    foreach (var entity in queryResult.Results)
                    {
                        documentCount++;

                        if (isFirstBatch && dataTable.Columns.Count == 0)
                        {
                            // システム列を追加
                            dataTable.Columns.Add("PartitionKey", typeof(string));
                            dataTable.Columns.Add("RowKey", typeof(string));
                            dataTable.Columns.Add("Timestamp", typeof(DateTimeOffset));
                            dataTable.Columns.Add("ETag", typeof(string));

                            // 動的プロパティのカラムを追加
                            foreach (var property in entity.Properties)
                            {
                                // まだ列が存在しなければ追加
                                if (!dataTable.Columns.Contains(property.Key))
                                {
                                    Type propertyType = GetClrType(property.Value.PropertyType);
                                    dataTable.Columns.Add(property.Key, propertyType);
                                }
                            }
                        }

                        // データ行を追加
                        var row = dataTable.NewRow();

                        // システム列の値をセット
                        row["PartitionKey"] = entity.PartitionKey;
                        row["RowKey"] = entity.RowKey;
                        row["Timestamp"] = entity.Timestamp;
                        row["ETag"] = entity.ETag;

                        // 動的プロパティの値をセット
                        foreach (var property in entity.Properties)
                        {
                            // 列が存在しなければ追加
                            if (!dataTable.Columns.Contains(property.Key))
                            {
                                Type propertyType = GetClrType(property.Value.PropertyType);
                                dataTable.Columns.Add(property.Key, propertyType);
                            }

                            // プロパティタイプに基づいて値をセット
                            row[property.Key] = GetEntityPropertyValue(property.Value);
                        }

                        dataTable.Rows.Add(row);
                    }

                    isFirstBatch = false;

                    // 指定した最大数に達した場合はループを終了
                    if (documentCount >= maxItemCount)
                    {
                        break;
                    }
                }
                while (continuationToken != null); result.Data = dataTable;
                result.DocumentCount = documentCount;
                result.PageCount = pageCount;
                result.TotalRequestCharge = totalRequestCharge;
            }
            catch (StorageException ex) when (ex.RequestInformation?.HttpStatusCode == 400)
            {
                // クエリ構文エラーや不正なフィルター式の場合
                throw new Exception($"OData query syntax error: {ex.Message}\nDetails: {ex.RequestInformation?.ExtendedErrorInformation?.ErrorMessage ?? "Unknown error"}", ex);
            }
            catch (StorageException ex)
            {
                // その他のStorage例外
                throw new Exception($"Table API error: {ex.Message}\nStatus code: {ex.RequestInformation?.HttpStatusCode}\nDetails: {ex.RequestInformation?.ExtendedErrorInformation?.ErrorMessage ?? "Unknown error"}", ex);
            }
            catch (Exception ex)
            {
                // その他の一般的な例外
                throw new Exception($"Error occurred while retrieving table data: {ex.Message}", ex);
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// EntityPropertyの値を.NET型に変換する
        /// </summary>
        /// <param name="property">変換するEntityProperty</param>
        /// <returns>変換された.NET型のオブジェクト</returns>
        private object GetEntityPropertyValue(EntityProperty property)
        {
            if (property == null)
                return DBNull.Value;

            switch (property.PropertyType)
            {
                case EdmType.String:
                    return property.StringValue != null ? property.StringValue : DBNull.Value;
                case EdmType.Binary:
                    return property.BinaryValue != null ? property.BinaryValue : DBNull.Value;
                case EdmType.Boolean:
                    return property.BooleanValue.HasValue ? property.BooleanValue.Value : DBNull.Value;
                case EdmType.DateTime:
                    return property.DateTimeOffsetValue.HasValue ? property.DateTimeOffsetValue.Value : DBNull.Value;
                case EdmType.Double:
                    return property.DoubleValue.HasValue ? property.DoubleValue.Value : DBNull.Value;
                case EdmType.Guid:
                    return property.GuidValue.HasValue ? property.GuidValue.Value : DBNull.Value;
                case EdmType.Int32:
                    return property.Int32Value.HasValue ? property.Int32Value.Value : DBNull.Value;
                case EdmType.Int64:
                    return property.Int64Value.HasValue ? property.Int64Value.Value : DBNull.Value;
                default:
                    return DBNull.Value;
            }
        }

        /// <summary>
        /// EdmTypeからCLR型に変換する
        /// </summary>
        /// <param name="edmType">変換するEdmType</param>
        /// <returns>対応するCLR型</returns>
        private Type GetClrType(EdmType edmType)
        {
            switch (edmType)
            {
                case EdmType.String:
                    return typeof(string);
                case EdmType.Binary:
                    return typeof(byte[]);
                case EdmType.Boolean:
                    return typeof(bool);
                case EdmType.DateTime:
                    return typeof(DateTimeOffset);
                case EdmType.Double:
                    return typeof(double);
                case EdmType.Guid:
                    return typeof(Guid);
                case EdmType.Int32:
                    return typeof(int);
                case EdmType.Int64:
                    return typeof(long);
                default:
                    return typeof(string);
            }
        }

        /// <summary>
        /// JSONオブジェクトからテーブルエンティティを作成する
        /// </summary>
        /// <param name="jsonObject">JSONオブジェクト</param>
        /// <returns>作成されたテーブルエンティティ</returns>
        public DynamicTableEntity CreateEntityFromJson(JObject jsonObject)
        {
            if (jsonObject == null)
                throw new ArgumentNullException(nameof(jsonObject));
            if (!jsonObject.ContainsKey("PartitionKey") || !jsonObject.ContainsKey("RowKey"))
                throw new ArgumentException("JSON object must contain PartitionKey and RowKey");

            var partitionKey = jsonObject["PartitionKey"].ToString();
            var rowKey = jsonObject["RowKey"].ToString();

            // DynamicTableEntityの作成
            var entity = new DynamicTableEntity(partitionKey, rowKey);

            // 他のプロパティを追加
            foreach (var property in jsonObject.Properties())
            {
                var propertyName = property.Name;

                // システム列はスキップ
                if (systemColumns.Contains(propertyName))
                    continue;

                // プロパティ値に基づいてEntityPropertyを作成
                entity.Properties[propertyName] = CreateEntityProperty(property.Value);
            }

            return entity;
        }

        /// <summary>
        /// JSONトークンからEntityPropertyを作成する
        /// </summary>
        /// <param name="token">JSONトークン</param>
        /// <returns>作成されたEntityProperty</returns>
        private EntityProperty CreateEntityProperty(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return EntityProperty.CreateEntityPropertyFromObject(null);

            switch (token.Type)
            {
                case JTokenType.String:
                    return EntityProperty.GeneratePropertyForString(token.Value<string>());

                case JTokenType.Integer:
                    // Int32かInt64かを確認
                    var intValue = token.Value<long>();
                    if (intValue >= int.MinValue && intValue <= int.MaxValue)
                        return EntityProperty.GeneratePropertyForInt(token.Value<int>());
                    else
                        return EntityProperty.GeneratePropertyForLong(intValue);

                case JTokenType.Float:
                    return EntityProperty.GeneratePropertyForDouble(token.Value<double>());

                case JTokenType.Boolean:
                    return EntityProperty.GeneratePropertyForBool(token.Value<bool>());

                case JTokenType.Date:
                    return EntityProperty.GeneratePropertyForDateTimeOffset(token.Value<DateTimeOffset>());

                case JTokenType.Guid:
                    return EntityProperty.GeneratePropertyForGuid(token.Value<Guid>());

                case JTokenType.Object:
                case JTokenType.Array:
                    // オブジェクトや配列はJSON文字列として保存
                    return EntityProperty.GeneratePropertyForString(token.ToString(Formatting.None));

                default:
                    // その他のタイプはすべて文字列として扱う
                    return EntityProperty.GeneratePropertyForString(token.ToString());
            }
        }

        /// <summary>
        /// エンティティを挿入または更新する
        /// </summary>
        /// <param name="jsonObject">挿入または更新するJSONデータ</param>
        /// <returns>操作の結果</returns>
        public async Task<TableResult> UpsertEntityAsync(JObject jsonObject)
        {
            var entity = CreateEntityFromJson(jsonObject);
            var operation = TableOperation.InsertOrReplace(entity);
            return await Table.ExecuteAsync(operation);
        }

        /// <summary>
        /// エンティティを挿入または更新する
        /// </summary>
        /// <param name="entity">挿入/更新するエンティティ</param>
        /// <returns>操作の成功を示すTask</returns>
        public async Task InsertOrReplaceEntityAsync(DynamicTableEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity is null");
            }

            if (string.IsNullOrEmpty(entity.PartitionKey))
            {
                throw new ArgumentException("Partition key is not specified", nameof(entity));
            }

            if (string.IsNullOrEmpty(entity.RowKey))
            {
                throw new ArgumentException("Row key is not specified", nameof(entity));
            }

            // 挿入または更新操作の作成
            var operation = TableOperation.InsertOrReplace(entity);

            // 操作の実行
            await Table.ExecuteAsync(operation);
        }

        /// <summary>
        /// エンティティを削除する
        /// </summary>
        /// <param name="partitionKey">パーティションキー</param>
        /// <param name="rowKey">行キー</param>
        /// <returns>操作の成功を示すTask</returns>
        public async Task DeleteEntityAsync(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey))
            {
                throw new ArgumentException("Partition key is not specified", nameof(partitionKey));
            }

            if (string.IsNullOrEmpty(rowKey))
            {
                throw new ArgumentException("Row key is not specified", nameof(rowKey));
            }

            // エンティティを取得
            var operation = TableOperation.Retrieve<DynamicTableEntity>(partitionKey, rowKey);
            var result = await Table.ExecuteAsync(operation);
            var entity = result.Result as DynamicTableEntity;
            if (entity == null)
            {
                throw new Exception($"Entity not found with specified keys: PartitionKey={partitionKey}, RowKey={rowKey}");
            }

            // 削除操作の作成
            var deleteOperation = TableOperation.Delete(entity);

            // 削除操作の実行
            await Table.ExecuteAsync(deleteOperation);
        }

        /// <summary>
        /// JSONオブジェクトからパーティションキーと行キーの値を取得する
        /// </summary>
        /// <param name="jsonObject">JSONオブジェクト</param>
        /// <returns>パーティションキーと行キーの値を含む文字列</returns>
        public string GetPartitionAndRowKeyValues(JObject jsonObject)
        {
            if (!jsonObject.ContainsKey("PartitionKey") || !jsonObject.ContainsKey("RowKey"))
                throw new ArgumentException("JSON object must contain PartitionKey and RowKey");

            var partitionKey = jsonObject["PartitionKey"]?.ToString() ?? "";
            var rowKey = jsonObject["RowKey"]?.ToString() ?? "";

            return $"PartitionKey: {partitionKey}{Environment.NewLine}RowKey: {rowKey}";
        }

        /// <summary>
        /// クエリ文字列を構築する
        /// </summary>
        /// <param name="filterText">フィルタ条件のテキスト</param>
        /// <param name="maxItemCount">最大アイテム数</param>
        /// <returns>ODATA形式のクエリ文字列</returns>
        public string BuildQuery(string filterText, int maxItemCount)
        {
            // フィルタ文字列が空の場合は全件取得
            if (string.IsNullOrWhiteSpace(filterText))
            {
                return "";
            }

            return filterText;
        }

        /// <summary>
        /// テーブルを作成する
        /// </summary>
        /// <returns>テーブル作成の成功/失敗と結果メッセージ</returns>
        public async Task<(bool Success, string Message)> CreateTableAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Table.Name))
                {
                    return (false, "Table name is not set");
                }

                var exists = await Table.ExistsAsync();
                if (exists)
                {
                    return (true, $"Table '{Table.Name}' already exists.");
                }

                // テーブルの作成
                await Table.CreateIfNotExistsAsync();
                return (true, $"Table '{Table.Name}' created successfully.");
            }
            catch (StorageException ex)
            {
                return (false, $"Storage exception occurred: {ex.Message}\nStatus code: {ex.RequestInformation?.HttpStatusCode}\nDetails: {ex.RequestInformation?.ExtendedErrorInformation?.ErrorMessage}");
            }
            catch (Exception ex)
            {
                return (false, $"Exception occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Table APIのインデックスポリシーを取得する
        /// </summary>
        /// <returns>インデックスポリシーのJSONオブジェクト</returns>
        public async Task<JObject> GetIndexingPolicyAsync()
        {
            try
            {
                // REST APIエンドポイントを構築
                var resourceUri = $"https://{_accountName}.table.cosmos.azure.com:443/";
                var resourcePath = $"Tables('{Table.Name}')";
                var uri = new Uri($"{resourceUri}{resourcePath}");

                using (var httpClient = new HttpClient())
                {
                    // 認証ヘッダーを作成
                    var utcNow = DateTime.UtcNow;
                    var httpMethod = "GET";
                    var authHeader = CreateAuthorizationHeader(httpMethod, resourcePath, utcNow);

                    httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
                    httpClient.DefaultRequestHeaders.Add("x-ms-date", utcNow.ToString("R"));
                    httpClient.DefaultRequestHeaders.Add("x-ms-version", "2020-04-08");

                    var response = await httpClient.GetAsync(uri);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var jsonResponse = JObject.Parse(content);

                        // インデックスポリシーが含まれている場合は返す
                        if (jsonResponse.ContainsKey("IndexingPolicy"))
                        {
                            return jsonResponse["IndexingPolicy"] as JObject;
                        }

                        // デフォルトのインデックスポリシーを返す
                        return CreateDefaultTableIndexingPolicy();
                    }
                    else
                    {
                        // エラーの場合はデフォルトを返す
                        return CreateDefaultTableIndexingPolicy();
                    }
                }
            }
            catch (Exception)
            {
                // エラーの場合はデフォルトのインデックスポリシーを返す
                return CreateDefaultTableIndexingPolicy();
            }
        }

        /// <summary>
        /// Table API用のデフォルトインデックスポリシーを作成
        /// </summary>
        /// <returns>デフォルトインデックスポリシー</returns>
        private JObject CreateDefaultTableIndexingPolicy()
        {
            return JObject.Parse(@"{
                ""indexingMode"": ""consistent"",
                ""automatic"": true,
                ""includedPaths"": [
                    {
                        ""path"": ""/*""
                    }
                ],
                ""excludedPaths"": [
                    {
                        ""path"": ""/\""_etag\""/?"" 
                    }
                ]
            }");
        }

        /// <summary>
        /// REST API用の認証ヘッダーを作成
        /// </summary>
        /// <param name="httpMethod">HTTPメソッド</param>
        /// <param name="resourcePath">リソースパス</param>
        /// <param name="utcNow">UTC日時</param>
        /// <returns>認証ヘッダー</returns>
        private string CreateAuthorizationHeader(string httpMethod, string resourcePath, DateTime utcNow)
        {
            var stringToSign = $"{httpMethod}\n\n\n{utcNow:R}\n/{_accountName}/{resourcePath}";

            using (var hmacSha256 = new HMACSHA256(Convert.FromBase64String(_accountKey)))
            {
                var signature = Convert.ToBase64String(hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
                return $"SharedKey {_accountName}:{signature}";
            }
        }

        /// <summary>
        /// 接続文字列からアカウント名とキーを抽出
        /// </summary>
        /// <param name="connectionString">接続文字列</param>
        private void ParseConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.StartsWith("AccountName="))
                {
                    _accountName = part.Substring("AccountName=".Length);
                }
                else if (part.StartsWith("AccountKey="))
                {
                    _accountKey = part.Substring("AccountKey=".Length);
                }
            }
        }
    }
}
