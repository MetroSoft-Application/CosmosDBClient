using System.Data;
using System.Net.Http;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace CosmosDBClient.CosmosDB
{
    /// <summary>
    /// CosmosDBに対する操作を行うサービスクラス
    /// </summary>
    public class CosmosDBService
    {
        /// <summary>
        /// ドキュメントのシステム項目一覧
        /// </summary>
        public readonly string[] systemColumns = { "id", "_etag", "_rid", "_self", "_attachments", "_ts" };

        private static CosmosClient _cosmosClient;
        private static bool _bypassProxy = false;
        private Database _cosmosDatabase;
        private Container _cosmosContainer;
        private RequestOptions _requestOptions;

        /// <summary>
        /// CosmosDBのクライアントオブジェクト
        /// </summary>
        public CosmosClient CosmosClient
        {
            get => _cosmosClient;
        }

        /// <summary>
        /// CosmosDBのデータベースオブジェクト
        /// </summary>
        public Database CosmosDatabase
        {
            get => _cosmosDatabase;
        }

        /// <summary>
        /// CosmosDBのコンテナオブジェクト
        /// </summary>
        public Container CosmosContainer
        {
            get => _cosmosContainer;
        }

        /// <summary>
        /// リクエストオプション
        /// </summary>
        public RequestOptions RequestOptions
        {
            get => _requestOptions;
            set => _requestOptions = value;
        }

        /// <summary>
        /// PriorityLevelを設定したQueryRequestOptionsを生成する
        /// </summary>
        /// <returns>QueryRequestOptionsオブジェクト</returns>
        private QueryRequestOptions CreateQueryRequestOptions()
        {
            return new QueryRequestOptions { PriorityLevel = _requestOptions.PriorityLevel };
        }

        /// <summary>
        /// PriorityLevelを設定したItemRequestOptionsを生成する
        /// </summary>
        /// <returns>ItemRequestOptionsオブジェクト</returns>
        private ItemRequestOptions CreateItemRequestOptions()
        {
            return new ItemRequestOptions { PriorityLevel = _requestOptions.PriorityLevel };
        }

        /// <summary>
        /// PriorityLevelを設定したContainerRequestOptionsを生成する
        /// </summary>
        /// <returns>ContainerRequestOptionsオブジェクト</returns>
        private ContainerRequestOptions CreateContainerRequestOptions()
        {
            return new ContainerRequestOptions { PriorityLevel = _requestOptions.PriorityLevel };
        }

        /// <summary>
        /// CosmosDBService クラスのコンストラクタ
        /// </summary>
        /// <param name="connectionString">CosmosDBへの接続文字列</param>
        /// <param name="databaseName">データベース名</param>
        /// <param name="containerName">コンテナ名</param>
        public CosmosDBService(string connectionString, string databaseName, string containerName)
        {
            // 初回のみ CosmosClient を作成
            if (_cosmosClient == null)
            {
                _cosmosClient = CreateCosmosClient(connectionString);
            }

            // データベースの作成または取得（プロキシ認証エラー時はプロキシなしでリトライ）
            DatabaseResponse databaseResponse;
            try
            {
                databaseResponse = _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName).Result;
            }
            catch (Exception ex) when (!_bypassProxy && IsProxyAuthenticationError(ex))
            {
                // プロキシ認証エラー: プロキシなしでクライアントを再作成してリトライ
                _bypassProxy = true;
                _cosmosClient.Dispose();
                _cosmosClient = CreateCosmosClient(connectionString);
                databaseResponse = _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName).Result;
            }

            _cosmosDatabase = databaseResponse.Database;

            // データベースとコンテナの取得
            _cosmosContainer = _cosmosClient.GetContainer(databaseName, containerName);

            // デフォルトのリクエストオプションを初期化
            _requestOptions = new RequestOptions();
            _requestOptions.PriorityLevel = PriorityLevel.Low;
        }

        /// <summary>
        /// CosmosClientを生成する（_bypassProxyフラグに応じてプロキシ設定を切り替える）
        /// </summary>
        /// <param name="connectionString">CosmosDBへの接続文字列</param>
        /// <returns>CosmosClientオブジェクト</returns>
        private static CosmosClient CreateCosmosClient(string connectionString)
        {
            CosmosClientOptions cosmosClientOptions;
            if (_bypassProxy)
            {
                cosmosClientOptions = new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway,
                    HttpClientFactory = () => new HttpClient(new HttpClientHandler { UseProxy = false })
                };
            }
            else
            {
                cosmosClientOptions = new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway
                };
            }
            return new CosmosClient(connectionString, cosmosClientOptions);
        }

        /// <summary>
        /// プロキシ認証エラー（HTTP 407）かどうかを判定する
        /// </summary>
        /// <param name="ex">検査する例外</param>
        /// <returns>プロキシ認証エラーの場合はtrue</returns>
        private static bool IsProxyAuthenticationError(Exception ex)
        {
            if (ex is AggregateException aggregate)
            {
                return aggregate.InnerExceptions.Any(IsProxyAuthenticationError);
            }
            if (ex.InnerException != null && IsProxyAuthenticationError(ex.InnerException))
            {
                return true;
            }
            if (ex is HttpRequestException httpEx &&
                (httpEx.StatusCode == System.Net.HttpStatusCode.ProxyAuthenticationRequired ||
                 httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden))
            {
                return true;
            }
            var message = ex.Message ?? string.Empty;
            return message.Contains("407") || message.Contains("Proxy Authentication Required") ||
                   message.Contains("403") || message.Contains("Forbidden");
        }

        /// <summary>
        /// コンテナ名を指定してコンテナを設定する
        /// </summary>
        /// <param name="containerName">コンテナ名</param>
        public void SetContainerByName(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));
            }

            // コンテナの存在を確認
            var containerExists = DoesContainerExistAsync(containerName).GetAwaiter().GetResult();
            if (!containerExists)
            {
                throw new InvalidOperationException($"The container '{containerName}' does not exist in the database '{_cosmosDatabase.Id}'.");
            }

            // 指定されたコンテナ名でコンテナを取得
            _cosmosContainer = _cosmosDatabase.GetContainer(containerName);
        }

        /// <summary>
        /// 指定されたコンテナが存在するか確認する
        /// </summary>
        /// <param name="containerName">コンテナ名</param>
        /// <returns>コンテナが存在する場合はtrue、それ以外はfalse</returns>
        private async Task<bool> DoesContainerExistAsync(string containerName)
        {
            var requestOptions = CreateQueryRequestOptions();
            using (var iterator = _cosmosDatabase.GetContainerQueryIterator<ContainerProperties>(requestOptions: requestOptions))
            {
                while (iterator.HasMoreResults)
                {
                    foreach (var container in await iterator.ReadNextAsync())
                    {
                        if (container.Id == containerName)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// CosmosDBからデータを取得し、ステータス情報を返す
        /// </summary>
        /// <param name="query">実行するクエリ文字列</param>
        /// <param name="maxItemCount">取得する最大アイテム数</param>
        /// <returns>データ取得結果を表す FetchDataResult オブジェクト</returns>
        public async Task<FetchDataResult> FetchDataWithStatusAsync(string query, int maxItemCount)
        {
            var dataTable = new DataTable();
            var totalRequestCharge = 0d;
            var documentCount = 0;
            var pageCount = 0;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var startTime = DateTime.UtcNow;

            string errorMessage = null;
            FeedIterator<dynamic> queryResultSetIterator = null;

            try
            {
                // クエリの定義と実行
                var queryDefinition = new QueryDefinition(query);
                var requestOptions = CreateQueryRequestOptions();
                requestOptions.MaxItemCount = maxItemCount;
                queryResultSetIterator = _cosmosContainer.GetItemQueryIterator<dynamic>(
                    queryDefinition,
                    requestOptions: requestOptions);

                // 結果の処理
                while (queryResultSetIterator.HasMoreResults)
                {
                    var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    pageCount++;
                    totalRequestCharge += currentResultSet.RequestCharge;
                    documentCount += currentResultSet.Count;

                    // データテーブルに結果を追加
                    foreach (var item in currentResultSet)
                    {
                        var jsonObject = JObject.Parse(item.ToString());
                        AddRowToDataTable(jsonObject, dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
            }

            // パーティションキー情報を取得し、カラムの並び替えを実施する
            var pkPaths = await GetPartitionKeyPathsAsync();
            MoveSystemColumnsToEnd(dataTable, pkPaths);
            dataTable = ConvertNumericColumns(dataTable);

            var endTime = DateTime.UtcNow;
            var dataSizeInBytes = CalculateDataSize(dataTable);

            return new FetchDataResult(
                dataTable,
                totalRequestCharge,
                documentCount,
                pageCount,
                stopwatch.ElapsedMilliseconds,
                errorMessage,
                query,
                dataSizeInBytes,
                startTime,
                endTime);
        }

        /// <summary>
        /// CosmosDBから1ページ分のデータを取得し、ステータス情報を返す
        /// </summary>
        /// <param name="query">実行するクエリ文字列</param>
        /// <param name="pageSize">1ページあたりの最大アイテム数</param>
        /// <param name="continuationToken">前のページから取得したContinuationToken</param>
        /// <returns>データ取得結果を表す FetchDataResult オブジェクト</returns>
        public async Task<FetchDataResult> FetchDataPageAsync(string query, int pageSize, string continuationToken = null)
        {
            var dataTable = new DataTable();
            var totalRequestCharge = 0d;
            var documentCount = 0;
            var pageCount = 0;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var startTime = DateTime.UtcNow;

            string errorMessage = null;
            string nextContinuationToken = null;

            try
            {
                // クエリの定義と実行
                var queryDefinition = new QueryDefinition(query);
                var requestOptions = CreateQueryRequestOptions();
                requestOptions.MaxItemCount = pageSize;
                var queryResultSetIterator = _cosmosContainer.GetItemQueryIterator<dynamic>(
                    queryDefinition,
                    continuationToken,
                    requestOptions);

                // 1ページ分のみ取得
                if (queryResultSetIterator.HasMoreResults)
                {
                    var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    pageCount = 1;
                    totalRequestCharge = currentResultSet.RequestCharge;
                    documentCount = currentResultSet.Count;
                    nextContinuationToken = currentResultSet.ContinuationToken;

                    // データテーブルに結果を追加
                    foreach (var item in currentResultSet)
                    {
                        var jsonObject = JObject.Parse(item.ToString());
                        AddRowToDataTable(jsonObject, dataTable);
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
            }

            // パーティションキー情報を取得し、カラムの並び替えを実施する
            var pkPaths = await GetPartitionKeyPathsAsync();
            MoveSystemColumnsToEnd(dataTable, pkPaths);
            dataTable = ConvertNumericColumns(dataTable);

            var endTime = DateTime.UtcNow;
            var dataSizeInBytes = CalculateDataSize(dataTable);

            return new FetchDataResult(
                dataTable,
                totalRequestCharge,
                documentCount,
                pageCount,
                stopwatch.ElapsedMilliseconds,
                errorMessage,
                query,
                dataSizeInBytes,
                startTime,
                endTime,
                nextContinuationToken);
        }

        /// <summary>
        /// DataTableのデータサイズを計算する
        /// </summary>
        /// <param name="dataTable">データテーブル</param>
        /// <returns>データサイズ（バイト単位）</returns>
        private long CalculateDataSize(DataTable dataTable)
        {
            long size = 0;

            foreach (DataRow row in dataTable.Rows)
            {
                foreach (var item in row.ItemArray)
                {
                    if (item != null)
                    {
                        size += System.Text.Encoding.UTF8.GetByteCount(item.ToString());
                    }
                }
            }

            return size;
        }

        /// <summary>
        /// DataTableのカラムを並び替え、システムカラムを最後に移動する
        /// カラム順序: id → パーティションキー → ユーザープロパティ → システムプロパティ
        /// </summary>
        /// <param name="dataTable">並び替え対象のDataTable</param>
        /// <param name="partitionKeyPaths">パーティションキーのパス配列（省略可）</param>
        public void MoveSystemColumnsToEnd(DataTable dataTable, string[] partitionKeyPaths = null)
        {
            if (dataTable.Columns.Count == 0)
            {
                return;
            }

            // アンダースコアで始まるシステムカラムを取得（DataTableに存在するもののみ）
            var systemColumnSet = new HashSet<string>(systemColumns.Where(c => c.StartsWith("_") && dataTable.Columns.Contains(c)));

            // パーティションキーのセットを作成
            var pkSet = new HashSet<string>(partitionKeyPaths ?? Array.Empty<string>());

            // カラムを分類
            var idColumns = new List<string>();
            var pkColumns = new List<string>();
            var userColumns = new List<string>();
            var sysColumns = new List<string>();

            foreach (DataColumn col in dataTable.Columns)
            {
                if (col.ColumnName == "id")
                {
                    idColumns.Add(col.ColumnName);
                }
                else if (systemColumnSet.Contains(col.ColumnName))
                {
                    sysColumns.Add(col.ColumnName);
                }
                else if (pkSet.Contains(col.ColumnName))
                {
                    pkColumns.Add(col.ColumnName);
                }
                else
                {
                    userColumns.Add(col.ColumnName);
                }
            }

            // id → パーティションキー → ユーザープロパティ → システムプロパティ の順に並び替え
            var columnOrder = idColumns.Concat(pkColumns).Concat(userColumns).Concat(sysColumns).ToList();

            for (int i = 0; i < columnOrder.Count; i++)
            {
                dataTable.Columns[columnOrder[i]].SetOrdinal(i);
            }
        }

        /// <summary>
        /// コンテナのパーティションキーパスを取得する
        /// </summary>
        /// <returns>パーティションキーのフィールド名配列</returns>
        public async Task<string[]> GetPartitionKeyPathsAsync()
        {
            var containerProperties = await GetContainerPropertiesAsync();
            return containerProperties.PartitionKeyPaths.Select(p => p.Trim('/')).ToArray();
        }

        /// <summary>
        /// JSONオブジェクトからデータテーブルに行を追加する
        /// </summary>
        /// <param name="jsonObject">JSONオブジェクト</param>
        /// <param name="dataTable">データテーブル</param>
        private void AddRowToDataTable(JObject jsonObject, DataTable dataTable)
        {
            var row = dataTable.NewRow();
            var jstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

            foreach (var property in jsonObject.Properties())
            {
                // カラムが存在するか確認
                if (!dataTable.Columns.Contains(property.Name))
                {
                    // 存在しないカラムは動的に追加
                    dataTable.Columns.Add(property.Name, typeof(string));
                }

                // 日付項目をJSTに変換してフォーマット（年月日を含む文字列のみ対象）
                var strValue = property.Value?.ToString();
                if (strValue != null &&
                    System.Text.RegularExpressions.Regex.IsMatch(strValue, @"\d{4}-\d{2}-\d{2}") &&
                    DateTime.TryParse(strValue, out var dateValue))
                {
                    var jstDate = TimeZoneInfo.ConvertTimeFromUtc(dateValue.ToUniversalTime(), jstTimeZone);
                    row[property.Name] = jstDate.ToString("yyyy-MM-ddTHH:mm:ss"); // タイムゾーン情報なし
                }
                else
                {
                    // 新しいカラムに値を設定
                    row[property.Name] = property.Value ?? string.Empty;
                }
            }

            // 行をDataTableに追加
            dataTable.Rows.Add(row);
        }

        /// <summary>
        /// レコードをアップサートする
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="jsonObject">アップサートするレコード</param>
        /// <param name="partitionKey">パーティションキー</param>
        /// <returns>アップサートの結果</returns>
        public async Task<ItemResponse<T>> UpsertItemAsync<T>(T jsonObject, PartitionKey partitionKey)
        {
            var requestOptions = CreateItemRequestOptions();
            return await _cosmosContainer.UpsertItemAsync(jsonObject, partitionKey, requestOptions);
        }

        /// <summary>
        /// レコードを削除する
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <param name="id">削除するアイテムのID</param>
        /// <param name="partitionKey">パーティションキー</param>
        /// <returns>削除の結果</returns>
        public async Task<ItemResponse<T>> DeleteItemAsync<T>(string id, PartitionKey partitionKey)
        {
            var requestOptions = CreateItemRequestOptions();
            return await _cosmosContainer.DeleteItemAsync<T>(id, partitionKey, requestOptions);
        }

        /// <summary>
        /// コンテナのプロパティを取得する
        /// </summary>
        /// <returns>ContainerPropertiesオブジェクト</returns>
        public async Task<ContainerProperties> GetContainerPropertiesAsync()
        {
            var requestOptions = CreateContainerRequestOptions();
            return await _cosmosContainer.ReadContainerAsync(requestOptions);
        }

        /// <summary>
        /// CosmosDBアカウント内のデータベース一覧を取得する
        /// </summary>
        /// <returns>データベース名のリスト</returns>
        public async Task<List<string>> GetDatabaseNamesAsync()
        {
            var databases = new List<string>();
            var requestOptions = CreateQueryRequestOptions();
            using (var iterator = _cosmosClient.GetDatabaseQueryIterator<DatabaseProperties>(requestOptions: requestOptions))
            {
                while (iterator.HasMoreResults)
                {
                    foreach (var database in await iterator.ReadNextAsync())
                    {
                        databases.Add(database.Id);
                    }
                }
            }
            return databases;
        }

        /// <summary>
        /// 指定されたデータベースのコンテナ一覧を取得する
        /// </summary>
        /// <returns>コンテナ名のリスト</returns>
        public async Task<List<string>> GetContainerNamesAsync()
        {
            var containers = new List<string>();
            var database = _cosmosClient.GetDatabase(_cosmosContainer.Database.Id);
            var requestOptions = CreateQueryRequestOptions();
            using (var iterator = database.GetContainerQueryIterator<ContainerProperties>(requestOptions: requestOptions))
            {
                while (iterator.HasMoreResults)
                {
                    foreach (var container in await iterator.ReadNextAsync())
                    {
                        containers.Add(container.Id);
                    }
                }
            }
            return containers;
        }

        /// <summary>
        /// パーティションキーを解決する
        /// </summary>
        /// <param name="jsonObject">JSONオブジェクト</param>
        /// <returns>パーティションキー</returns>
        public async Task<PartitionKey> ResolvePartitionKeyAsync(JObject jsonObject)
        {
            var requestOptions = CreateContainerRequestOptions();
            var containerProperties = await _cosmosContainer.ReadContainerAsync(requestOptions);
            var partitionKeyPaths = containerProperties.Resource.PartitionKeyPaths;
            var partitionKeyBuilder = new PartitionKeyBuilder();

            foreach (var path in partitionKeyPaths)
            {
                var key = jsonObject.SelectToken(path.Trim('/'))?.ToString();
                if (key == null)
                {
                    return PartitionKey.None;
                }
                partitionKeyBuilder.Add(key);
            }

            return partitionKeyBuilder.Build();
        }

        /// <summary>
        /// PartitionKeyに対応するフィールド名と値を取得し、改行して表示するための文字列を構築する
        /// </summary>
        /// <param name="jsonObject">パースされたJSONオブジェクト</param>
        /// <returns>PartitionKeyに対応するフィールド名と値を改行で連結した文字列</returns>
        public string GetPartitionKeyValues(JObject jsonObject)
        {
            var requestOptions = CreateContainerRequestOptions();
            var containerProperties = _cosmosContainer.ReadContainerAsync(requestOptions).Result;
            var partitionKeyPaths = containerProperties.Resource.PartitionKeyPaths;

            return string.Join("\n", partitionKeyPaths.Select(path =>
            {
                var key = jsonObject.SelectToken(path.Trim('/'))?.ToString();
                return $"{path.Trim('/')}: {key}";
            }));
        }

        /// <summary>
        /// 型判定でサンプリングする最大行数
        /// </summary>
        private const int TypeDetectionSampleSize = 200;

        /// <summary>
        /// DataTable の列値を先頭 <see cref="TypeDetectionSampleSize"/> 行のサンプリングで検査し、
        /// 数値列を long または double 型に変換した新しい DataTable を返す
        /// </summary>
        /// <param name="source">変換元の DataTable</param>
        /// <returns>数値列を適切な型に変換した DataTable</returns>
        public DataTable ConvertNumericColumns(DataTable source)
        {
            if (source.Rows.Count == 0)
            {
                return source;
            }

            // 各列の型を判定
            var columnTypes = new Type[source.Columns.Count];
            for (int i = 0; i < source.Columns.Count; i++)
            {
                columnTypes[i] = DetectColumnType(source, i);
            }

            // 変換が不要な場合はそのまま返す
            if (columnTypes.All(t => t == typeof(string)))
            {
                return source;
            }

            // 新しい DataTable を構築
            var result = new DataTable();
            for (int i = 0; i < source.Columns.Count; i++)
            {
                result.Columns.Add(source.Columns[i].ColumnName, columnTypes[i]);
            }

            foreach (DataRow row in source.Rows)
            {
                var newRow = result.NewRow();
                for (int i = 0; i < source.Columns.Count; i++)
                {
                    var val = row[i]?.ToString();
                    if (string.IsNullOrEmpty(val))
                    {
                        newRow[i] = DBNull.Value;
                    }
                    else if (columnTypes[i] == typeof(long))
                    {
                        newRow[i] = long.TryParse(val, out var l) ? (object)l : DBNull.Value;
                    }
                    else if (columnTypes[i] == typeof(double))
                    {
                        newRow[i] = double.TryParse(val, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var d)
                            ? (object)d : DBNull.Value;
                    }
                    else
                    {
                        newRow[i] = val;
                    }
                }
                result.Rows.Add(newRow);
            }

            return result;
        }

        /// <summary>
        /// DataTable の指定列を先頭 <see cref="TypeDetectionSampleSize"/> 行のサンプリングで判定し、適切な型を返す
        /// </summary>
        private static Type DetectColumnType(DataTable table, int columnIndex)
        {
            bool allLong = true;
            bool allDouble = true;
            bool hasValue = false;
            int checked_ = 0;

            foreach (DataRow row in table.Rows)
            {
                if (checked_ >= TypeDetectionSampleSize)
                {
                    break;
                }

                var val = row[columnIndex]?.ToString();
                if (string.IsNullOrEmpty(val))
                {
                    continue;
                }

                hasValue = true;
                checked_++;

                if (allLong && !long.TryParse(val, out _))
                {
                    allLong = false;
                }

                if (!double.TryParse(val, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                {
                    allDouble = false;
                    break;
                }
            }

            if (!hasValue || !allDouble)
            {
                return typeof(string);
            }

            if (allLong)
            {
                return typeof(long);
            }

            return typeof(double);
        }
    }
}
