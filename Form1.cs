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
        private readonly string[] systemColumns = { "id", "_etag", "_rid", "_self", "_attachments", "_ts" };

        /// <summary>
        /// �V���� <see cref="Form1"/> �N���X�̃C���X�^���X������������
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            // �t�H�[���̃X�P�[�����O���[�h��ݒ�
            AutoScaleMode = AutoScaleMode.Dpi;

            var configuration = LoadConfiguration();

            useHyperlinkHandler = configuration.GetValue<bool>("AppSettings:EnableHyperlinkHandler");
            maxItemCount = configuration.GetValue<int>("AppSettings:MaxItemCount");
            connectionString = configuration.GetValue<string>("AppSettings:ConnectionString");
            databaseName = configuration.GetValue<string>("AppSettings:DatabaseName");
            containerName = configuration.GetValue<string>("AppSettings:ContainerName");

            textBoxConnectionString.Text = connectionString;
            textBoxDatabaseName.Text = databaseName;
            cmbBoxContainerName.Text = containerName;
            numericUpDownMaxCount.Value = maxItemCount;

            try
            {
                cosmosClient = new CosmosClient(connectionString);
                if (cosmosClient != null && !string.IsNullOrWhiteSpace(databaseName))
                {
                    LoadContainersIntoComboBox(databaseName);
                }
            }
            catch (Exception)
            {
            }

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
            try
            {
                // �}�E�X�J�[�\����ҋ@���ɕύX
                Cursor.Current = Cursors.WaitCursor;

                // Cosmos DB �N���C�A���g��������
                cosmosContainer = InitializeCosmosContainer(textBoxConnectionString.Text, textBoxDatabaseName.Text, cmbBoxContainerName.Text);

                // DatagridView���X�V
                await UpdateDatagridView();

                buttonInsert.Enabled = true;
            }
            finally
            {
                // ����������������}�E�X�J�[�\�������ɖ߂�
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// DatagridView���X�V����
        /// </summary>
        /// <returns>Task</returns>
        private async Task UpdateDatagridView()
        {
            // �f�[�^�̎擾
            var dataTable = await FetchDataFromCosmosDBAsync();
            SetReadOnlyColumns(dataTable);
            dataGridViewResults.DataSource = dataTable;
        }

        /// <summary>
        /// Cosmos DB �R���e�i������������
        /// </summary>
        /// <param name="connectionString">�ڑ�������</param>
        /// <param name="databaseName">DB��</param>
        /// <param name="containerName">�R���e�i��</param>
        /// <returns>Container�C���X�^���X</returns>
        private Container InitializeCosmosContainer(string connectionString, string databaseName, string containerName)
        {
            cosmosClient = new CosmosClient(connectionString);
            return cosmosClient.GetContainer(databaseName, containerName);
        }

        /// <summary>
        /// �w�肳�ꂽ�f�[�^�x�[�X�ɂ���R���e�i�̈ꗗ��ComboBox�Ɋi�[����
        /// </summary>
        /// <param name="databaseId">�f�[�^�x�[�XID</param>
        private async void LoadContainersIntoComboBox(string databaseId)
        {
            try
            {
                var databaseReference = cosmosClient.GetDatabase(databaseId);

                // �R���e�i�ꗗ���擾
                using (var containerIterator = databaseReference.GetContainerQueryIterator<ContainerProperties>())
                {
                    while (containerIterator.HasMoreResults)
                    {
                        foreach (var container in await containerIterator.ReadNextAsync())
                        {
                            // ComboBox�ɃR���e�i����ǉ�
                            cmbBoxContainerName.Items.Add(container.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        /// <summary>
        /// �񓯊��� Cosmos DB ����f�[�^���擾���ADataTable �Ɋi�[����
        /// </summary>
        /// <returns>Cosmos DB ����擾�����f�[�^���܂� <see cref="DataTable"/></returns>
        private async Task<DataTable> FetchDataFromCosmosDBAsync()
        {
            var dataTable = new DataTable();

            try
            {
                var maxCount = GetMaxItemCount();
                var query = BuildQuery(richTextBoxQuery.Text, maxCount);
                await ExecuteCosmosDbQuery(query, maxCount, dataTable);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }

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
        /// <returns>Task</returns>
        private async Task ExecuteCosmosDbQuery(string query, int maxCount, DataTable dataTable)
        {
            var stopwatch = Stopwatch.StartNew();
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
            UpdateStatusStrip(totalRequestCharge, documentCount, pageCount, stopwatch.ElapsedMilliseconds);
            stopwatch.Stop();
        }

        /// <summary>
        /// �N�G����������\�z����
        /// </summary>
        /// <param name="queryText">�N�G���e�L�X�g</param>
        /// <param name="maxCount">�擾����ő�A�C�e����</param>
        /// <returns>�\�z���ꂽ�N�G��������</returns>
        private string BuildQuery(string queryText, int maxCount)
        {
            var query = $"SELECT TOP {maxCount} * FROM c";

            if (!string.IsNullOrWhiteSpace(queryText))
            {
                query = queryText;
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
                if (dataTable.Columns.Count == 0)
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
        /// �ǂݎ���p�ɂ�����ݒ肷��
        /// </summary>
        /// <param name="dataTable">�f�[�^���i�[���� DataTable</param>
        private async void SetReadOnlyColumns(DataTable dataTable)
        {
            try
            {
                var containerProperties = await cosmosContainer.ReadContainerAsync();
                var partitionKeyPaths = containerProperties.Resource.PartitionKeyPaths.Select(p => p.Trim('/')).ToArray();
                var readOnlyColumns = systemColumns.Concat(partitionKeyPaths).ToArray();

                foreach (DataColumn column in dataTable.Columns)
                {
                    if (readOnlyColumns.Contains(column.ColumnName))
                    {
                        dataGridViewResults.Columns[column.ColumnName].ReadOnly = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
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
            if (e.RowIndex < 0)
            {
                return;
            }
            // �I�����ꂽ�s�̑S�ẴJ�����̃f�[�^��JObject�ɕϊ�����
            var jsonObject = new JObject();
            foreach (DataGridViewColumn column in dataGridViewResults.Columns)
            {
                var item = dataGridViewResults.Rows[e.RowIndex].Cells[column.Index].Value?.ToString() ?? string.Empty;
                jsonObject[column.HeaderText] = item;
            }

            // JSON�`���ŕێ�����
            var jsonData = jsonObject.ToString();
            JsonData.Text = jsonData;

            if (useHyperlinkHandler)
            {
                hyperlinkHandler.MarkLinkTextFromJson(JsonData);
            }

            if (e.ColumnIndex > -1)
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

        /// <summary>
        /// ���R�[�h���X�V����
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M��</param>
        /// <param name="e">�C�x���g�f�[�^</param>
        private async void buttonUpdate_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Do you want to update your records?",
                "Info",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // JSON�f�[�^���p�[�X
                var jsonObject = JObject.Parse(JsonData.Text);

                // PartitionKey�������I�ɉ������Ď擾
                var partitionKey = await ResolvePartitionKeyAsync(jsonObject);

                // PartitionKey�ɑΉ�����L�[���ڂ��擾
                string partitionKeyInfo = GetPartitionKeyValues(jsonObject);

                // Cosmos DB��Upsert���������s
                var response = await cosmosContainer.UpsertItemAsync(jsonObject, partitionKey);

                // �������b�Z�[�W��\��
                var id = jsonObject["id"].ToString();
                var message = $"Upsert successful!\n\nId: {id}\nPartitionKey:\n{partitionKeyInfo}\n\nRequest charge: {response.RequestCharge}";
                MessageBox.Show(message);
                await UpdateDatagridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        /// <summary>
        /// ���R�[�h���폜����
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M��</param>
        /// <param name="e">�C�x���g�f�[�^</param>
        private async void buttonDelete_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Do you want to delete your records?",
                "Info",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                // JSON�f�[�^���p�[�X
                var jsonObject = JObject.Parse(JsonData.Text);
                var id = jsonObject["id"].ToString();

                // PartitionKey�������I�ɉ������Ď擾
                var partitionKey = await ResolvePartitionKeyAsync(jsonObject);

                // PartitionKey�ɑΉ�����L�[���ڂ��擾
                var partitionKeyInfo = GetPartitionKeyValues(jsonObject);

                // Cosmos DB��Delete���������s
                var response = await cosmosContainer.DeleteItemAsync<object>(id, partitionKey);

                // �������b�Z�[�W��\��
                var message = $"Delete successful!\n\nId: {id}\nPartitionKey:\n{partitionKeyInfo}\n\nRequest charge: {response.RequestCharge}";
                MessageBox.Show(message);
                await UpdateDatagridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        /// <summary>
        /// �R���e�i��json�̏��Ɋ�Â���PartitionKey���������ACosmosDB�Ɏg�p����PartitionKey���\�z����
        /// </summary>
        /// <param name="jsonObject">�p�[�X���ꂽJSON�I�u�W�F�N�g</param>
        /// <returns>�������ꂽPartitionKey�I�u�W�F�N�g</returns>
        private async System.Threading.Tasks.Task<PartitionKey> ResolvePartitionKeyAsync(JObject jsonObject)
        {
            // �R���e�i�̃v���p�e�B����PartitionKeyPaths���擾
            var containerProperties = await cosmosContainer.ReadContainerAsync();
            var partitionKeyPaths = containerProperties.Resource.PartitionKeyPaths;

            // PartitionKeyBuilder���g�p���ĊK�w�I��PartitionKey���\�z
            var partitionKeyBuilder = new PartitionKeyBuilder();

            // �ePartitionKeyPath�ɑ΂��āAJSON�I�u�W�F�N�g����l���擾���APartitionKeyBuilder�ɒǉ�
            foreach (var path in partitionKeyPaths)
            {
                var key = jsonObject.SelectToken(path.Trim('/'))?.ToString();
                if (key == null)
                {
                    return default(PartitionKey);
                }

                partitionKeyBuilder.Add(key);
            }

            // PartitionKey���\�z���ĕԂ�
            return partitionKeyBuilder.Build();
        }

        /// <summary>
        /// PartitionKey�ɑΉ�����t�B�[���h���ƒl���擾���A���s���ĕ\�����邽�߂̕�������\�z����
        /// </summary>
        /// <param name="jsonObject">�p�[�X���ꂽJSON�I�u�W�F�N�g</param>
        /// <returns>PartitionKey�ɑΉ�����t�B�[���h���ƒl�����s�ŘA������������</returns>
        private string GetPartitionKeyValues(JObject jsonObject)
        {
            var containerProperties = cosmosContainer.ReadContainerAsync().Result;
            var partitionKeyPaths = containerProperties.Resource.PartitionKeyPaths;

            // �t�B�[���h���ƒl�����s�Ō������ĕ\��
            return string.Join("\n", partitionKeyPaths.Select(path =>
            {
                var key = jsonObject.SelectToken(path.Trim('/'))?.ToString();
                return $"{path.Trim('/')}: {key}";
            }));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JsonData_TextChanged(object sender, EventArgs e)
        {
            var jsonData = (RichTextBox)sender;
            buttonUpdate.Enabled = jsonData.Text != null;
            buttonDelete.Enabled = jsonData.Text != null;
        }

        /// <summary>
        /// DataGridView �̃Z���t�H�[�}�b�g���ɌĂяo�����C�x���g�n���h���B�ǂݎ���p�̃J�����ɑ΂��Ĕw�i�F�ƕ����F��ݒ肷��B
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M��</param>
        /// <param name="e">�Z���t�H�[�}�b�g�Ɋւ���C�x���g�f�[�^</param>
        private void dataGridViewResults_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            foreach (DataGridViewColumn column in dataGridViewResults.Columns)
            {
                // ReadOnly�v���p�e�B��true�̃J�����̐F��ύX
                if (column.ReadOnly)
                {
                    dataGridViewResults.Rows[e.RowIndex].Cells[column.Index].Style.BackColor = Color.DarkGray;
                    dataGridViewResults.Rows[e.RowIndex].Cells[column.Index].Style.ForeColor = Color.White;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void buttonInsert_Click(object sender, EventArgs e)
        {
            using (var formInsert = new FormInsert(this.cosmosContainer, JsonData.Text))
            {
                formInsert.ShowDialog();
            }

            await UpdateDatagridView();
        }
    }
}
