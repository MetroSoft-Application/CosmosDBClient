using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace CosmosDBClient
{
    /// <summary>
    /// Cosmos DB �N���C�A���g�̃f�[�^��\������ъǗ����邽�߂̃t�H�[��
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
        /// �V���� <see cref="Form1"/> �N���X�̃C���X�^���X������������
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
        /// �ݒ�t�@�C���Ɗ��ϐ�����A�v���P�[�V�����ݒ��ǂݍ���
        /// </summary>
        /// <returns>�ݒ���܂� <see cref="IConfigurationRoot"/> �I�u�W�F�N�g</returns>
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
        /// Cosmos DB ����f�[�^�����[�h���ADataGridView �ɕ\������
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M��</param>
        /// <param name="e">�C�x���g�̃f�[�^</param>
        private async void buttonLoadData_Click(object sender, EventArgs e)
        {
            InitializeCosmosClient(textBoxConnectionString.Text,textBoxDatabaseName.Text,textBoxContainerName.Text);
            var dataTable = await FetchDataFromCosmosDBAsync();
            AddHiddenJsonColumnIfNeeded();
            dataGridViewResults.DataSource = dataTable;
            dataGridViewResults.Columns[1].Visible = false;
        }

        /// <summary>
        /// Cosmos DB �N���C�A���g������������
        /// </summary>
        /// <param name="connectionString">�ڑ�������</param>
        /// <param name="databaseName">DB��</param>
        /// <param name="containerName">�R���e�i��</param>
        private void InitializeCosmosClient(string connectionString,string databaseName,string containerName)
        {
            cosmosClient = new CosmosClient(connectionString);
            cosmosContainer = cosmosClient.GetContainer(databaseName, containerName);
        }

        /// <summary>
        /// DataGridView �ɉB�����ǉ�����
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
        /// �񓯊��� Cosmos DB ����f�[�^���擾���ADataTable �Ɋi�[����
        /// </summary>
        /// <returns>Cosmos DB ����擾�����f�[�^���܂� <see cref="DataTable"/></returns>
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
        /// DataTable ���쐬���A�����ݒ���s��
        /// </summary>
        /// <returns>���������ꂽ <see cref="DataTable"/></returns>
        private DataTable CreateDataTable()
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add("JsonData", typeof(string));
            return dataTable;
        }

        /// <summary>
        /// �ő�A�C�e�������擾����
        /// </summary>
        /// <returns>�ő�A�C�e����</returns>
        private int GetMaxItemCount()
        {
            return Math.Max((int)numericUpDownMaxCount.Value, maxItemCount);
        }

        /// <summary>
        /// �񓯊��� Cosmos DB �̃N�G�������s����
        /// </summary>
        /// <param name="query">���s����N�G��</param>
        /// <param name="maxCount">�擾����ő�A�C�e����</param>
        /// <param name="dataTable">�f�[�^���i�[���� DataTable</param>
        /// <returns>�N�G�����s��̓��v��� (���N�G�X�g�`���[�W�A�h�L�������g���A�y�[�W��)</returns>
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
        /// �N�G����������\�z����
        /// </summary>
        /// <param name="maxCount">�擾����ő�A�C�e����</param>
        /// <returns>�\�z���ꂽ�N�G��������</returns>
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
        /// �N�G�����ʂ��������ADataTable �ɒǉ�����
        /// </summary>
        /// <param name="resultSet">�N�G�����ʂ̃Z�b�g</param>
        /// <param name="dataTable">�f�[�^���i�[���� DataTable</param>
        /// <param name="maxCount">�ő�A�C�e����</param>
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
        /// JSON�I�u�W�F�N�g���� DataTable �ɗ��ǉ�����
        /// </summary>
        /// <param name="jsonObject">JSON �I�u�W�F�N�g</param>
        /// <param name="dataTable">�f�[�^���i�[���� DataTable</param>
        private void AddColumnsToDataTable(JObject jsonObject, DataTable dataTable)
        {
            foreach (var property in jsonObject.Properties())
            {
                dataTable.Columns.Add(property.Name);
            }
        }

        /// <summary>
        /// JSON�I�u�W�F�N�g���� DataTable �ɍs��ǉ�����
        /// </summary>
        /// <param name="jsonObject">JSON �I�u�W�F�N�g</param>
        /// <param name="dataTable">�f�[�^���i�[���� DataTable</param>
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
        /// �X�e�[�^�X�X�g���b�v���X�V����
        /// </summary>
        /// <param name="totalRequestCharge">�����N�G�X�g�`���[�W</param>
        /// <param name="documentCount">�h�L�������g��</param>
        /// <param name="pageCount">�y�[�W��</param>
        /// <param name="elapsedMilliseconds">�o�ߎ��� (�~���b)</param>
        private void UpdateStatusStrip(double totalRequestCharge, int documentCount, int pageCount, long elapsedMilliseconds)
        {
            toolStripStatusLabel1.Text = $"Total RU: {totalRequestCharge:F2}";
            toolStripStatusLabel2.Text = $"Documents: {documentCount}";
            toolStripStatusLabel3.Text = $"Pages: {pageCount}";
            toolStripStatusLabel4.Text = $"Elapsed Time: {elapsedMilliseconds} ms";
        }

        /// <summary>
        /// DataGridView �̍s���`�悳�ꂽ��̏������s��
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M��</param>
        /// <param name="e">�s�̕`��C�x���g�f�[�^</param>
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
        /// DataGridView �̃Z�����N���b�N���ꂽ�Ƃ��̏������s��
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M��</param>
        /// <param name="e">�Z���N���b�N�C�x���g�f�[�^</param>
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
        /// RichTextBox �̃}�E�X�A�b�v�C�x���g����������
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M��</param>
        /// <param name="e">�}�E�X�C�x���g�f�[�^</param>
        private void JsonData_MouseUp(object sender, MouseEventArgs e)
        {
            if (useHyperlinkHandler)
            {
                hyperlinkHandler.HandleMouseUpJson(e, JsonData);
            }
        }

        /// <summary>
        /// RichTextBox �̃}�E�X�A�b�v�C�x���g����������
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M��</param>
        /// <param name="e">�}�E�X�C�x���g�f�[�^</param>
        private void richTextBoxSelectedCell_MouseUp(object sender, MouseEventArgs e)
        {
            if (useHyperlinkHandler)
            {
                hyperlinkHandler.HandleMouseUpText(e, richTextBoxSelectedCell);
            }
        }
    }
}
