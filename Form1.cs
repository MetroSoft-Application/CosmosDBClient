using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace CosmosDBClient
{
    public partial class Form1 : Form
    {
        private CosmosClient cosmosClient;
        private Container cosmosContainer;
        private readonly int MaxItemCount = 1;

        public Form1()
        {
            InitializeComponent();
            LoadEnvironmentVariables();
        }

        private void LoadEnvironmentVariables()
        {
            textBoxConnectionString.Text = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING");
            textBoxDatabaseName.Text = Environment.GetEnvironmentVariable("COSMOS_DB_NAME");
            textBoxContainerName.Text = Environment.GetEnvironmentVariable("COSMOS_FILE_CONTAINER_NAME");
        }

        private async void buttonLoadData_Click(object sender, EventArgs e)
        {
            InitializeCosmosClient();
            var dataTable = await FetchDataFromCosmosDBAsync();

            // DataGridViewにデータをバインドする前に隠し列を追加
            if (!dataGridViewResults.Columns.Contains("JsonData"))
            {
                DataGridViewTextBoxColumn jsonColumn = new DataGridViewTextBoxColumn
                {
                    Name = "JsonData",
                    HeaderText = "JsonData",
                    Visible = false
                };
                dataGridViewResults.Columns.Add(jsonColumn);
            }

            dataGridViewResults.DataSource = dataTable;
        }

        private void InitializeCosmosClient()
        {
            cosmosClient = new CosmosClient(textBoxConnectionString.Text);
            cosmosContainer = cosmosClient.GetContainer(textBoxDatabaseName.Text, textBoxContainerName.Text);
        }

        private async Task<DataTable> FetchDataFromCosmosDBAsync()
        {
            var dataTable = new DataTable();
            var totalRequestCharge = 0d;
            var documentCount = 0;
            var pageCount = 0;

            try
            {
                var maxCount = (int)Math.Max(numericUpDownMaxCount.Value, MaxItemCount);
                var query = BuildQuery(maxCount);

                // データテーブルの列を初期化
                dataTable.Columns.Add("JsonData", typeof(string));

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var queryDefinition = new QueryDefinition(query);
                using var queryResultSetIterator = cosmosContainer.GetItemQueryIterator<dynamic>(
                    queryDefinition, requestOptions: new QueryRequestOptions { MaxItemCount = maxCount });
                while (queryResultSetIterator.HasMoreResults)
                {
                    var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    pageCount++;

                    // 現在のリクエストにかかったRUを加算
                    totalRequestCharge += currentResultSet.RequestCharge;

                    // 取得したドキュメントの数を加算
                    documentCount += currentResultSet.Count;

                    // Diagnosticsからクエリメトリクスを取得
                    var diagnostics = currentResultSet.Diagnostics.ToString();

                    ProcessQueryResults(currentResultSet, dataTable, maxCount);
                }

                stopwatch.Stop();
                var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                // StatusStripに情報を表示
                toolStripStatusLabel1.Text = $"Total RU:{totalRequestCharge:F2}";
                toolStripStatusLabel2.Text = $"Documents:{documentCount}";
                toolStripStatusLabel3.Text = $"Pages:{pageCount}";
                toolStripStatusLabel4.Text = $"Elapsed Time:{elapsedMilliseconds} ms";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return dataTable;
        }

        private string BuildQuery(int maxCount)
        {
            var query = $"SELECT TOP {maxCount} * FROM c";

            if (!string.IsNullOrWhiteSpace(richTextBoxQuery.Text))
            {
                query = richTextBoxQuery.Text;
                if (!Regex.IsMatch(query, @"\bSELECT\s+TOP\b", RegexOptions.IgnoreCase))
                {
                    var selectIndex = Regex.Match(query, @"\bSELECT\b", RegexOptions.IgnoreCase).Index;
                    if (selectIndex != -1)
                    {
                        query = query.Insert(selectIndex + 6, $" TOP {maxCount}");
                    }
                }
            }

            return query;
        }

        private void ProcessQueryResults(FeedResponse<dynamic> resultSet, DataTable dataTable, int maxCount)
        {
            foreach (var item in resultSet)
            {
                var jsonObject = JObject.Parse(item.ToString());

                if (dataTable.Columns.Count == 1)
                {
                    AddColumnsToDataTable(jsonObject, dataTable);
                }

                var row = dataTable.NewRow();
                foreach (var property in jsonObject.Properties())
                {
                    row[property.Name] = property.Value?.ToString() ?? string.Empty;
                }

                // JSONデータを隠し列に格納
                row["JsonData"] = jsonObject.ToString();

                dataTable.Rows.Add(row);

                if (dataTable.Rows.Count >= maxCount)
                {
                    return;
                }
            }
        }

        private void AddColumnsToDataTable(JObject jsonObject, DataTable dataTable)
        {
            foreach (var property in jsonObject.Properties())
            {
                dataTable.Columns.Add(property.Name);
            }
        }

        private void AddRowToDataTable(JObject jsonObject, DataTable dataTable)
        {
            var row = dataTable.NewRow();
            foreach (var property in jsonObject.Properties())
            {
                row[property.Name] = property.Value?.ToString() ?? string.Empty;
            }
            dataTable.Rows.Add(row);
        }

        private void dataGridViewResults_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            using var brush = new SolidBrush(dataGridViewResults.RowHeadersDefaultCellStyle.ForeColor);
            e.Graphics.DrawString((e.RowIndex + 1).ToString(),
                                  dataGridViewResults.DefaultCellStyle.Font,
                                  brush,
                                  e.RowBounds.Location.X + 15,
                                  e.RowBounds.Location.Y + 4);
        }

        private void dataGridViewResults_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 1)
                return;

            // JSONデータの取得
            var jsonData = dataGridViewResults.Rows[e.RowIndex].Cells[1].Value?.ToString();
            JsonData.Text = jsonData;

            if (e.ColumnIndex > 1)
            {
                var cellValue = dataGridViewResults.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                richTextBoxSelectedCell.Text = cellValue;
            }
        }
    }
}
