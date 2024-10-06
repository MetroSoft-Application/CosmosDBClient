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
    /// Cosmos DB クライアントのデータを表示および管理するフォームクラス
    /// </summary>
    public partial class FormMain : Form
    {
        private CosmosDBService _cosmosDBService;
        private readonly int _maxItemCount;
        private readonly bool _useHyperlinkHandler;
        private HyperlinkHandler _hyperlinkHandler;

        /// <summary>
        /// FormMain クラスのコンストラクタ設定を読み込み、CosmosDBServiceのインスタンスを初期化する
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

                ResizeRowHeader();

                buttonInsert.Enabled = true;
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
                var size = g.MeasureString(cmbBoxContainerName.Text, dataGridViewResults.Font);

                // 最大幅を保持
                var maxRowHeaderWidth = (int)size.Width - 20;

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
            var query = BuildQuery(richTextBoxQuery.Text, GetMaxItemCount());
            var (dataTable, totalRequestCharge, documentCount, pageCount, elapsedMilliseconds) =
                await _cosmosDBService.FetchDataWithStatusAsync(query, GetMaxItemCount());

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
            return Math.Max((int)numericUpDownMaxCount.Value, _maxItemCount);
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
                jsonObject = JObject.Parse(JsonData.Text);
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
                jsonObject = JObject.Parse(JsonData.Text);
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
            using (var formInsert = new FormInsert(_cosmosDBService, JsonData.Text))
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
        /// RichTextBox でマウスアップイベントが発生した際の処理ハイパーリンクを処理する
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">マウスイベントデータ</param>
        private void JsonData_MouseUp(object sender, MouseEventArgs e)
        {
            if (_useHyperlinkHandler)
            {
                _hyperlinkHandler.HandleMouseUpJson(e, JsonData);
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
            var jsonData = (RichTextBox)sender;
            buttonUpdate.Enabled = !string.IsNullOrWhiteSpace(jsonData.Text);
            buttonDelete.Enabled = !string.IsNullOrWhiteSpace(jsonData.Text);
        }
    }
}
