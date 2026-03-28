namespace CosmosDBClient.CosmosDB
{
    /// <summary>
    /// パーティション単位の Query Metrics 生データを表すクラス
    /// </summary>
    public class QueryMetricsPerPartitionRecord
    {
        /// <summary>パーティション キー範囲 ID</summary>
        public string PartitionKeyRangeId { get; }

        /// <summary>取得されたドキュメント数</summary>
        public long RetrievedDocumentCount { get; }

        /// <summary>取得されたドキュメントの合計サイズ（バイト）</summary>
        public long RetrievedDocumentSizeInBytes { get; }

        /// <summary>出力されたドキュメント数</summary>
        public long OutputDocumentCount { get; }

        /// <summary>出力されたドキュメントの合計サイズ（バイト）</summary>
        public long OutputDocumentSizeInBytes { get; }

        /// <summary>インデックスにヒットしたドキュメント数</summary>
        public long IndexHitDocumentCount { get; }

        /// <summary>インデックス検索にかかった時間（ミリ秒）</summary>
        public double IndexLookupTimeMilliseconds { get; }

        /// <summary>ドキュメント読み込みにかかった時間（ミリ秒）</summary>
        public double DocumentLoadTimeMilliseconds { get; }

        /// <summary>クエリ エンジン実行にかかった時間（ミリ秒）</summary>
        public double QueryEngineExecutionTimeMilliseconds { get; }

        /// <summary>システム関数実行にかかった時間（ミリ秒）</summary>
        public double SystemFunctionExecutionTimeMilliseconds { get; }

        /// <summary>ユーザー定義関数実行にかかった時間（ミリ秒）</summary>
        public double UserDefinedFunctionExecutionTimeMilliseconds { get; }

        /// <summary>ドキュメント書き込みにかかった時間（ミリ秒）</summary>
        public double DocumentWriteTimeMilliseconds { get; }

        /// <summary>
        /// <see cref="QueryMetricsPerPartitionRecord"/> の新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="partitionKeyRangeId">パーティション キー範囲 ID</param>
        /// <param name="retrievedDocumentCount">取得されたドキュメント数</param>
        /// <param name="retrievedDocumentSizeInBytes">取得されたドキュメントの合計サイズ（バイト）</param>
        /// <param name="outputDocumentCount">出力されたドキュメント数</param>
        /// <param name="outputDocumentSizeInBytes">出力されたドキュメントの合計サイズ（バイト）</param>
        /// <param name="indexHitDocumentCount">インデックスにヒットしたドキュメント数</param>
        /// <param name="indexLookupTimeMilliseconds">インデックス検索にかかった時間（ミリ秒）</param>
        /// <param name="documentLoadTimeMilliseconds">ドキュメント読み込みにかかった時間（ミリ秒）</param>
        /// <param name="queryEngineExecutionTimeMilliseconds">クエリ エンジン実行にかかった時間（ミリ秒）</param>
        /// <param name="systemFunctionExecutionTimeMilliseconds">システム関数実行にかかった時間（ミリ秒）</param>
        /// <param name="userDefinedFunctionExecutionTimeMilliseconds">ユーザー定義関数実行にかかった時間（ミリ秒）</param>
        /// <param name="documentWriteTimeMilliseconds">ドキュメント書き込みにかかった時間（ミリ秒）</param>
        public QueryMetricsPerPartitionRecord(
            string partitionKeyRangeId,
            long retrievedDocumentCount,
            long retrievedDocumentSizeInBytes,
            long outputDocumentCount,
            long outputDocumentSizeInBytes,
            long indexHitDocumentCount,
            double indexLookupTimeMilliseconds,
            double documentLoadTimeMilliseconds,
            double queryEngineExecutionTimeMilliseconds,
            double systemFunctionExecutionTimeMilliseconds,
            double userDefinedFunctionExecutionTimeMilliseconds,
            double documentWriteTimeMilliseconds)
        {
            PartitionKeyRangeId = partitionKeyRangeId ?? string.Empty;
            RetrievedDocumentCount = retrievedDocumentCount;
            RetrievedDocumentSizeInBytes = retrievedDocumentSizeInBytes;
            OutputDocumentCount = outputDocumentCount;
            OutputDocumentSizeInBytes = outputDocumentSizeInBytes;
            IndexHitDocumentCount = indexHitDocumentCount;
            IndexLookupTimeMilliseconds = indexLookupTimeMilliseconds;
            DocumentLoadTimeMilliseconds = documentLoadTimeMilliseconds;
            QueryEngineExecutionTimeMilliseconds = queryEngineExecutionTimeMilliseconds;
            SystemFunctionExecutionTimeMilliseconds = systemFunctionExecutionTimeMilliseconds;
            UserDefinedFunctionExecutionTimeMilliseconds = userDefinedFunctionExecutionTimeMilliseconds;
            DocumentWriteTimeMilliseconds = documentWriteTimeMilliseconds;
        }
    }
}