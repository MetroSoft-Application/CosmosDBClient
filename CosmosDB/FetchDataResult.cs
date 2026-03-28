using System.Data;

namespace CosmosDBClient.CosmosDB
{
    /// <summary>
    /// CosmosDBからのデータ取得結果を表すクラス
    /// </summary>
    public class FetchDataResult
    {
        /// <summary>
        /// 取得したデータ
        /// </summary>
        public DataTable Data { get; }

        /// <summary>
        /// 総リクエストチャージ (RU)
        /// </summary>
        public double TotalRequestCharge { get; }

        /// <summary>
        /// ドキュメント数
        /// </summary>
        public int DocumentCount { get; }

        /// <summary>
        /// ページ数
        /// </summary>
        public int PageCount { get; }

        /// <summary>
        /// アプリケーション全体の処理時間 (ミリ秒)
        /// </summary>
        public long ElapsedMilliseconds { get; }

        /// <summary>
        /// Cosmos DB への要求と応答に要した時間の合計 (ミリ秒)
        /// </summary>
        public long CosmosLatencyMilliseconds { get; }

        /// <summary>
        /// データ取得中に発生したエラー情報
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// 実行したクエリ文字列
        /// </summary>
        public string ExecutedQuery { get; }

        /// <summary>
        /// 取得したデータの行数
        /// </summary>
        public int RowCount => Data?.Rows.Count ?? 0;

        /// <summary>
        /// 取得したデータの総バイト数
        /// </summary>
        public long DataSizeInBytes { get; }

        /// <summary>
        /// データ取得の開始時刻
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// データ取得の終了時刻
        /// </summary>
        public DateTime EndTime { get; }

        /// <summary>
        /// 次のページ取得用ContinuationToken
        /// </summary>
        public string ContinuationToken { get; }

        /// <summary>
        /// パーティション単位の Query Metrics 生データ
        /// </summary>
        public IReadOnlyList<QueryMetricsPerPartitionRecord> QueryMetricsPerPartition { get; }

        /// <summary>
        /// すべてのプロパティを指定するコンストラクタ
        /// </summary>
        /// <param name="data">取得したデータ</param>
        /// <param name="totalRequestCharge">総リクエストチャージ (RU)</param>
        /// <param name="documentCount">ドキュメント数</param>
        /// <param name="pageCount">ページ数</param>
        /// <param name="elapsedMilliseconds">アプリケーション全体の処理時間 (ミリ秒)</param>
        /// <param name="cosmosLatencyMilliseconds">Cosmos DB への要求と応答に要した時間の合計 (ミリ秒)</param>
        /// <param name="errorMessage">データ取得中に発生したエラー情報</param>
        /// <param name="executedQuery">実行したクエリ文字列</param>
        /// <param name="dataSizeInBytes">取得したデータの総バイト数</param>
        /// <param name="startTime">データ取得の開始時刻</param>
        /// <param name="endTime">データ取得の終了時刻</param>
        /// <param name="continuationToken">次のページ取得用ContinuationToken</param>
        /// <param name="queryMetricsPerPartition">パーティション単位の Query Metrics 生データ</param>
        public FetchDataResult(
            DataTable data,
            double totalRequestCharge,
            int documentCount,
            int pageCount,
            long elapsedMilliseconds,
            long cosmosLatencyMilliseconds,
            string errorMessage,
            string executedQuery,
            long dataSizeInBytes,
            DateTime startTime,
            DateTime endTime,
            string continuationToken = null,
            IReadOnlyList<QueryMetricsPerPartitionRecord> queryMetricsPerPartition = null)
        {
            Data = data ?? new DataTable();
            TotalRequestCharge = totalRequestCharge;
            DocumentCount = documentCount;
            PageCount = pageCount;
            ElapsedMilliseconds = elapsedMilliseconds;
            CosmosLatencyMilliseconds = cosmosLatencyMilliseconds;
            ErrorMessage = errorMessage;
            ExecutedQuery = executedQuery ?? string.Empty;
            DataSizeInBytes = dataSizeInBytes;
            StartTime = startTime;
            EndTime = endTime;
            ContinuationToken = continuationToken;
            QueryMetricsPerPartition = queryMetricsPerPartition ?? Array.Empty<QueryMetricsPerPartitionRecord>();
        }
    }
}
