using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace CosmosDBClient
{
    /// <summary>
    /// Cosmos DB クライアントのデータを表示および管理するためのフォーム
    /// </summary>
    public partial class Form1 : Form
    {
        private CosmosClient cosmosClient;
        private Container cosmosContainer;
        private readonly int maxItemCount;
        private HyperlinkHandler hyperlinkHandler;
        private readonly bool useHyperlinkHandler;
        private readonly string? connectionString;
        private readonly string? databaseName;
        private readonly string? containerName;

        /// <summary>
        /// 新しい <see cref="Form1"/> クラスのインスタンスを初期化する
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            var configuration = LoadConfiguration();

            useHyperlinkHandler = configuration.GetValue<bool>("AppSettings:EnableHyperlinkHandler");
            maxItemCount = configuration.GetValue<int>("AppSettings:MaxItemCount");
            connectionString = configuration.GetValue<string>("AppSettings:ConnectionString");
            databaseName = configuration.GetValue<string>("AppSettings:DatabaseName");
            containerName = configuration.GetValue<string>("AppSettings:ContainerName");

            textBoxConnectionString.Text = connectionString;
            textBoxDatabaseName.Text = databaseName;
            textBoxContainerName.Text = containerName;
            numericUpDownMaxCount.Value = maxItemCount;

            if (useHyperlinkHandler)
            {
                hyperlinkHandler = new HyperlinkHandler();
            }
        }

        /// <summary>
        /// 設定ファイルと環境変数からアプリケーション設定を読み込む
        /// </summary>
        /// <returns>設定を含む <see cref="IConfigurationRoot"/> オブジェクト</returns>
        private IConfigurationRoot LoadConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return configuration;
        }

        /// <summary>
        /// Cosmos DB からデータをロードし、DataGridView に表示する
        /// </summary>
        /// <param name="sender">イベントの送信者</param>
        /// <param name="e">イベントのデータ</param>
        private async void buttonLoadData_Click(object sender, EventArgs e)
        {
            InitializeCosmosClient(textBoxConnectionString.Text,textBoxDatabaseName.Text,textBoxContainerName.Text);
            var dataTable = await FetchDataFromCosmosDBAsync();
            AddHiddenJsonColumnIfNeeded();
            dataGridViewResults.DataSource = dataTable;
            dataGridViewResults.Columns[1].Visible = false;
        }

        /// <summary>
        /// Cosmos DB クライアントを初期化する
        /// </summary>
        /// <param name="connectionString">接続文字列</param>
        /// <param name="databaseName">DB名</param>
        /// <param name="containerName">コンテナ名</param>
        private void InitializeCosmosClient(string connectionString,string databaseName,string containerName)
        {
            cosmosClient = new CosmosClient(connectionString);
            cosmosContainer = cosmosClient.GetContainer(databaseName, containerName);
        }

        /// <summary>
        /// DataGridView に隠し列を追加する
        /// </summary>
        private void AddHiddenJsonColumnIfNeeded()
        {
            if (!dataGridViewResults.Columns.Contains("JsonData"))
            {
                var jsonColumn = new DataGridViewTextBoxColumn
                {
                    Name = "JsonData",
                    HeaderText = "JsonData",
                    Visible = false,
                };
                dataGridViewResults.Columns.Add(jsonColumn);
            }
        }

        /// <summary>
        /// 非同期で Cosmos DB からデータを取得し、DataTable に格納する
        /// </summary>
        /// <returns>Cosmos DB から取得したデータを含む <see cref="DataTable"/></returns>
        private async Task<DataTable> FetchDataFromCosmosDBAsync()
        {
            var dataTable = CreateDataTable();
            var maxCount = GetMaxItemCount();
            var query = BuildQuery(maxCount);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var (totalRequestCharge, documentCount, pageCount) = await ExecuteCosmosDbQuery(query, maxCount, dataTable);
                stopwatch.Stop();
                UpdateStatusStrip(totalRequestCharge, documentCount, pageCount, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return dataTable;
        }

        /// <summary>
        /// DataTable を作成し、初期設定を行う
        /// </summary>
        /// <returns>初期化された <see cref="DataTable"/></returns>
        private DataTable CreateDataTable()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("JsonData", typeof(string));
            return dataTable;
        }

        /// <summary>
        /// 最大アイテム数を取得する
        /// </summary>
        /// <returns>最大アイテム数</returns>
        private int GetMaxItemCount()
        {
            return Math.Max((int)numericUpDownMaxCount.Value, maxItemCount);
        }

        /// <summary>
        /// 非同期で Cosmos DB のクエリを実行する
        /// </summary>
        /// <param name="query">実行するクエリ</param>
        /// <param name="maxCount">取得する最大アイテム数</param>
        /// <param name="dataTable">データを格納する DataTable</param>
        /// <returns>クエリ実行後の統計情報 (リクエストチャージ、ドキュメント数、ページ数)</returns>
        private async Task<(double totalRequestCharge, int documentCount, int pageCount)> ExecuteCosmosDbQuery(string query, int maxCount, DataTable dataTable)
        {
            var totalRequestCharge = 0d;
            var documentCount = 0;
            var pageCount = 0;

            var queryDefinition = new QueryDefinition(query);
            using var queryResultSetIterator = cosmosContainer.GetItemQueryIterator<dynamic>(
                queryDefinition, requestOptions: new QueryRequestOptions { MaxItemCount = maxCount });

            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                pageCount++;

                totalRequestCharge += currentResultSet.RequestCharge;
                documentCount += currentResultSet.Count;

                ProcessQueryResults(currentResultSet, dataTable, maxCount);
            }

            return (totalRequestCharge, documentCount, pageCount);
        }

        /// <summary>
        /// クエリ文字列を構築する
        /// </summary>
        /// <param name="maxCount">取得する最大アイテム数</param>
        /// <returns>構築されたクエリ文字列</returns>
        private string BuildQuery(int maxCount)
        {
            var query = $"SELECT TOP {maxCount} * FROM c";

            if (!string.IsNullOrWhiteSpace(richTextBoxQuery.Text))
            {
                query = richTextBoxQuery.Text;
                if (!Regex.IsMatch(query, @"\bSELECT\s+TOP\b", RegexOptions.IgnoreCase))
                {
                    int selectIndex = Regex.Match(query, @"\bSELECT\b", RegexOptions.IgnoreCase).Index;
                    if (selectIndex != -1)
                    {
                        query = query.Insert(selectIndex + 6, $" TOP {maxCount}");
                    }
                }
            }

            return query;
        }

        /// <summary>
        /// クエリ結果を処理し、DataTable に追加する
        /// </summary>
        /// <param name="resultSet">クエリ結果のセット</param>
        /// <param name="dataTable">データを格納する DataTable</param>
        /// <param name="maxCount">最大アイテム数</param>
        private void ProcessQueryResults(FeedResponse<dynamic> resultSet, DataTable dataTable, int maxCount)
        {
            foreach (var item in resultSet)
            {
                var jsonObject = JObject.Parse(item.ToString());

                if (dataTable.Columns.Count == 1)
                {
                    AddColumnsToDataTable(jsonObject, dataTable);
                }

                AddRowToDataTable(jsonObject, dataTable);

                if (dataTable.Rows.Count >= maxCount)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// JSONオブジェクトから DataTable に列を追加する
        /// </summary>
        /// <param name="jsonObject">JSON オブジェクト</param>
        /// <param name="dataTable">データを格納する DataTable</param>
        private void AddColumnsToDataTable(JObject jsonObject, DataTable dataTable)
        {
            foreach (var property in jsonObject.Properties())
            {
                dataTable.Columns.Add(property.Name);
            }
        }

        /// <summary>
        /// JSONオブジェクトから DataTable に行を追加する
        /// </summary>
        /// <param name="jsonObject">JSON オブジェクト</param>
        /// <param name="dataTable">データを格納する DataTable</param>
        private void AddRowToDataTable(JObject jsonObject, DataTable dataTable)
        {
            var row = dataTable.NewRow();
            foreach (var property in jsonObject.Properties())
            {
                row[property.Name] = property.Value?.ToString() ?? string.Empty;
            }
            row["JsonData"] = jsonObject.ToString();
            dataTable.Rows.Add(row);
        }

        /// <summary>
        /// ステータスストリップを更新する
        /// </summary>
        /// <param name="totalRequestCharge">総リクエストチャージ</param>
        /// <param name="documentCount">ドキュメント数</param>
        /// <param name="pageCount">ページ数</param>
        /// <param name="elapsedMilliseconds">経過時間 (ミリ秒)</param>
        private void UpdateStatusStrip(double totalRequestCharge, int documentCount, int pageCount, long elapsedMilliseconds)
        {
            toolStripStatusLabel1.Text = $"Total RU: {totalRequestCharge:F2}";
            toolStripStatusLabel2.Text = $"Documents: {documentCount}";
            toolStripStatusLabel3.Text = $"Pages: {pageCount}";
            toolStripStatusLabel4.Text = $"Elapsed Time: {elapsedMilliseconds} ms";
        }

        /// <summary>
        /// DataGridView の行が描画された後の処理を行う
        /// </summary>
        /// <param name="sender">イベントの送信者</param>
        /// <param name="e">行の描画イベントデータ</param>
        private void dataGridViewResults_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            using var brush = new SolidBrush(dataGridViewResults.RowHeadersDefaultCellStyle.ForeColor);
            e.Graphics.DrawString((e.RowIndex + 1).ToString(),
                                  dataGridViewResults.DefaultCellStyle.Font,
                                  brush,
                                  e.RowBounds.Location.X + 15,
                                  e.RowBounds.Location.Y + 4);
        }

        /// <summary>
        /// DataGridView のセルがクリックされたときの処理を行う
        /// </summary>
        /// <param name="sender">イベントの送信者</param>
        /// <param name="e">セルクリックイベントデータ</param>
        private void dataGridViewResults_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 1)
            {
                return;
            }

            var jsonData = dataGridViewResults.Rows[e.RowIndex].Cells[1].Value?.ToString();
            JsonData.Text = jsonData;

            if (useHyperlinkHandler)
            {
                hyperlinkHandler.MarkLinkTextFromJson(JsonData);
            }

            if (e.ColumnIndex > 1)
            {
                var cellValue = dataGridViewResults.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                richTextBoxSelectedCell.Text = cellValue;

                if (useHyperlinkHandler && (
                    dataGridViewResults.Columns[e.ColumnIndex].HeaderText == "folderName" || dataGridViewResults.Columns[e.ColumnIndex].HeaderText == "fullPath"
                    ))
                {
                    hyperlinkHandler.MarkLinkTextFromText(richTextBoxSelectedCell);
                }
            }
        }

        /// <summary>
        /// RichTextBox のマウスアップイベントを処理する
        /// </summary>
        /// <param name="sender">イベントの送信者</param>
        /// <param name="e">マウスイベントデータ</param>
        private void JsonData_MouseUp(object sender, MouseEventArgs e)
        {
            if (useHyperlinkHandler)
            {
                hyperlinkHandler.HandleMouseUpJson(e, JsonData);
            }
        }

        /// <summary>
        /// RichTextBox のマウスアップイベントを処理する
        /// </summary>
        /// <param name="sender">イベントの送信者</param>
        /// <param name="e">マウスイベントデータ</param>
        private void richTextBoxSelectedCell_MouseUp(object sender, MouseEventArgs e)
        {
            if (useHyperlinkHandler)
            {
                hyperlinkHandler.HandleMouseUpText(e, richTextBoxSelectedCell);
            }
        }
    }
}
