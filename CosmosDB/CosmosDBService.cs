using System.Data;
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
        private Database _cosmosDatabase;
        private Container _cosmosContainer;

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
                var cosmosClientOptions = new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway
                };
                _cosmosClient = new CosmosClient(connectionString, cosmosClientOptions);
            }

            // データベースの作成または取得
            var databaseResponse = _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName).Result;
            _cosmosDatabase = databaseResponse.Database;

            // データベースとコンテナの取得
            _cosmosContainer = _cosmosClient.GetContainer(databaseName, containerName);
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
            using (var iterator = _cosmosDatabase.GetContainerQueryIterator<ContainerProperties>())
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
                queryResultSetIterator = _cosmosContainer.GetItemQueryIterator<dynamic>(
                    queryDefinition, requestOptions: new QueryRequestOptions { MaxItemCount = maxItemCount });

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

            // カラムの並び替えを実施する
            MoveSystemColumnsToEnd(dataTable);

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
        /// </summary>
        /// <param name="dataTable">並び替え対象のDataTable</param>
        private void MoveSystemColumnsToEnd(DataTable dataTable)
        {
            // システムカラムをリスト化（データテーブルに存在するカラムだけを取得）
            var systemColumnsList = systemColumns.Where(c => c != "id" && dataTable.Columns.Contains(c)).ToList();

            // システムカラム以外のカラムをリスト化
            var nonSystemColumns = dataTable.Columns.Cast<DataColumn>()
                .Where(col => !systemColumnsList.Contains(col.ColumnName))
                .Select(col => col.ColumnName)
                .ToList();

            // 並び替え用のカラム順序リストを作成
            var columnOrder = nonSystemColumns.Concat(systemColumnsList).ToList();

            // 新しい順序でDataTableを構築
            for (int i = 0; i < columnOrder.Count; i++)
            {
                dataTable.Columns[columnOrder[i]].SetOrdinal(i);
            }
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

                // 日付項目をJSTに変換してフォーマット
                if (DateTime.TryParse(property.Value?.ToString(), out var dateValue))
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
            return await _cosmosContainer.UpsertItemAsync(jsonObject, partitionKey);
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
            return await _cosmosContainer.DeleteItemAsync<T>(id, partitionKey);
        }

        /// <summary>
        /// コンテナのプロパティを取得する
        /// </summary>
        /// <returns>ContainerPropertiesオブジェクト</returns>
        public async Task<ContainerProperties> GetContainerPropertiesAsync()
        {
            return await _cosmosContainer.ReadContainerAsync();
        }

        /// <summary>
        /// CosmosDBアカウント内のデータベース一覧を取得する
        /// </summary>
        /// <returns>データベース名のリスト</returns>
        public async Task<List<string>> GetDatabaseNamesAsync()
        {
            var databases = new List<string>();
            using (var iterator = _cosmosClient.GetDatabaseQueryIterator<DatabaseProperties>())
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
            using (var iterator = database.GetContainerQueryIterator<ContainerProperties>())
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
            var containerProperties = await _cosmosContainer.ReadContainerAsync();
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
            var containerProperties = _cosmosContainer.ReadContainerAsync().Result;
            var partitionKeyPaths = containerProperties.Resource.PartitionKeyPaths;

            return string.Join("\n", partitionKeyPaths.Select(path =>
            {
                var key = jsonObject.SelectToken(path.Trim('/'))?.ToString();
                return $"{path.Trim('/')}: {key}";
            }));
        }
    }
}
