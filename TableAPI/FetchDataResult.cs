using System.Data;

namespace CosmosDBClient.TableAPI
{
    /// <summary>
    /// データ取得結果を格納するクラス
    /// </summary>
    public class FetchDataResult
    {
        /// <summary>
        /// データが格納されたDataTable
        /// </summary>
        public DataTable Data { get; set; }

        /// <summary>
        /// 合計リクエスト料金（Request Units）
        /// </summary>
        public double TotalRequestCharge { get; set; }

        /// <summary>
        /// ドキュメントの数
        /// </summary>
        public int DocumentCount { get; set; }

        /// <summary>
        /// ページ数
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// 処理にかかった時間（ミリ秒）
        /// </summary>
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FetchDataResult()
        {
            Data = new DataTable();
            TotalRequestCharge = 0;
            DocumentCount = 0;
            PageCount = 0;
            ElapsedMilliseconds = 0;
        }
    }
}
