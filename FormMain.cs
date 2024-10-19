using System.Collections.ObjectModel;
using System.Data;
using FastColoredTextBoxNS;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosmosDBClient
{
    /// <summary>
    /// Cosmos DB クライアントのデータを表示および管理するフォームクラス
    /// </summary>
    public partial class FormMain : Form
    {
        private CosmosDBService _cosmosDBService;
        private readonly int _maxItemCount;
        private readonly bool _useHyperlinkHandler;
        private HyperlinkHandler _hyperlinkHandler;
        private FastColoredTextBox _textBoxQuery;
        private FastColoredTextBox _jsonData;

        /// <summary>
        /// FormMain クラスのコンストラクタ設定を読み込み、CosmosDBServiceのインスタンスを初期化する
        /// </summary>
        public FormMain()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;

            _textBoxQuery = new FastColoredTextBox();
            _textBoxQuery.Language = Language.SQL;
            _textBoxQuery.Dock = DockStyle.Fill;
            _textBoxQuery.ImeMode = ImeMode.Hiragana;
            _textBoxQuery.BorderStyle = BorderStyle.Fixed3D;
            _textBoxQuery.Text = "SELECT\n    * \nFROM\n    c \nWHERE\n    1 = 1";
            _textBoxQuery.TabLength = 4;
            _textBoxQuery.WordWrap = false;
            _textBoxQuery.ShowLineNumbers = true;
            panel1.Controls.Add(_textBoxQuery);

            _jsonData = new FastColoredTextBox();
            _jsonData.Language = Language.JSON;
            _jsonData.Dock = DockStyle.Fill;
            _jsonData.ImeMode = ImeMode.Hiragana;
            _jsonData.BorderStyle = BorderStyle.Fixed3D;
            _jsonData.BackColor = SystemColors.ButtonFace;
            _jsonData.Font = new Font("Yu Gothic UI", 9);
            _jsonData.WordWrap = true;
            _jsonData.ShowLineNumbers = false;
            _jsonData.ReadOnly = true;
            _jsonData.TextChanged += JsonData_TextChanged;
            splitContainer3.Panel1.Controls.Add(_jsonData);

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

            if (_useHyperlinkHandler)
            {
                _hyperlinkHandler = new HyperlinkHandler();
            }

            try
            {
                _cosmosDBService = new CosmosDBService(textBoxConnectionString.Text, textBoxDatabaseName.Text, cmbBoxContainerName.Text);
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    LoadContainersIntoComboBox(databaseName);
                    DisplayContainerSettings();
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 設定ファイルと環境変数からアプリケーション設定を読み込む
        /// </summary>
        /// <returns>設定情報を含む IConfigurationRoot オブジェクト</returns>
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
        /// ボタンクリック時にデータをロードし、DataGridView に表示する
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private async void buttonLoadData_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                _cosmosDBService = new CosmosDBService(textBoxConnectionString.Text, textBoxDatabaseName.Text, cmbBoxContainerName.Text);
                await UpdateDatagridView();
                DisplayContainerSettings();
                _jsonData.Text = string.Empty;

                ResizeRowHeader();

                buttonInsert.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// 行ヘッダーのサイズを調整
        /// </summary>
        private void ResizeRowHeader()
        {
            using (Graphics g = dataGridViewResults.CreateGraphics())
            {
                // テキストの幅を計算
                var size = g.MeasureString(dataGridViewResults.RowCount.ToString(), dataGridViewResults.Font);

                // 最大幅を保持
                var maxRowHeaderWidth = (int)size.Width + 20;

                // 計算した最大幅を RowHeadersWidth に設定
                dataGridViewResults.RowHeadersWidth = maxRowHeaderWidth;
            }
        }

        /// <summary>
        /// Cosmos DB からデータを取得し、DataGridView に表示する
        /// </summary>
        /// <returns>非同期の Task</returns>
        private async Task UpdateDatagridView()
        {
            var query = BuildQuery(_textBoxQuery.Text, GetMaxItemCount());
            var (dataTable, totalRequestCharge, documentCount, pageCount, elapsedMilliseconds) =
                await _cosmosDBService.FetchDataWithStatusAsync(query, GetMaxItemCount());

            dataGridViewResults.DataSource = null;
            SetReadOnlyColumns(dataTable);
            dataGridViewResults.DataSource = dataTable;

            UpdateStatusStrip(totalRequestCharge, documentCount, pageCount, elapsedMilliseconds);
        }

        /// <summary>
        /// クエリ文字列を構築
        /// 指定された最大件数に基づいて、TOP句を挿入
        /// </summary>
        /// <param name="queryText">クエリ文字列</param>
        /// <param name="maxCount">取得する最大アイテム数</param>
        /// <returns>構築されたクエリ文字列</returns>
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
        /// 最大アイテム数を取得する
        /// </summary>
        /// <returns>最大アイテム数</returns>
        private int GetMaxItemCount()
        {
            return (int)numericUpDownMaxCount.Value;
        }

        /// <summary>
        /// 読み取り専用の列を設定
        /// パーティションキーの列も読み取り専用に設定
        /// </summary>
        /// <param name="dataTable">表示するデータを格納したDataTable</param>
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
        /// コンボボックスにデータベースのコンテナ一覧を読み込む
        /// </summary>
        /// <param name="databaseId">対象のデータベースID</param>
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
        /// コンテナの設定を表示する
        /// </summary>
        private async void DisplayContainerSettings()
        {
            try
            {
                var containerProperties = await _cosmosDBService.GetContainerPropertiesAsync();

                foreach (TabPage tab in tabControl1.TabPages)
                {
                    foreach (Control control in tab.Controls)
                    {
                        switch (control.Name)
                        {
                            case "txtPartitionKey":
                                control.Text = string.Join(",", containerProperties.PartitionKeyPaths);
                                break;

                            case "txtUniqueKey":
                                control.Text = string.Join(",", containerProperties.UniqueKeyPolicy.UniqueKeys.FirstOrDefault()?.Paths ?? new Collection<string>());
                                break;

                            case "radioTimeToLiveOff":
                                {
                                    if (containerProperties.DefaultTimeToLive == null)
                                    {
                                        var radio = (RadioButton)control;
                                        radio.Checked = true;
                                    }
                                }

                                break;

                            case "radioTimeToLiveOn":
                                {
                                    if (containerProperties.DefaultTimeToLive != null)
                                    {
                                        var radio = (RadioButton)control;
                                        radio.Checked = true;
                                    }
                                }

                                break;

                            case "nupTimeToLiveSeconds":
                                {
                                    if (containerProperties.DefaultTimeToLive != null)
                                    {
                                        control.Text = containerProperties.DefaultTimeToLive.ToString();
                                        control.Visible = true;
                                    }
                                    else
                                    {
                                        control.Visible = false;
                                    }
                                }

                                break;

                            case "txtIndexingPolicy":
                                control.Text = JsonConvert.SerializeObject(containerProperties.IndexingPolicy, Formatting.Indented);
                                break;

                            default:
                                break;
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
        /// ステータスバーを更新する
        /// リクエストチャージ、ドキュメント数、ページ数、経過時間が表示される
        /// </summary>
        /// <param name="totalRequestCharge">総リクエストチャージ (RU)</param>
        /// <param name="documentCount">取得したドキュメント数</param>
        /// <param name="pageCount">ページ数</param>
        /// <param name="elapsedMilliseconds">経過時間（ミリ秒）</param>
        private void UpdateStatusStrip(double totalRequestCharge, int documentCount, int pageCount, long elapsedMilliseconds)
        {
            toolStripStatusLabel1.Text = $"Total RU: {totalRequestCharge:F2}";
            toolStripStatusLabel2.Text = $"Documents: {documentCount}";
            toolStripStatusLabel3.Text = $"Pages: {pageCount}";
            toolStripStatusLabel4.Text = $"Elapsed Time: {elapsedMilliseconds} ms";
        }

        /// <summary>
        /// レコードを更新する
        /// ユーザー確認後に、Cosmos DB へアップサート（更新または挿入）される
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
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
                // JSONデータをパース
                jsonObject = JObject.Parse(_jsonData.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            try
            {
                // JSONオブジェクトからidを取得
                var id = jsonObject["id"].ToString();

                // PartitionKeyを自動的に解決して取得
                var partitionKey = await _cosmosDBService.ResolvePartitionKeyAsync(jsonObject);

                // PartitionKeyに対応するキー項目を取得
                string partitionKeyInfo = _cosmosDBService.GetPartitionKeyValues(jsonObject);

                // Cosmos DBにUpsert処理を実行
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
        /// レコードを削除する
        /// ユーザー確認後、Cosmos DB から該当レコードが削除される
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private async void buttonDelete_Click(object sender, EventArgs e)
        {
            await Delete();
        }

        /// <summary>
        /// レコードを削除する
        /// </summary>
        /// <returns>Task</returns>
        private async Task Delete()
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
                // JSONデータをパース
                jsonObject = JObject.Parse(_jsonData.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return;
            }

            try
            {
                // JSONオブジェクトからidを取得
                var id = jsonObject["id"].ToString();

                // PartitionKeyを自動的に解決して取得
                var partitionKey = await _cosmosDBService.ResolvePartitionKeyAsync(jsonObject);

                // PartitionKeyに対応するキー項目を取得
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
        /// レコードを挿入するユーザーがデータを入力し、新しいレコードが Cosmos DB に追加される
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private async void buttonInsert_Click(object sender, EventArgs e)
        {
            using (var formInsert = new FormInsert(_cosmosDBService, _jsonData.Text))
            {
                formInsert.ShowDialog();
            }

            await UpdateDatagridView();
        }

        /// <summary>
        /// DataGridView の行描画後に行番号を表示するための処理
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">行描画イベントデータ</param>
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
        /// DataGridView のセルがクリックされた際に、そのセルのデータを表示する
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">セルクリックイベントデータ</param>
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

            _jsonData.Text = jsonObject.ToString();

            //if (_useHyperlinkHandler)
            //{
            //    _hyperlinkHandler.MarkLinkTextFromJson(JsonData);
            //}

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
        /// RichTextBox でのマウスアップイベントを処理するリンクが含まれている場合、リンクを処理する
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">マウスイベントデータ</param>
        private void richTextBoxSelectedCell_MouseUp(object sender, MouseEventArgs e)
        {
            if (_useHyperlinkHandler)
            {
                _hyperlinkHandler.HandleMouseUpText(e, richTextBoxSelectedCell);
            }
        }

        /// <summary>
        /// DataGridView のセルフォーマット時に、読み取り専用のカラムに背景色と文字色を設定する処理
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">セルフォーマットイベントデータ</param>
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
        /// RichTextBox の内容が変更された際に、ボタンの有効状態を更新する
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private void JsonData_TextChanged(object sender, EventArgs e)
        {
            var jsonData = (FastColoredTextBox)sender;
            buttonUpdate.Enabled = !string.IsNullOrWhiteSpace(jsonData.Text);
            buttonDelete.Enabled = !string.IsNullOrWhiteSpace(jsonData.Text);
        }

        /// <summary>
        /// DataGridViewでのキー押下イベント
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private async void dataGridViewResults_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                var selectedRows = dataGridViewResults.SelectedRows;

                // 選択された行が1行以上ある場合は、削除処理を呼び出す
                if (selectedRows.Count > 0)
                {
                    await DeleteSelectedRows(selectedRows);
                }
            }
        }

        /// <summary>
        /// 行削除処理
        /// </summary>
        /// <param name="selectedRows">選択行</param>
        /// <returns>task</returns>
        private async Task DeleteSelectedRows(DataGridViewSelectedRowCollection selectedRows)
        {
            // 削除確認ダイアログを表示
            DialogResult result = MessageBox.Show(
                "Do you want to delete the selected records?",
                "Info",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes)
            {
                return;
            }

            var deletedIds = new List<string>();

            try
            {
                foreach (DataGridViewRow row in selectedRows)
                {
                    // 各カラムのセルの値を取得し、JSON オブジェクトを構築
                    var jsonObject = new JObject();

                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        // カラム名をキーにし、セルの値を適切に設定
                        var columnName = dataGridViewResults.Columns[cell.ColumnIndex].Name;
                        var cellValue = cell.Value;

                        if (cellValue is DBNull || cellValue is null)
                        {
                            jsonObject[columnName] = null;
                        }
                        else if (cellValue is string || cellValue is DateTime)
                        {
                            jsonObject[columnName] = cellValue.ToString();
                        }
                        else if (cellValue is bool)
                        {
                            jsonObject[columnName] = (bool)cellValue;
                        }
                        else if (cellValue is double || cellValue is float || cellValue is decimal)
                        {
                            jsonObject[columnName] = Convert.ToDouble(cellValue);
                        }
                        else
                        {
                            jsonObject[columnName] = cellValue.ToString();
                        }
                    }

                    // IDを取得してリストに追加
                    var id = jsonObject["id"]?.ToString();
                    if (!string.IsNullOrEmpty(id))
                    {
                        deletedIds.Add($"id:{id}");
                    }

                    await DeleteRow(jsonObject);

                    // DataGridViewから行を削除
                    dataGridViewResults.Rows.Remove(row);
                }

                // 削除したIDをメッセージボックスで表示
                if (deletedIds.Count > 0)
                {
                    var message = $"Deleted IDs:\n{string.Join("\n", deletedIds)}";
                    MessageBox.Show(message, "Info");
                }

                // DataGridViewの更新
                await UpdateDatagridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        /// <summary>
        /// 行削除
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns>task</returns>
        private async Task DeleteRow(JObject jsonObject)
        {
            try
            {
                // JSONオブジェクトからidを取得
                var id = jsonObject["id"].ToString();

                // PartitionKeyを自動的に解決して取得
                var partitionKey = await _cosmosDBService.ResolvePartitionKeyAsync(jsonObject);
                var partitionKeyInfo = _cosmosDBService.GetPartitionKeyValues(jsonObject);

                var response = await _cosmosDBService.DeleteItemAsync<object>(id, partitionKey);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }
    }
}
