using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace CosmosDBClient
{
    /// <summary>
    /// Cosmos DB �N���C�A���g�̃f�[�^��\������ъǗ�����t�H�[���N���X
    /// </summary>
    public partial class FormMain : Form
    {
        private CosmosDBService _cosmosDBService;
        private readonly int _maxItemCount;
        private readonly bool _useHyperlinkHandler;
        private HyperlinkHandler _hyperlinkHandler;

        /// <summary>
        /// FormMain �N���X�̃R���X�g���N�^�ݒ��ǂݍ��݁ACosmosDBService�̃C���X�^���X������������
        /// </summary>
        public FormMain()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;

            var configuration = LoadConfiguration();
            _useHyperlinkHandler = configuration.GetValue<bool>("AppSettings:EnableHyperlinkHandler");
            _maxItemCount = configuration.GetValue<int>("AppSettings:MaxItemCount");
            var connectionString = configuration.GetValue<string>("AppSettings:ConnectionString");
            var databaseName = configuration.GetValue<string>("AppSettings:DatabaseName");
            var containerName = configuration.GetValue<string>("AppSettings:ContainerName");

            textBoxConnectionString.Text = connectionString;
            textBoxDatabaseName.Text = databaseName;
            cmbBoxContainerName.Text = containerName;
            numericUpDownMaxCount.Value = _maxItemCount;

            _cosmosDBService = new CosmosDBService(textBoxConnectionString.Text, textBoxDatabaseName.Text, cmbBoxContainerName.Text);

            if (_useHyperlinkHandler)
            {
                _hyperlinkHandler = new HyperlinkHandler();
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    LoadContainersIntoComboBox(databaseName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        /// <summary>
        /// �ݒ�t�@�C���Ɗ��ϐ�����A�v���P�[�V�����ݒ��ǂݍ���
        /// </summary>
        /// <returns>�ݒ�����܂� IConfigurationRoot �I�u�W�F�N�g</returns>
        private IConfigurationRoot LoadConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return configuration;
        }

        /// <summary>
        /// �{�^���N���b�N���Ƀf�[�^�����[�h���ADataGridView �ɕ\������
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M���I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g�f�[�^</param>
        private async void buttonLoadData_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                _cosmosDBService = new CosmosDBService(textBoxConnectionString.Text, textBoxDatabaseName.Text, cmbBoxContainerName.Text);
                await UpdateDatagridView();

                ResizeRowHeader();

                buttonInsert.Enabled = true;
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// �s�w�b�_�[�̃T�C�Y�𒲐�
        /// </summary>
        private void ResizeRowHeader()
        {
            using (Graphics g = dataGridViewResults.CreateGraphics())
            {
                // �e�L�X�g�̕����v�Z
                var size = g.MeasureString(cmbBoxContainerName.Text, dataGridViewResults.Font);

                // �ő啝��ێ�
                var maxRowHeaderWidth = (int)size.Width - 20;

                // �v�Z�����ő啝�� RowHeadersWidth �ɐݒ�
                dataGridViewResults.RowHeadersWidth = maxRowHeaderWidth;
            }
        }

        /// <summary>
        /// Cosmos DB ����f�[�^���擾���ADataGridView �ɕ\������
        /// </summary>
        /// <returns>�񓯊��� Task</returns>
        private async Task UpdateDatagridView()
        {
            var query = BuildQuery(richTextBoxQuery.Text, GetMaxItemCount());
            var (dataTable, totalRequestCharge, documentCount, pageCount, elapsedMilliseconds) =
                await _cosmosDBService.FetchDataWithStatusAsync(query, GetMaxItemCount());

            SetReadOnlyColumns(dataTable);
            dataGridViewResults.DataSource = dataTable;

            UpdateStatusStrip(totalRequestCharge, documentCount, pageCount, elapsedMilliseconds);
        }

        /// <summary>
        /// �N�G����������\�z
        /// �w�肳�ꂽ�ő匏���Ɋ�Â��āATOP���}��
        /// </summary>
        /// <param name="queryText">�N�G��������</param>
        /// <param name="maxCount">�擾����ő�A�C�e����</param>
        /// <returns>�\�z���ꂽ�N�G��������</returns>
        private string BuildQuery(string queryText, int maxCount)
        {
            var query = $"SELECT TOP {maxCount} * FROM c";

            if (!string.IsNullOrWhiteSpace(queryText))
            {
                query = queryText;
                if (!System.Text.RegularExpressions.Regex.IsMatch(query, @"\bSELECT\s+TOP\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    var selectIndex = System.Text.RegularExpressions.Regex.Match(query, @"\bSELECT\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Index;
                    if (selectIndex != -1)
                    {
                        query = query.Insert(selectIndex + 6, $" TOP {maxCount}");
                    }
                }
            }

            return query;
        }

        /// <summary>
        /// �ő�A�C�e�������擾����
        /// </summary>
        /// <returns>�ő�A�C�e����</returns>
        private int GetMaxItemCount()
        {
            return Math.Max((int)numericUpDownMaxCount.Value, _maxItemCount);
        }

        /// <summary>
        /// �ǂݎ���p�̗��ݒ�
        /// �p�[�e�B�V�����L�[�̗���ǂݎ���p�ɐݒ�
        /// </summary>
        /// <param name="dataTable">�\������f�[�^���i�[����DataTable</param>
        private async void SetReadOnlyColumns(DataTable dataTable)
        {
            try
            {
                var containerProperties = await _cosmosDBService.GetContainerPropertiesAsync();
                var partitionKeyPaths = containerProperties.PartitionKeyPaths.Select(p => p.Trim('/')).ToArray();
                var readOnlyColumns = _cosmosDBService.systemColumns.Concat(partitionKeyPaths).ToArray();

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
        /// �R���{�{�b�N�X�Ƀf�[�^�x�[�X�̃R���e�i�ꗗ��ǂݍ���
        /// </summary>
        /// <param name="databaseId">�Ώۂ̃f�[�^�x�[�XID</param>
        private async void LoadContainersIntoComboBox(string databaseId)
        {
            try
            {
                var containerNames = await _cosmosDBService.GetContainerNamesAsync();
                cmbBoxContainerName.Items.AddRange(containerNames.ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        /// <summary>
        /// �X�e�[�^�X�o�[���X�V����
        /// ���N�G�X�g�`���[�W�A�h�L�������g���A�y�[�W���A�o�ߎ��Ԃ��\�������
        /// </summary>
        /// <param name="totalRequestCharge">�����N�G�X�g�`���[�W (RU)</param>
        /// <param name="documentCount">�擾�����h�L�������g��</param>
        /// <param name="pageCount">�y�[�W��</param>
        /// <param name="elapsedMilliseconds">�o�ߎ��ԁi�~���b�j</param>
        private void UpdateStatusStrip(double totalRequestCharge, int documentCount, int pageCount, long elapsedMilliseconds)
        {
            toolStripStatusLabel1.Text = $"Total RU: {totalRequestCharge:F2}";
            toolStripStatusLabel2.Text = $"Documents: {documentCount}";
            toolStripStatusLabel3.Text = $"Pages: {pageCount}";
            toolStripStatusLabel4.Text = $"Elapsed Time: {elapsedMilliseconds} ms";
        }

        /// <summary>
        /// ���R�[�h���X�V����
        /// ���[�U�[�m�F��ɁACosmos DB �փA�b�v�T�[�g�i�X�V�܂��͑}���j�����
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M���I�u�W�F�N�g</param>
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

            var jsonObject = default(JObject);

            try
            {
                // JSON�f�[�^���p�[�X
                jsonObject = JObject.Parse(JsonData.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            try
            {
                // JSON�I�u�W�F�N�g����id���擾
                var id = jsonObject["id"].ToString();

                // PartitionKey�������I�ɉ������Ď擾
                var partitionKey = await _cosmosDBService.ResolvePartitionKeyAsync(jsonObject);

                // PartitionKey�ɑΉ�����L�[���ڂ��擾
                string partitionKeyInfo = _cosmosDBService.GetPartitionKeyValues(jsonObject);

                // Cosmos DB��Upsert���������s
                var response = await _cosmosDBService.UpsertItemAsync(jsonObject, partitionKey);

                var message = $"Upsert successful!\n\nId:{id}\nPartitionKey:\n{partitionKeyInfo}\n\nRequest charge:{response.RequestCharge}";
                MessageBox.Show(message, "Info");
                await UpdateDatagridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        /// <summary>
        /// ���R�[�h���폜����
        /// ���[�U�[�m�F��ACosmos DB ����Y�����R�[�h���폜�����
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M���I�u�W�F�N�g</param>
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

            var jsonObject = default(JObject);

            try
            {
                // JSON�f�[�^���p�[�X
                jsonObject = JObject.Parse(JsonData.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            try
            {
                // JSON�I�u�W�F�N�g����id���擾
                var id = jsonObject["id"].ToString();

                // PartitionKey�������I�ɉ������Ď擾
                var partitionKey = await _cosmosDBService.ResolvePartitionKeyAsync(jsonObject);

                // PartitionKey�ɑΉ�����L�[���ڂ��擾
                string partitionKeyInfo = _cosmosDBService.GetPartitionKeyValues(jsonObject);

                var response = await _cosmosDBService.DeleteItemAsync<object>(id, partitionKey);

                var message = $"Delete successful!\n\nId:{id}\nPartitionKey:\n{partitionKeyInfo}\n\nRequest charge:{response.RequestCharge}";
                MessageBox.Show(message, "Info");
                await UpdateDatagridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        /// <summary>
        /// ���R�[�h��}�����郆�[�U�[���f�[�^����͂��A�V�������R�[�h�� Cosmos DB �ɒǉ������
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M���I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g�f�[�^</param>
        private async void buttonInsert_Click(object sender, EventArgs e)
        {
            using (var formInsert = new FormInsert(_cosmosDBService, JsonData.Text))
            {
                formInsert.ShowDialog();
            }

            await UpdateDatagridView();
        }

        /// <summary>
        /// DataGridView �̍s�`���ɍs�ԍ���\�����邽�߂̏���
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M���I�u�W�F�N�g</param>
        /// <param name="e">�s�`��C�x���g�f�[�^</param>
        private void dataGridViewResults_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            using var brush = new System.Drawing.SolidBrush(dataGridViewResults.RowHeadersDefaultCellStyle.ForeColor);
            e.Graphics.DrawString((e.RowIndex + 1).ToString(),
                                  dataGridViewResults.DefaultCellStyle.Font,
                                  brush,
                                  e.RowBounds.Location.X + 15,
                                  e.RowBounds.Location.Y + 4);
        }

        /// <summary>
        /// DataGridView �̃Z�����N���b�N���ꂽ�ۂɁA���̃Z���̃f�[�^��\������
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M���I�u�W�F�N�g</param>
        /// <param name="e">�Z���N���b�N�C�x���g�f�[�^</param>
        private void dataGridViewResults_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            var jsonObject = new JObject();
            foreach (DataGridViewColumn column in dataGridViewResults.Columns)
            {
                var item = dataGridViewResults.Rows[e.RowIndex].Cells[column.Index].Value?.ToString() ?? string.Empty;
                jsonObject[column.HeaderText] = item;
            }

            JsonData.Text = jsonObject.ToString();

            if (_useHyperlinkHandler)
            {
                _hyperlinkHandler.MarkLinkTextFromJson(JsonData);
            }

            if (e.ColumnIndex > -1)
            {
                var cellValue = dataGridViewResults.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                richTextBoxSelectedCell.Text = cellValue;

                if (_useHyperlinkHandler && (
                    dataGridViewResults.Columns[e.ColumnIndex].HeaderText == "folderName" || dataGridViewResults.Columns[e.ColumnIndex].HeaderText == "fullPath"
                    ))
                {
                    _hyperlinkHandler.MarkLinkTextFromText(richTextBoxSelectedCell);
                }
            }
        }

        /// <summary>
        /// RichTextBox �Ń}�E�X�A�b�v�C�x���g�����������ۂ̏����n�C�p�[�����N����������
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M���I�u�W�F�N�g</param>
        /// <param name="e">�}�E�X�C�x���g�f�[�^</param>
        private void JsonData_MouseUp(object sender, MouseEventArgs e)
        {
            if (_useHyperlinkHandler)
            {
                _hyperlinkHandler.HandleMouseUpJson(e, JsonData);
            }
        }

        /// <summary>
        /// RichTextBox �ł̃}�E�X�A�b�v�C�x���g���������郊���N���܂܂�Ă���ꍇ�A�����N����������
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M���I�u�W�F�N�g</param>
        /// <param name="e">�}�E�X�C�x���g�f�[�^</param>
        private void richTextBoxSelectedCell_MouseUp(object sender, MouseEventArgs e)
        {
            if (_useHyperlinkHandler)
            {
                _hyperlinkHandler.HandleMouseUpText(e, richTextBoxSelectedCell);
            }
        }

        /// <summary>
        /// DataGridView �̃Z���t�H�[�}�b�g���ɁA�ǂݎ���p�̃J�����ɔw�i�F�ƕ����F��ݒ肷�鏈��
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M���I�u�W�F�N�g</param>
        /// <param name="e">�Z���t�H�[�}�b�g�C�x���g�f�[�^</param>
        private void dataGridViewResults_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            foreach (DataGridViewColumn column in dataGridViewResults.Columns)
            {
                if (column.ReadOnly)
                {
                    dataGridViewResults.Rows[e.RowIndex].Cells[column.Index].Style.BackColor = System.Drawing.Color.DarkGray;
                    dataGridViewResults.Rows[e.RowIndex].Cells[column.Index].Style.ForeColor = System.Drawing.Color.White;
                }
            }
        }

        /// <summary>
        /// RichTextBox �̓��e���ύX���ꂽ�ۂɁA�{�^���̗L����Ԃ��X�V����
        /// </summary>
        /// <param name="sender">�C�x���g�̑��M���I�u�W�F�N�g</param>
        /// <param name="e">�C�x���g�f�[�^</param>
        private void JsonData_TextChanged(object sender, EventArgs e)
        {
            var jsonData = (RichTextBox)sender;
            buttonUpdate.Enabled = !string.IsNullOrWhiteSpace(jsonData.Text);
            buttonDelete.Enabled = !string.IsNullOrWhiteSpace(jsonData.Text);
        }
    }
}
