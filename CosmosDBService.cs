using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;

namespace CosmosDBClient
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
        private readonly Container _cosmosContainer;

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

            // データベースとコンテナの取得
            _cosmosContainer = _cosmosClient.GetContainer(databaseName, containerName);
        }

        /// <summary>
        /// CosmosDBからデータを取得し、ステータス情報を返す
        /// </summary>
        /// <param name="query">実行するクエリ文字列</param>
        /// <param name="maxItemCount">取得する最大アイテム数</param>
        /// <returns>データテーブルと、リクエストチャージ、ドキュメント数、ページ数、処理時間を含むタプル</returns>
        public async Task<(DataTable data, double totalRequestCharge, int documentCount, int pageCount, long elapsedMilliseconds)>
            FetchDataWithStatusAsync(string query, int maxItemCount)
        {
            var dataTable = new DataTable();
            var totalRequestCharge = 0d;
            var documentCount = 0;
            var pageCount = 0;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // クエリの定義と実行
            var queryDefinition = new QueryDefinition(query);
            var queryResultSetIterator = _cosmosContainer.GetItemQueryIterator<dynamic>(
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

            stopwatch.Stop();
            // カラムの並び替えを実施する
            MoveSystemColumnsToEnd(dataTable);

            return (dataTable, totalRequestCharge, documentCount, pageCount, stopwatch.ElapsedMilliseconds);
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

            foreach (var property in jsonObject.Properties())
            {
                // カラムが存在するか確認
                if (!dataTable.Columns.Contains(property.Name))
                {
                    // 存在しないカラムは動的に追加
                    dataTable.Columns.Add(property.Name, typeof(string));
                }

                // 新しいカラムに値を設定
                row[property.Name] = property.Value?.ToString() ?? string.Empty;
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
