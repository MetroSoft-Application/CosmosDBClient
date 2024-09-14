using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace CosmosDBClient
{
    public partial class Form1 : Form
    {
        private CosmosClient cosmosClient;
        private Container cosmosContainer;
        private int MaxItemCount = 1;

        public Form1()
        {
            InitializeComponent();

            // 環境変数から接続情報を取得
            textBox1.Text = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING");
            textBox2.Text = Environment.GetEnvironmentVariable("COSMOS_DB_NAME");
            textBox3.Text = Environment.GetEnvironmentVariable("COSMOS_FILE_CONTAINER_NAME");
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            // CosmosClientの初期化
            cosmosClient = new CosmosClient(textBox1.Text);
            cosmosContainer = cosmosClient.GetContainer(textBox2.Text, textBox3.Text);

            // データを取得してDataGridViewに表示
            DataTable dataTable = await FetchDataFromCosmosDBAsync();
            dataGridView1.DataSource = dataTable;
        }

        private async Task<DataTable> FetchDataFromCosmosDBAsync()
        {
            DataTable dataTable = new DataTable();
            try
            {
                var maxCount = (int)Math.Max(numericUpDown1.Value, MaxItemCount);
                var query = $"SELECT TOP {maxCount} * FROM c";

                // RichTextBox2の内容が空白でない場合
                if (!string.IsNullOrWhiteSpace(richTextBox2.Text))
                {
                    query = richTextBox2.Text;

                    // クエリにTOP句が含まれていない場合（大文字小文字の区別なし）
                    if (!Regex.IsMatch(query, @"\bSELECT\s+TOP\b", RegexOptions.IgnoreCase))
                    {
                        // SELECT文の後にTOP句を追加
                        int selectIndex = Regex.Match(query, @"\bSELECT\b", RegexOptions.IgnoreCase).Index;
                        if (selectIndex != -1)
                        {
                            query = query.Insert(selectIndex + 6, $" TOP {maxCount}");
                        }
                    }
                }

                // クエリの定義 (TOP句で取得件数を制限)
                var queryDefinition = new QueryDefinition(query);
                using FeedIterator<dynamic> queryResultSetIterator = cosmosContainer.GetItemQueryIterator<dynamic>(
                    queryDefinition, requestOptions: new QueryRequestOptions { MaxItemCount = maxCount });

                // 結果をDataTableに変換
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<dynamic> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (var item in currentResultSet)
                    {
                        // itemをJObjectに変換
                        JObject jsonObject = JObject.Parse(item.ToString());

                        if (dataTable.Columns.Count == 0)
                        {
                            // 最初の結果からカラムを作成
                            foreach (var property in jsonObject.Properties())
                            {
                                dataTable.Columns.Add(property.Name);
                            }
                        }

                        DataRow row = dataTable.NewRow();
                        foreach (var property in jsonObject.Properties())
                        {
                            row[property.Name] = property.Value?.ToString() ?? string.Empty;
                        }
                        dataTable.Rows.Add(row);

                        // 件数制限に達したら終了
                        if (dataTable.Rows.Count >= maxCount)
                        {
                            return dataTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return dataTable;
        }

        private void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(dataGridView1.RowHeadersDefaultCellStyle.ForeColor))
            {
                e.Graphics.DrawString((e.RowIndex + 1).ToString(),
                                      dataGridView1.DefaultCellStyle.Font,
                                      brush,
                                      e.RowBounds.Location.X + 15,
                                      e.RowBounds.Location.Y + 4);
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            // 選択されたセルの値を取得
            var cellValue = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
            richTextBox1.Text = cellValue;
        }
    }
}
