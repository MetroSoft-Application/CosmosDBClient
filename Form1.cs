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

            // ���ϐ�����ڑ������擾
            textBox1.Text = Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING");
            textBox2.Text = Environment.GetEnvironmentVariable("COSMOS_DB_NAME");
            textBox3.Text = Environment.GetEnvironmentVariable("COSMOS_FILE_CONTAINER_NAME");
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            // CosmosClient�̏�����
            cosmosClient = new CosmosClient(textBox1.Text);
            cosmosContainer = cosmosClient.GetContainer(textBox2.Text, textBox3.Text);

            // �f�[�^���擾����DataGridView�ɕ\��
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

                // RichTextBox2�̓��e���󔒂łȂ��ꍇ
                if (!string.IsNullOrWhiteSpace(richTextBox2.Text))
                {
                    query = richTextBox2.Text;

                    // �N�G����TOP�傪�܂܂�Ă��Ȃ��ꍇ�i�啶���������̋�ʂȂ��j
                    if (!Regex.IsMatch(query, @"\bSELECT\s+TOP\b", RegexOptions.IgnoreCase))
                    {
                        // SELECT���̌��TOP���ǉ�
                        int selectIndex = Regex.Match(query, @"\bSELECT\b", RegexOptions.IgnoreCase).Index;
                        if (selectIndex != -1)
                        {
                            query = query.Insert(selectIndex + 6, $" TOP {maxCount}");
                        }
                    }
                }

                // �N�G���̒�` (TOP��Ŏ擾�����𐧌�)
                var queryDefinition = new QueryDefinition(query);
                using FeedIterator<dynamic> queryResultSetIterator = cosmosContainer.GetItemQueryIterator<dynamic>(
                    queryDefinition, requestOptions: new QueryRequestOptions { MaxItemCount = maxCount });

                // ���ʂ�DataTable�ɕϊ�
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<dynamic> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    foreach (var item in currentResultSet)
                    {
                        // item��JObject�ɕϊ�
                        JObject jsonObject = JObject.Parse(item.ToString());

                        if (dataTable.Columns.Count == 0)
                        {
                            // �ŏ��̌��ʂ���J�������쐬
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

                        // ���������ɒB������I��
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

            // �I�����ꂽ�Z���̒l���擾
            var cellValue = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
            richTextBox1.Text = cellValue;
        }
    }
}
