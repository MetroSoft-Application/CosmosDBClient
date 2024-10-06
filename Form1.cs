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
        private readonly string[] systemColumns = { "id", "_etag", "_rid", "_self", "_attachments", "_ts" };

        /// <summary>
        /// 新しい <see cref="Form1"/> クラスのインスタンスを初期化する
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            // フォームのスケーリングモードを設定
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
            try
            {
                // マウスカーソルを待機中に変更
                Cursor.Current = Cursors.WaitCursor;

                // Cosmos DB クライアントを初期化
                cosmosContainer = InitializeCosmosContainer(textBoxConnectionString.Text, textBoxDatabaseName.Text, cmbBoxContainerName.Text);

                // DatagridViewを更新
                await UpdateDatagridView();

                buttonInsert.Enabled = true;
            }
            finally
            {
                // 処理が完了したらマウスカーソルを元に戻す
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// DatagridViewを更新する
        /// </summary>
        /// <returns>Task</returns>
        private async Task UpdateDatagridView()
        {
            // データの取得
            var dataTable = await FetchDataFromCosmosDBAsync();
            SetReadOnlyColumns(dataTable);
            dataGridViewResults.DataSource = dataTable;
        }

        /// <summary>
        /// Cosmos DB コンテナを初期化する
        /// </summary>
        /// <param name="connectionString">接続文字列</param>
        /// <param name="databaseName">DB名</param>
        /// <param name="containerName">コンテナ名</param>
        /// <returns>Containerインスタンス</returns>
        private Container InitializeCosmosContainer(string connectionString, string databaseName, string containerName)
        {
            cosmosClient = new CosmosClient(connectionString);
            return cosmosClient.GetContainer(databaseName, containerName);
        }

        /// <summary>
        /// 指定されたデータベースにあるコンテナの一覧をComboBoxに格納する
        /// </summary>
        /// <param name="databaseId">データベースID</param>
        private async void LoadContainersIntoComboBox(string databaseId)
        {
            try
            {
                var databaseReference = cosmosClient.GetDatabase(databaseId);

                // コンテナ一覧を取得
                using (var containerIterator = databaseReference.GetContainerQueryIterator<ContainerProperties>())
                {
                    while (containerIterator.HasMoreResults)
                    {
                        foreach (var container in await containerIterator.ReadNextAsync())
                        {
                            // ComboBoxにコンテナ名を追加
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
        /// 非同期で Cosmos DB からデータを取得し、DataTable に格納する
        /// </summary>
        /// <returns>Cosmos DB から取得したデータを含む <see cref="DataTable"/></returns>
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
        /// クエリ文字列を構築する
        /// </summary>
        /// <param name="queryText">クエリテキスト</param>
        /// <param name="maxCount">取得する最大アイテム数</param>
        /// <returns>構築されたクエリ文字列</returns>
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
        /// 読み取り専用にする列を設定する
        /// </summary>
        /// <param name="dataTable">データを格納する DataTable</param>
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
            if (e.RowIndex < 0)
            {
                return;
            }
            // 選択された行の全てのカラムのデータをJObjectに変換する
            var jsonObject = new JObject();
            foreach (DataGridViewColumn column in dataGridViewResults.Columns)
            {
                var item = dataGridViewResults.Rows[e.RowIndex].Cells[column.Index].Value?.ToString() ?? string.Empty;
                jsonObject[column.HeaderText] = item;
            }

            // JSON形式で保持する
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

        /// <summary>
        /// レコードを更新する
        /// </summary>
        /// <param name="sender">イベントの送信者</param>
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

            try
            {
                // JSONデータをパース
                var jsonObject = JObject.Parse(JsonData.Text);

                // PartitionKeyを自動的に解決して取得
                var partitionKey = await ResolvePartitionKeyAsync(jsonObject);

                // PartitionKeyに対応するキー項目を取得
                string partitionKeyInfo = GetPartitionKeyValues(jsonObject);

                // Cosmos DBにUpsert処理を実行
                var response = await cosmosContainer.UpsertItemAsync(jsonObject, partitionKey);

                // 成功メッセージを表示
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
        /// レコードを削除する
        /// </summary>
        /// <param name="sender">イベントの送信者</param>
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

            try
            {
                // JSONデータをパース
                var jsonObject = JObject.Parse(JsonData.Text);
                var id = jsonObject["id"].ToString();

                // PartitionKeyを自動的に解決して取得
                var partitionKey = await ResolvePartitionKeyAsync(jsonObject);

                // PartitionKeyに対応するキー項目を取得
                var partitionKeyInfo = GetPartitionKeyValues(jsonObject);

                // Cosmos DBにDelete処理を実行
                var response = await cosmosContainer.DeleteItemAsync<object>(id, partitionKey);

                // 成功メッセージを表示
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
        /// コンテナとjsonの情報に基づいてPartitionKeyを解決し、CosmosDBに使用するPartitionKeyを構築する
        /// </summary>
        /// <param name="jsonObject">パースされたJSONオブジェクト</param>
        /// <returns>解決されたPartitionKeyオブジェクト</returns>
        private async System.Threading.Tasks.Task<PartitionKey> ResolvePartitionKeyAsync(JObject jsonObject)
        {
            // コンテナのプロパティからPartitionKeyPathsを取得
            var containerProperties = await cosmosContainer.ReadContainerAsync();
            var partitionKeyPaths = containerProperties.Resource.PartitionKeyPaths;

            // PartitionKeyBuilderを使用して階層的なPartitionKeyを構築
            var partitionKeyBuilder = new PartitionKeyBuilder();

            // 各PartitionKeyPathに対して、JSONオブジェクトから値を取得し、PartitionKeyBuilderに追加
            foreach (var path in partitionKeyPaths)
            {
                var key = jsonObject.SelectToken(path.Trim('/'))?.ToString();
                if (key == null)
                {
                    return default(PartitionKey);
                }

                partitionKeyBuilder.Add(key);
            }

            // PartitionKeyを構築して返す
            return partitionKeyBuilder.Build();
        }

        /// <summary>
        /// PartitionKeyに対応するフィールド名と値を取得し、改行して表示するための文字列を構築する
        /// </summary>
        /// <param name="jsonObject">パースされたJSONオブジェクト</param>
        /// <returns>PartitionKeyに対応するフィールド名と値を改行で連結した文字列</returns>
        private string GetPartitionKeyValues(JObject jsonObject)
        {
            var containerProperties = cosmosContainer.ReadContainerAsync().Result;
            var partitionKeyPaths = containerProperties.Resource.PartitionKeyPaths;

            // フィールド名と値を改行で結合して表示
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
        /// DataGridView のセルフォーマット時に呼び出されるイベントハンドラ。読み取り専用のカラムに対して背景色と文字色を設定する。
        /// </summary>
        /// <param name="sender">イベントの送信者</param>
        /// <param name="e">セルフォーマットに関するイベントデータ</param>
        private void dataGridViewResults_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            foreach (DataGridViewColumn column in dataGridViewResults.Columns)
            {
                // ReadOnlyプロパティがtrueのカラムの色を変更
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
