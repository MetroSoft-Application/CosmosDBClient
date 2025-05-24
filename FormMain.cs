using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using CosmosDBClient.CosmosDB;
using FastColoredTextBoxNS;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Zuby.ADGV;

namespace CosmosDBClient
{
    /// <summary>
    /// Cosmos DB クライアントのデータを表示および管理するフォームクラス
    /// </summary>
    public partial class FormMain : Form
    {
        private CosmosDBService _cosmosDBService;
        private TableAPI.TableAPIService _tableAPIService;
        private readonly int _maxItemCount;
        private readonly bool _useHyperlinkHandler;
        private HyperlinkHandler _hyperlinkHandler;
        private FastColoredTextBox _textBoxQuery;
        private FastColoredTextBox _jsonData;
        private AdvancedDataGridView dataGridViewResults;
        private TextStyle jsonStringStyle = new TextStyle(Brushes.Black, null, FontStyle.Regular);
        private DataTable _virtualDataTable;
        private DataTable _originalDataTable;
        private List<string> _columnNames;
        private bool _virtualModeEnabled = true;
        private ApiMode _apiMode;
        private Label _tableModeLabel;
        private TextBox textBoxInfo;

        /// <summary>
        /// FormMain クラスのコンストラクタ設定を読み込み、CosmosDBServiceのインスタンスを初期化する
        /// </summary>
        public FormMain()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;
            SetupDatagridview();
            // Time to Live設定用のFlowLayoutPanel
            var ttlFlowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            // tabPage1に追加
            if (CosmosDBFactory.IsTableApiMode())
            {
                ttlFlowPanel.Controls.Add(label5);
                ttlFlowPanel.Controls.Add(radioTimeToLiveOff);
                ttlFlowPanel.Controls.Add(radioTimeToLiveOn);
                ttlFlowPanel.Controls.Add(nupTimeToLiveSeconds);
                ttlFlowPanel.Controls.Add(label6);
                ttlFlowPanel.Controls.Add(label7);
                ttlFlowPanel.Controls.Add(txtPartitionKey);
                ttlFlowPanel.Controls.Add(label8);
                ttlFlowPanel.Controls.Add(txtUniqueKey);

                // テーブル情報表示用のテキストボックスを初期化
                textBoxInfo = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Fill,
                    Font = new Font("Yu Gothic UI", 9),
                    Visible = false
                };
                tabPage1.Controls.Clear();
                tabPage1.Controls.Add(ttlFlowPanel);
                tabPage1.Controls.Add(textBoxInfo);
            }

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
            _jsonData.TabLength = 4;
            _jsonData.WordWrap = true;
            _jsonData.ShowLineNumbers = false;
            _jsonData.ReadOnly = true;
            _jsonData.TextChangedDelayed += (sender, args) =>
            {
                _jsonData.BeginUpdate();
                // 全範囲を対象にクリアしてから、JSON文字列にスタイル適用
                _jsonData.Range.ClearStyle(_jsonData.SyntaxHighlighter.StringStyle);
                _jsonData.Range.ClearStyle(jsonStringStyle);
                _jsonData.Range.SetStyle(jsonStringStyle, "\".*?\"");
                _jsonData.EndUpdate();
            };

            splitContainer3.Panel1.Controls.Add(_jsonData);

            var configuration = LoadConfiguration();
            // APIモードの取得と初期化
            _apiMode = CosmosDBFactory.InitializeFromConfig(configuration);
            // クエリテキストボックスは既にコンストラクタの先頭で設定済み

            _useHyperlinkHandler = configuration.GetValue<bool>("AppSettings:EnableHyperlinkHandler");
            if (_useHyperlinkHandler)
            {
                _hyperlinkHandler = new HyperlinkHandler();
            }
            _maxItemCount = configuration.GetValue<int>("AppSettings:MaxItemCount");

            // モードに応じた初期値とUI設定
            if (CosmosDBFactory.IsSqlApiMode())
            {
                var sqlApiSettings = configuration.GetSection("AppSettings:SqlApi");
                var connectionString = sqlApiSettings.GetValue<string>("ConnectionString");
                var databaseName = sqlApiSettings.GetValue<string>("DatabaseName");
                var containerName = sqlApiSettings.GetValue<string>("ContainerName");

                textBoxConnectionString.Text = connectionString;
                textBoxDatabaseName.Text = databaseName;
                cmbBoxContainerName.Text = containerName;
                label2.Text = "Database";
                label3.Text = "Container";

                this.Text = "Cosmos DB Client Tool - SQL API Mode";

                try
                {
                    _cosmosDBService = CosmosDBFactory.CreateSqlApiService(
                        textBoxConnectionString.Text,
                        textBoxDatabaseName.Text,
                        cmbBoxContainerName.Text);
                    if (!string.IsNullOrWhiteSpace(textBoxDatabaseName.Text))
                    {
                        LoadContainersIntoComboBox(textBoxDatabaseName.Text);
                        DisplayContainerSettings();
                    }
                }
                catch (Exception)
                {
                    // 初期化時のエラーは無視（設定が不完全な場合）
                }
            }
            else  // Table API モード
            {
                var tableName = configuration.GetValue<string>("AppSettings:TableName");
                var tableApiSettings = configuration.GetSection("AppSettings:TableApi");
                var connectionString = tableApiSettings.GetValue<string>("ConnectionString");

                textBoxConnectionString.Text = connectionString;
                textBoxDatabaseName.Visible = false;
                label2.Visible = false;
                cmbBoxContainerName.Text = tableName;
                label3.Text = "Table";

                this.Text = "Cosmos DB Client Tool - Table API Mode";

                // Table APIモードの表示
                _tableModeLabel = new Label
                {
                    Text = "TABLE API MODE",
                    ForeColor = Color.Red,
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(textBoxDatabaseName.Location.X, textBoxDatabaseName.Location.Y)
                };
                groupBox1.Controls.Add(_tableModeLabel);
                try
                {
                    _tableAPIService = CosmosDBFactory.CreateTableApiService(
                        textBoxConnectionString.Text,
                        cmbBoxContainerName.Text); if (!string.IsNullOrWhiteSpace(tableName))
                    {
                        LoadTablesIntoComboBox();
                        DisplayContainerSettings();
                    }
                }
                catch (Exception)
                {
                    // 初期化時のエラーは無視（設定が不完全な場合）
                }
            }

            numericUpDownMaxCount.Value = _maxItemCount;

            if (_useHyperlinkHandler)
            {
                _hyperlinkHandler = new HyperlinkHandler();
            }

            // SQL API モードの場合のみ、重複初期化を避ける
            if (CosmosDBFactory.IsSqlApiMode() && _cosmosDBService == null)
            {
                try
                {
                    _cosmosDBService = new CosmosDBService(textBoxConnectionString.Text, textBoxDatabaseName.Text, cmbBoxContainerName.Text);
                    if (!string.IsNullOrWhiteSpace(textBoxDatabaseName.Text))
                    {
                        LoadContainersIntoComboBox(textBoxDatabaseName.Text);
                        DisplayContainerSettings();
                    }
                }
                catch (Exception)
                {
                    // 初期化エラーは無視
                }
            }
            // textBoxInfoは既にコンストラクタの最初で初期化済みのため、ここでの再初期化は不要
        }

        /// <summary>
        /// DataGridView の設定を行う
        /// </summary>
        private void SetupDatagridview()
        {
            dataGridViewResults = new AdvancedDataGridView();
            dataGridViewResults.AllowUserToAddRows = false;
            dataGridViewResults.AllowUserToDeleteRows = false;
            dataGridViewResults.AllowUserToOrderColumns = true;

            // 仮想モードを有効化
            dataGridViewResults.VirtualMode = _virtualModeEnabled;

            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.ActiveCaption;
            dataGridViewCellStyle1.Font = new Font("Yu Gothic UI", 9F);
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dataGridViewResults.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridViewResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewResults.Dock = DockStyle.Fill;
            dataGridViewResults.Location = new Point(0, 0);
            dataGridViewResults.Name = "dataGridViewResults";
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle2.Font = new Font("Yu Gothic UI", 9F);
            dataGridViewCellStyle2.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            dataGridViewResults.RowHeadersDefaultCellStyle = dataGridViewCellStyle2;
            dataGridViewResults.RowHeadersWidth = 51;
            dataGridViewResults.Size = new Size(1026, 478);
            dataGridViewResults.TabIndex = 19;

            // 仮想モード用のイベントハンドラを追加
            if (_virtualModeEnabled)
            {
                dataGridViewResults.CellValueNeeded += dataGridViewResults_CellValueNeeded;
                dataGridViewResults.CellValuePushed += dataGridViewResults_CellValuePushed;
                dataGridViewResults.RowCount = 0;
            }

            // AdvancedDataGridViewのフィルタとソート関連のイベントを追加
            if (dataGridViewResults is AdvancedDataGridView advancedGrid)
            {
                dataGridViewResults.FilterStringChanged += dataGridViewResults_FilterStringChanged;
                dataGridViewResults.SortStringChanged += dataGridViewResults_SortStringChanged;
            }

            dataGridViewResults.CellClick += dataGridViewResults_CellClick;
            dataGridViewResults.CellFormatting += dataGridViewResults_CellFormatting;
            dataGridViewResults.RowPostPaint += dataGridViewResults_RowPostPaint;
            dataGridViewResults.KeyUp += dataGridViewResults_KeyUp;
            splitContainer2.Panel1.Controls.Add(dataGridViewResults);
        }

        /// <summary>
        /// AdvancedDataGridViewのフィルタリングイベントハンドラ
        /// </summary>
        private void dataGridViewResults_FilterStringChanged(object sender, AdvancedDataGridView.FilterEventArgs e)
        {
            if (_virtualModeEnabled && _virtualDataTable != null)
            {
                try
                {
                    // フィルタ適用前にオリジナルのデータテーブルをバックアップ（必要に応じて）
                    if (_originalDataTable == null)
                    {
                        _originalDataTable = _virtualDataTable.Copy();
                    }

                    // フィルタ文字列を取得
                    var filterString = dataGridViewResults.FilterString;

                    if (string.IsNullOrEmpty(filterString))
                    {
                        // フィルタなしの場合、元のデータに戻す
                        if (_originalDataTable != null)
                        {
                            _virtualDataTable = _originalDataTable.Copy();
                            _originalDataTable = null;
                        }
                    }
                    else
                    {
                        // データテーブルにフィルタを適用
                        var filteredRows = _originalDataTable.Select(filterString);
                        _virtualDataTable = _originalDataTable.Clone();

                        foreach (var row in filteredRows)
                        {
                            _virtualDataTable.ImportRow(row);
                        }
                    }

                    // UIを更新
                    dataGridViewResults.RowCount = _virtualDataTable.Rows.Count;
                    dataGridViewResults.Invalidate();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Filter error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// AdvancedDataGridViewの並べ替えイベントハンドラ
        /// </summary>
        private void dataGridViewResults_SortStringChanged(object sender, AdvancedDataGridView.SortEventArgs e)
        {
            if (_virtualModeEnabled && _virtualDataTable != null)
            {
                try
                {
                    // 並べ替え文字列を取得
                    var sortString = dataGridViewResults.SortString;

                    // 並べ替えなしの場合は処理しない
                    if (string.IsNullOrEmpty(sortString))
                    {
                        return;
                    }

                    // データテーブルに並べ替えを適用
                    _virtualDataTable.DefaultView.Sort = sortString;

                    // 並べ替えられたビューから新しいDataTableを生成
                    _virtualDataTable = _virtualDataTable.DefaultView.ToTable();

                    // UIを更新
                    dataGridViewResults.Invalidate();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Sort error: {ex.Message}");
                }
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
                buttonDelete.Enabled = true;
                buttonUpdate.Enabled = true;

                // 接続文字列のチェック
                if (string.IsNullOrWhiteSpace(textBoxConnectionString.Text))
                {
                    MessageBox.Show("Connection string is not set. Please enter a connection string.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 大量データの場合は進捗表示を行う                ShowProgressUI(true, "Loading data...");

                if (CosmosDBFactory.IsSqlApiMode())
                {
                    // データベース名のチェック
                    if (string.IsNullOrWhiteSpace(textBoxDatabaseName.Text))
                    {
                        MessageBox.Show("Database name is not set. Please enter a database name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // コンテナ名のチェック
                    if (string.IsNullOrWhiteSpace(cmbBoxContainerName.Text))
                    {
                        MessageBox.Show("Container name is not set. Please enter a container name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    try
                    {
                        _cosmosDBService = CosmosDBFactory.CreateSqlApiService(
                            textBoxConnectionString.Text,
                            textBoxDatabaseName.Text,
                            cmbBoxContainerName.Text);

                        if (_virtualModeEnabled && GetMaxItemCount() > 1000)
                        {
                            // 大量データの場合はバッファリングを使用
                            await UpdateVirtualDataGridView(BuildQuery(_textBoxQuery.Text, GetMaxItemCount()), 1000);
                        }
                        else
                        {
                            // 通常の更新処理
                            await UpdateDatagridView();
                        }

                        DisplayContainerSettings();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while executing SQL API service: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }                else // Table API Mode
                { 
                if (string.IsNullOrWhiteSpace(cmbBoxContainerName.Text))
                {
                    MessageBox.Show("Table name is not set. Please enter a table name.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                    try
                    {
                        _tableAPIService = CosmosDBFactory.CreateTableApiService(
                            textBoxConnectionString.Text,
                            cmbBoxContainerName.Text); await UpdateDatagridViewForTableAPI();
                        DisplayContainerSettings();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while executing Table API service: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                _jsonData.Text = string.Empty;
                ResizeRowHeader();
                buttonInsert.Enabled = true;

                // メモリ最適化
                OptimizeMemoryUsage();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            finally
            {
                ShowProgressUI(false);
            }
        }

        /// <summary>
        /// 進捗表示用UIの表示/非表示を切り替える
        /// </summary>
        private void ShowProgressUI(bool show, string message = "")
        {
            // この例ではステータスバーに進捗を表示
            if (show)
            {
                toolStripStatusLabel1.Text = message;
                Cursor = Cursors.WaitCursor;
                Application.DoEvents();
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// 進捗状況を更新する
        /// </summary>
        private void UpdateProgressUI(string message)
        {
            toolStripStatusLabel1.Text = message;
            Application.DoEvents();
        }

        /// <summary>
        /// 大量データ処理時のメモリ使用状況を最適化
        /// </summary>
        private void OptimizeMemoryUsage()
        {
            // 大規模データセットの場合、明示的なガベージコレクションを実行
            if (_virtualDataTable != null && _virtualDataTable.Rows.Count > 10000)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// バッファリングを活用した仮想モードの更新処理
        /// </summary>
        private async Task UpdateVirtualDataGridView(string query, int batchSize = 1000)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // 新しいデータテーブルを作成
                var newDataTable = new DataTable();
                var totalItems = 0;
                var totalRequestCharge = 0.0;
                var batchCount = 0;

                // バッチ処理によるデータ取得
                var queryDefinition = new QueryDefinition(query);
                var queryResultSetIterator = _cosmosDBService.CosmosContainer.GetItemQueryIterator<dynamic>(
                    queryDefinition, requestOptions: new QueryRequestOptions { MaxItemCount = batchSize });

                while (queryResultSetIterator.HasMoreResults)
                {
                    var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    batchCount++;
                    totalRequestCharge += currentResultSet.RequestCharge;
                    totalItems += currentResultSet.Count;

                    // 進捗状況の更新
                    UpdateProgressUI($"Loading batch {batchCount}, items: {totalItems}...");

                    // バッチごとのデータをDataTableに追加
                    foreach (var item in currentResultSet)
                    {
                        var jsonObject = JObject.Parse(item.ToString());
                        AddRowToDataTable(jsonObject, newDataTable);
                    }

                    // 一定間隔でUIを更新し応答性を維持
                    if (batchCount % 5 == 0)
                    {
                        Application.DoEvents();
                    }
                }

                // 読み込み完了したデータで更新
                _virtualDataTable = newDataTable;

                // カラム情報の設定
                dataGridViewResults.Columns.Clear();
                _columnNames = new List<string>();

                foreach (DataColumn column in _virtualDataTable.Columns)
                {
                    _columnNames.Add(column.ColumnName);
                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = column.ColumnName,
                        HeaderText = column.ColumnName,
                        DataPropertyName = column.ColumnName
                    };
                    dataGridViewResults.Columns.Add(col);
                }

                // 読み取り専用列の設定
                await SetReadOnlyColumnsForVirtualMode();

                // 行数を設定
                dataGridViewResults.RowCount = _virtualDataTable.Rows.Count;

                // ステータス更新
                stopwatch.Stop();
                UpdateStatusStrip(totalRequestCharge, totalItems, batchCount, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// JSONオブジェクトからデータテーブルに行を追加する
        /// </summary>
        /// <param name="jsonObject">JSONオブジェクト</param>
        /// <param name="dataTable">データテーブル</param>
        private void AddRowToDataTable(JObject jsonObject, DataTable dataTable)
        {
            var row = dataTable.NewRow();
            var jstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");

            foreach (var property in jsonObject.Properties())
            {
                // カラムが存在するか確認
                if (!dataTable.Columns.Contains(property.Name))
                {
                    // 存在しないカラムは動的に追加
                    dataTable.Columns.Add(property.Name, typeof(string));
                }

                // 日付項目をJSTに変換してフォーマット
                if (DateTime.TryParse(property.Value?.ToString(), out var dateValue))
                {
                    var jstDate = TimeZoneInfo.ConvertTimeFromUtc(dateValue.ToUniversalTime(), jstTimeZone);
                    row[property.Name] = jstDate.ToString("yyyy-MM-ddTHH:mm:ss"); // タイムゾーン情報なし
                }
                else
                {
                    // 新しいカラムに値を設定
                    row[property.Name] = property.Value ?? string.Empty;
                }
            }

            // 行をDataTableに追加
            dataTable.Rows.Add(row);
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
            var result = await _cosmosDBService.FetchDataWithStatusAsync(query, GetMaxItemCount());

            if (_virtualModeEnabled)
            {
                // 仮想モードの場合、DataTableを内部に保持して手動で管理
                _virtualDataTable = result.Data;
                _originalDataTable = null; // フィルタリング状態をリセット

                // カラム情報の設定
                dataGridViewResults.Columns.Clear();
                _columnNames = new List<string>();

                foreach (DataColumn column in _virtualDataTable.Columns)
                {
                    _columnNames.Add(column.ColumnName);
                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = column.ColumnName,
                        HeaderText = column.ColumnName,
                        DataPropertyName = column.ColumnName
                    };
                    dataGridViewResults.Columns.Add(col);
                }

                // 行数を設定
                dataGridViewResults.RowCount = _virtualDataTable.Rows.Count;

                // 読み取り専用列の設定
                await SetReadOnlyColumnsForVirtualMode();
            }
            else
            {
                // 通常モードの場合は従来通りDataSourceを使用
                dataGridViewResults.DataSource = null;
                SetReadOnlyColumns(result.Data);
                dataGridViewResults.DataSource = result.Data;
            }

            UpdateStatusStrip(result.TotalRequestCharge, result.DocumentCount, result.PageCount, result.ElapsedMilliseconds);
        }

        /// <summary>
        /// Table APIのデータでDataGridViewを更新する
        /// </summary>
        private async Task UpdateDatagridViewForTableAPI()
        {            ShowProgressUI(true, "Retrieving data from Table API...");

            try
            {
                // テーブル名が空の場合はエラーメッセージを表示
                if (string.IsNullOrWhiteSpace(cmbBoxContainerName.Text))
                {
                    MessageBox.Show("Table name is not set. Please enter a table name and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // TableAPIServiceが未初期化の場合は初期化
                if (_tableAPIService == null)
                {
                    try
                    {
                        _tableAPIService = CosmosDBFactory.CreateTableApiService(
                            textBoxConnectionString.Text,
                            cmbBoxContainerName.Text);
                    }
                    catch (Exception ex)
                    {                        MessageBox.Show($"Failed to initialize Table API service: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // テーブルの存在確認
                bool tableExists = await _tableAPIService.TableExistsAsync();
                if (!tableExists)
                {                    var tableConfirmResult = MessageBox.Show(
                        $"Table '{cmbBoxContainerName.Text}' does not exist. Would you like to create it?",
                        "Table Confirmation",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (tableConfirmResult == DialogResult.Yes)
                    {
                        var createResult = await _tableAPIService.CreateTableAsync();
                        if (!createResult.Success)
                        {                            MessageBox.Show(createResult.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        // テーブルが作成されたことをメッセージで通知
                        ShowProgressUI(true, "Table created. Loading data...");
                    }
                    else
                    {
                        // テーブル作成をキャンセルした場合は処理を中止
                        return;
                    }
                }

                // クエリ文字列の取得
                string queryText = _textBoxQuery.Text?.Trim();
                int maxCount = (int)numericUpDownMaxCount.Value;

                // QueryResult取得
                var result = await _tableAPIService.FetchDataWithStatusAsync(queryText, maxCount);

                // 仮想モード用のデータテーブルを設定
                if (_virtualModeEnabled)
                {
                    _virtualDataTable = result.Data;
                    _columnNames = new List<string>();

                    // 列名のリストを作成
                    foreach (DataColumn column in _virtualDataTable.Columns)
                    {
                        _columnNames.Add(column.ColumnName);
                    }

                    // DataGridViewの列を設定
                    dataGridViewResults.Columns.Clear();
                    foreach (DataColumn column in _virtualDataTable.Columns)
                    {
                        var col = new DataGridViewTextBoxColumn
                        {
                            Name = column.ColumnName,
                            HeaderText = column.ColumnName,
                            DataPropertyName = column.ColumnName
                        };
                        dataGridViewResults.Columns.Add(col);
                    }

                    // 行数を設定
                    dataGridViewResults.RowCount = _virtualDataTable.Rows.Count;

                    // システム列を読み取り専用に設定
                    var readOnlyStyle = new DataGridViewCellStyle
                    {
                        BackColor = System.Drawing.Color.DarkGray,
                        ForeColor = System.Drawing.Color.White
                    };

                    var systemColumns = _tableAPIService.systemColumns;
                    var readOnlyColumnSet = new HashSet<string>(systemColumns);

                    foreach (DataGridViewColumn column in dataGridViewResults.Columns)
                    {
                        if (readOnlyColumnSet.Contains(column.Name))
                        {
                            column.ReadOnly = true;
                            column.DefaultCellStyle = readOnlyStyle;
                        }
                    }
                }
                else
                {
                    // 通常モードの場合は従来通りDataSourceを使用
                    dataGridViewResults.DataSource = null;
                    // システム列を読み取り専用に設定
                    SetReadOnlyColumnsForTableAPI(result.Data);
                    dataGridViewResults.DataSource = result.Data;
                }

                // ステータス更新
                UpdateStatusStrip(result.TotalRequestCharge, result.DocumentCount, result.PageCount, result.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while retrieving data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ShowProgressUI(false);
                OptimizeMemoryUsage();
            }
        }

        /// <summary>
        /// 仮想モード時のセル値取得イベントハンドラ
        /// </summary>
        private void dataGridViewResults_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_virtualDataTable != null && e.RowIndex < _virtualDataTable.Rows.Count && e.ColumnIndex < _columnNames.Count)
            {
                try
                {
                    string columnName = _columnNames[e.ColumnIndex];
                    e.Value = _virtualDataTable.Rows[e.RowIndex][columnName];
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CellValueNeeded error: {ex.Message} at row {e.RowIndex}, column {e.ColumnIndex}");
                    e.Value = null;
                }
            }
        }

        /// <summary>
        /// 仮想モード時のセル値変更イベントハンドラ
        /// </summary>
        private void dataGridViewResults_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_virtualDataTable != null && e.RowIndex < _virtualDataTable.Rows.Count && e.ColumnIndex < _columnNames.Count)
            {
                try
                {
                    string columnName = _columnNames[e.ColumnIndex];

                    // 型変換を適切に処理
                    var column = _virtualDataTable.Columns[columnName];
                    object typedValue = e.Value;

                    // DBNull対応
                    if (e.Value == null || string.IsNullOrEmpty(e.Value.ToString()))
                    {
                        typedValue = DBNull.Value;
                    }
                    else if (column.DataType == typeof(int) && int.TryParse(e.Value.ToString(), out int intValue))
                    {
                        typedValue = intValue;
                    }
                    else if (column.DataType == typeof(decimal) && decimal.TryParse(e.Value.ToString(), out decimal decimalValue))
                    {
                        typedValue = decimalValue;
                    }
                    // その他の型変換処理をここに追加

                    _virtualDataTable.Rows[e.RowIndex][columnName] = typedValue;

                    // 編集されたことをマークして必要なUIを更新
                    if (!_virtualDataTable.Rows[e.RowIndex].RowState.HasFlag(DataRowState.Modified))
                    {
                        _virtualDataTable.Rows[e.RowIndex].SetModified();
                    }

                    // セルのスタイル更新
                    dataGridViewResults.InvalidateCell(e.ColumnIndex, e.RowIndex);

                    // 重要な列（id, パーティションキーなど）が変更された場合の処理
                    if (columnName == "id" || IsPartitionKeyColumn(columnName))
                    {
                        // 重要な値の変更を通知する場合はここで実装
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CellValuePushed error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 指定された列名がパーティションキーの列かどうかをチェック
        /// </summary>
        private bool IsPartitionKeyColumn(string columnName)
        {
            try
            {
                var containerProperties = _cosmosDBService.GetContainerPropertiesAsync().Result;
                var partitionKeyPaths = containerProperties.PartitionKeyPaths.Select(p => p.Trim('/')).ToArray();
                return partitionKeyPaths.Contains(columnName);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 仮想モード時の読み取り専用列設定
        /// </summary>
        private async Task SetReadOnlyColumnsForVirtualMode()
        {
            try
            {
                var containerProperties = await _cosmosDBService.GetContainerPropertiesAsync();
                var partitionKeyPaths = containerProperties.PartitionKeyPaths.Select(p => p.Trim('/')).ToArray();
                var readOnlyColumns = _cosmosDBService.systemColumns.Concat(partitionKeyPaths).ToArray();

                // 読み取り専用セルの書式用のスタイルを一度だけ作成
                var readOnlyStyle = new DataGridViewCellStyle
                {
                    BackColor = System.Drawing.Color.DarkGray,
                    ForeColor = System.Drawing.Color.White
                };

                // ReadOnlyColumnsハッシュセットを作成（Contains検索を高速化）
                var readOnlyColumnSet = new HashSet<string>(readOnlyColumns);

                foreach (DataGridViewColumn column in dataGridViewResults.Columns)
                {
                    if (readOnlyColumnSet.Contains(column.Name))
                    {
                        column.ReadOnly = true;
                        column.DefaultCellStyle = readOnlyStyle;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        /// <summary>
        /// 仮想モード時のDataGridViewのセルがクリックされた際の処理
        /// </summary>
        private void dataGridViewResults_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            var jsonObject = BuildJsonObjectFromRow(e.RowIndex);

            // JSON文字列を整形し、改行とインデントを処理
            var formattedJson = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
            _jsonData.Text = formattedJson;

            if (e.ColumnIndex > -1)
            {
                var cellValue = dataGridViewResults.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
                richTextBoxSelectedCell.Text = cellValue;

                if (_useHyperlinkHandler && (
                    dataGridViewResults.Columns[e.ColumnIndex].HeaderText == "folderName" ||
                    dataGridViewResults.Columns[e.ColumnIndex].HeaderText == "fullPath"
                    ))
                {
                    _hyperlinkHandler.MarkLinkTextFromText(richTextBoxSelectedCell);
                }
            }
        }

        /// <summary>
        /// 選択された行のデータからJSONオブジェクトを構築
        /// </summary>
        /// <param name="rowIndex">行インデックス</param>
        /// <returns>構築されたJSONオブジェクト</returns>
        private JObject BuildJsonObjectFromRow(int rowIndex)
        {
            var jsonObject = new JObject();

            if (_virtualModeEnabled && _virtualDataTable != null && rowIndex < _virtualDataTable.Rows.Count)
            {
                // 仮想モードの場合はDataTableから直接データを取得（効率的）
                var dataRow = _virtualDataTable.Rows[rowIndex];

                foreach (var columnName in _columnNames)
                {
                    var value = dataRow[columnName];
                    if (value is DBNull)
                    {
                        jsonObject[columnName] = null;
                    }
                    else
                    {
                        jsonObject[columnName] = TryParseJson(value.ToString());
                    }
                }
            }
            else
            {
                // 通常モードの場合はDataGridViewのセルから取得
                foreach (DataGridViewColumn column in dataGridViewResults.Columns)
                {
                    var item = dataGridViewResults.Rows[rowIndex].Cells[column.Index].Value?.ToString() ?? string.Empty;
                    jsonObject[column.HeaderText] = TryParseJson(item);
                }
            }

            return jsonObject;
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
        /// コンボボックスにテーブル一覧を読み込む
        /// </summary>
        private async void LoadTablesIntoComboBox()
        {
            try
            {
                // 既存のアイテムをクリア
                cmbBoxContainerName.Items.Clear();

                if (_tableAPIService == null)
                {
                    _tableAPIService = CosmosDBFactory.CreateTableApiService(
                        textBoxConnectionString.Text,
                        string.Empty);
                }

                var tableNames = await _tableAPIService.GetTableNamesAsync();

                if (tableNames != null && tableNames.Any())
                {
                    cmbBoxContainerName.Items.AddRange(tableNames.ToArray());
                    if (cmbBoxContainerName.Items.Count > 0)
                    {
                        cmbBoxContainerName.SelectedIndex = 0;
                    }

                    // テーブルが見つかった場合はInfoに表示
                    textBoxInfo.Text = $"{tableNames.Count} tables found.";
                }
                else
                {
                    textBoxInfo.Text = "No tables found. Please create a new table.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while retrieving table list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBoxInfo.Text = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// コンテナの設定を表示する
        /// </summary>
        private async void DisplayContainerSettings()
        {
            try
            {
                // SQLモードの場合
                if (CosmosDBFactory.IsSqlApiMode())
                {
                    var containerProperties = await _cosmosDBService.GetContainerPropertiesAsync();

                    // パーティションキーとユニークキーを表示
                    txtPartitionKey.Visible = true;
                    label7.Visible = true;
                    txtUniqueKey.Visible = true;
                    label8.Visible = true;

                    // パーティションキー情報を設定
                    if (containerProperties.PartitionKeyPaths?.Any() == true)
                    {
                        txtPartitionKey.Text = string.Join(", ", containerProperties.PartitionKeyPaths);
                    }
                    else
                    {
                        txtPartitionKey.Text = "No partition key defined";
                    }

                    // ユニークキー情報を設定
                    if (containerProperties.UniqueKeyPolicy?.UniqueKeys?.Any() == true)
                    {
                        var uniqueKeys = containerProperties.UniqueKeyPolicy.UniqueKeys
                            .SelectMany(uk => uk.Paths)
                            .ToList();
                        txtUniqueKey.Text = string.Join(", ", uniqueKeys);
                    }
                    else
                    {
                        txtUniqueKey.Text = "No unique keys defined";
                    }

                    // TTLの設定を表示
                    radioTimeToLiveOff.Visible = true;
                    radioTimeToLiveOn.Visible = true;
                    label5.Visible = true;

                    if (containerProperties.DefaultTimeToLive == null)
                    {
                        radioTimeToLiveOff.Checked = true;
                        nupTimeToLiveSeconds.Visible = false;
                        label6.Visible = false;
                    }
                    else
                    {
                        radioTimeToLiveOn.Checked = true;
                        nupTimeToLiveSeconds.Value = containerProperties.DefaultTimeToLive.Value;
                        nupTimeToLiveSeconds.Visible = true;
                        label6.Visible = true;
                    }

                    // Indexing Policyタブの更新
                    txtIndexingPolicy.Text = JsonConvert.SerializeObject(containerProperties.IndexingPolicy, Formatting.Indented);
                }
                else
                {
                    // Table APIモードの場合
                    // パーティションキーとユニークキーを非表示
                    txtPartitionKey.Visible = false;
                    label7.Visible = false;
                    txtUniqueKey.Visible = false;
                    label8.Visible = false;

                    // Table APIモードでもTTL設定は表示
                    radioTimeToLiveOff.Visible = true;
                    radioTimeToLiveOn.Visible = true;
                    label5.Visible = true;

                    // Table APIではTTLは通常OFF
                    radioTimeToLiveOff.Checked = true;
                    nupTimeToLiveSeconds.Visible = false;
                    label6.Visible = false;

                    // Table APIのインデックスポリシーを取得して表示
                    try
                    {
                        var indexingPolicy = await _tableAPIService.GetIndexingPolicyAsync();
                        txtIndexingPolicy.Text = JsonConvert.SerializeObject(indexingPolicy, Formatting.Indented);
                    }
                    catch (Exception indexEx)
                    {
                        txtIndexingPolicy.Text = $"インデックスポリシーの取得中にエラーが発生しました: {indexEx.Message}";
                    }

                    // textBoxInfoは非表示
                    textBoxInfo.Visible = false;
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
            if (dataGridViewResults.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a record to update.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int selectedRowIndex = dataGridViewResults.SelectedRows[0].Index;

            try
            {
                if (CosmosDBFactory.IsSqlApiMode())
                {
                    // SQL APIモード
                    JObject jsonObject = BuildJsonObjectFromRow(selectedRowIndex);
                    if (jsonObject is null)
                    {
                        return;
                    }

                    using (var formInsert = new FormInsert(_cosmosDBService, jsonObject.ToString(Formatting.Indented)))
                    {
                        if (formInsert.ShowDialog() == DialogResult.OK)
                        {
                            await UpdateDatagridView();
                        }
                    }
                }
                else
                {
                    // Table APIモード
                    DataRow rowData = null;
                    if (_virtualModeEnabled && _virtualDataTable != null)
                    {
                        if (selectedRowIndex < _virtualDataTable.Rows.Count)
                        {
                            rowData = _virtualDataTable.Rows[selectedRowIndex];
                        }
                    }
                    else if (dataGridViewResults.DataSource is DataTable dataTable)
                    {
                        if (selectedRowIndex < dataTable.Rows.Count)
                        {
                            rowData = dataTable.Rows[selectedRowIndex];
                        }
                    }
                    if (rowData != null)
                    {
                        using (var formInsert = new FormInsert(_tableAPIService, rowData))
                        {
                            if (formInsert.ShowDialog() == DialogResult.OK)
                            {
                                await UpdateDatagridViewForTableAPI();
                            }
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
        /// レコードを削除する
        /// ユーザー確認後、Cosmos DB から該当レコードが削除される
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private async void buttonDelete_Click(object sender, EventArgs e)
        {
            await DeleteSelectedRows();
        }

        /// <summary>
        /// 選択された行を削除する
        /// </summary>
        private async Task DeleteSelectedRows()
        {
            if (dataGridViewResults.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a row to delete.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }            var confirmResult = MessageBox.Show(
                $"Are you sure you want to delete the selected {dataGridViewResults.SelectedRows.Count} row(s)?",
                "Delete Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes)
            {
                return;
            }

            ShowProgressUI(true, "データを削除中...");
            var successCount = 0;
            var errorCount = 0;
            var totalCount = dataGridViewResults.SelectedRows.Count;

            try
            {
                for (int i = 0; i < dataGridViewResults.SelectedRows.Count; i++)
                {
                    var row = dataGridViewResults.SelectedRows[i];
                    try
                    {
                        if (CosmosDBFactory.IsSqlApiMode())
                        {
                            string id = row.Cells["id"].Value?.ToString();
                            string partitionKey = GetPartitionKeyValue(row);

                            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(partitionKey))
                            {
                                errorCount++;
                                continue;
                            }

                            await _cosmosDBService.DeleteItemAsync<dynamic>(id, new PartitionKey(partitionKey));
                        }
                        else // Table API mode
                        {
                            string partitionKey = row.Cells["PartitionKey"].Value?.ToString();
                            string rowKey = row.Cells["RowKey"].Value?.ToString();

                            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                            {
                                errorCount++;
                                continue;
                            }

                            await _tableAPIService.DeleteEntityAsync(partitionKey, rowKey);
                        }

                        successCount++;
                        UpdateProgressUI($"削除中... ({successCount}/{totalCount})");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"行の削除中にエラーが発生しました: {ex.Message}");
                        errorCount++;
                    }
                }

                // 画面を更新
                if (CosmosDBFactory.IsSqlApiMode())
                {
                    await UpdateDatagridView();
                }
                else
                {
                    await UpdateDatagridViewForTableAPI();
                }                string resultMessage = $"Deletion completed:\nSuccess: {successCount}\nErrors: {errorCount}";
                MessageBox.Show(resultMessage, "Deletion Result", MessageBoxButtons.OK,
                    errorCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred during deletion: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ShowProgressUI(false);
            }
        }

        /// <summary>
        /// 行からパーティションキーの値を取得する
        /// </summary>
        private string GetPartitionKeyValue(DataGridViewRow row)
        {
            // パーティションキーの列名を取得（実際の環境に合わせて調整が必要）            // パーティションキーとして "Folder" 列を使用
            if (!row.DataGridView.Columns.Contains("Folder"))
            {
                return null;
            }

            return row.Cells["Folder"].Value?.ToString();
        }

        /// <summary>
        /// レコードを挿入するユーザーがデータを入力し、新しいレコードが Cosmos DB に追加される
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private async void buttonInsert_Click(object sender, EventArgs e)
        {
            if (CosmosDBFactory.IsSqlApiMode())
            {
                if (string.IsNullOrWhiteSpace(_jsonData.Text))
                {
                    return;
                }

                // SQL APIモード
                using (var formInsert = new FormInsert(_cosmosDBService, _jsonData.Text))
                {
                    if (formInsert.ShowDialog() == DialogResult.OK)
                    {
                        await UpdateDatagridView();
                    }
                }
            }
            else
            {
                // Table APIモード
                using (var formInsert = new FormInsert(_tableAPIService))
                {
                    if (formInsert.ShowDialog() == DialogResult.OK)
                    {
                        await UpdateDatagridViewForTableAPI();
                    }
                }
            }
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
        /// 文字列をJSON形式として解析し、JTokenに変換する。
        /// 解析に失敗した場合は元の文字列をそのまま返す。
        /// </summary>
        /// <param name="valueString">解析対象の文字列</param>
        /// <returns>解析されたJTokenまたは元の文字列をJValueとして</returns>
        private JToken TryParseJson(string valueString)
        {
            if (string.IsNullOrWhiteSpace(valueString))
            {
                return new JValue(string.Empty);
            }

            // JSON形式として解析を試みる
            try
            {
                if ((valueString.StartsWith("{") && valueString.EndsWith("}")) ||
                    (valueString.StartsWith("[") && valueString.EndsWith("]")))
                {
                    return JToken.Parse(valueString);
                }
            }
            catch (JsonReaderException)
            {
                // JSON解析エラーの場合は何もせず次へ
            }

            // 数値に変換を試みる
            if (decimal.TryParse(valueString, out decimal decimalValue))
            {
                return new JValue(decimalValue);
            }

            // 日付形式に変換を試みる
            if (DateTime.TryParse(valueString, out DateTime dateValue))
            {
                return new JValue(dateValue);
            }

            // ブール値に変換を試みる
            if (bool.TryParse(valueString, out bool boolValue))
            {
                return new JValue(boolValue);
            }

            // 特別な形式に該当しない場合は文字列として返す
            return new JValue(valueString);
        }

        /// <summary>
        /// TableAPI用の読み取り専用列設定
        /// </summary>
        /// <param name="dataTable">表示するデータを格納したDataTable</param>
        private void SetReadOnlyColumnsForTableAPI(DataTable dataTable)
        {
            try
            {
                if (_tableAPIService == null) return;

                var readOnlyColumns = _tableAPIService.systemColumns;

                foreach (DataColumn column in dataTable.Columns)
                {
                    if (readOnlyColumns.Contains(column.ColumnName))
                    {
                        column.ReadOnly = true;
                        if (dataGridViewResults.Columns.Contains(column.ColumnName))
                        {
                            dataGridViewResults.Columns[column.ColumnName].ReadOnly = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"列の読み取り専用設定中にエラーが発生しました: {ex.Message}");
            }
        }

        /// <summary>
        /// Cosmos DB Table APIでテーブルを作成する
        /// </summary>
        private async Task<bool> CreateTable()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cmbBoxContainerName.Text))
                {
                    MessageBox.Show("テーブル名が設定されていません。テーブル名を入力してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // TableAPIServiceが未初期化の場合は初期化
                if (_tableAPIService == null)
                {
                    try
                    {
                        _tableAPIService = CosmosDBFactory.CreateTableApiService(
                            textBoxConnectionString.Text,
                            cmbBoxContainerName.Text);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to initialize Table API service: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }

                var createResult = await _tableAPIService.CreateTableAsync();
                if (!createResult.Success)
                {
                    MessageBox.Show(createResult.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                MessageBox.Show($"テーブル '{cmbBoxContainerName.Text}' が正常に作成されました。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"テーブル作成中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// コンボボックスの選択変更時イベントハンドラ
        /// </summary>
        private void cmbBoxContainerName_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cmbBoxContainerName.Text))
                    return;
                // SQL APIモードの場合
                if (CosmosDBFactory.IsSqlApiMode())
                {
                    if (_cosmosDBService != null)
                    {
                        // コンテナ名を設定（メソッド呼び出しを使用）
                        _cosmosDBService = CosmosDBFactory.CreateSqlApiService(
                            textBoxConnectionString.Text,
                            textBoxDatabaseName.Text,
                            cmbBoxContainerName.Text);
                        DisplayContainerSettings();
                    }
                }
                // Table APIモードの場合
                else
                {
                    if (_tableAPIService != null)
                    {
                        // テーブル名を設定（メソッド呼び出しを使用）
                        _tableAPIService = CosmosDBFactory.CreateTableApiService(
                            textBoxConnectionString.Text, cmbBoxContainerName.Text);
                        DisplayContainerSettings();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred while selecting container/table: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// リッチテキストボックスのマウスアップイベントハンドラ（ハイパーリンク対応）
        /// </summary>
        private void richTextBoxSelectedCell_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (_useHyperlinkHandler && _hyperlinkHandler != null)
                {
                    // マウス位置のハイパーリンクをクリックした場合の処理
                    _hyperlinkHandler.HandleMouseUpText(e, richTextBoxSelectedCell);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error occurred during link click processing: {ex.Message}");
            }
        }

        /// <summary>
        /// DataGridView のセルフォーマット時に、読み取り専用のカラムに背景色と文字色を設定する処理
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">セルフォーマットイベントデータ</param>
        private void dataGridViewResults_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (_virtualModeEnabled)
            {
                // 仮想モードの場合、列の読み取り専用状態を直接確認
                var column = dataGridViewResults.Columns[e.ColumnIndex];
                if (column.ReadOnly)
                {
                    e.CellStyle.BackColor = System.Drawing.Color.DarkGray;
                    e.CellStyle.ForeColor = System.Drawing.Color.White;
                }
            }
            else
            {
                // 通常モードの場合、従来通りの処理
                foreach (DataGridViewColumn column in dataGridViewResults.Columns)
                {
                    if (column.ReadOnly)
                    {
                        dataGridViewResults.Rows[e.RowIndex].Cells[column.Index].Style.BackColor = System.Drawing.Color.DarkGray;
                        dataGridViewResults.Rows[e.RowIndex].Cells[column.Index].Style.ForeColor = System.Drawing.Color.White;
                    }
                }
            }
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
                $"Do you want to delete the selected records? Count:{selectedRows.Count}",
                "Info",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes)
            {
                return;
            }

            // タイマーを開始
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var deletedIds = new List<string>();
            int deletedCount = 0;

            try
            {
                dataGridViewResults.ReadOnly = true;
                dataGridViewResults.SuspendLayout();

                var tasks = new List<Task>();
                var maxDegreeOfParallelism = Environment.ProcessorCount;
                var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

                // 選択行のインデックスをコピー (削除後も元のインデックスを保持するため)
                var rowIndexes = new List<int>();
                foreach (DataGridViewRow row in selectedRows)
                {
                    rowIndexes.Add(row.Index);
                }

                // 降順でソート（インデックスの大きい行から削除して、インデックスのズレを防ぐ）
                rowIndexes.Sort((a, b) => b.CompareTo(a));

                foreach (var rowIndex in rowIndexes)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            JObject jsonObject = BuildJsonObjectFromRow(rowIndex);

                            var id = jsonObject["id"]?.ToString();
                            if (!string.IsNullOrEmpty(id))
                            {
                                lock (deletedIds)
                                {
                                    deletedIds.Add($"id:{id}");
                                }
                            }

                            await DeleteRow(jsonObject);

                            // 削除件数をカウント
                            Interlocked.Increment(ref deletedCount);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);

                // 仮想モードの場合は、キャッシュされたDataTableからも削除
                if (_virtualModeEnabled && _virtualDataTable != null)
                {
                    // UI更新を最小限にするため一度だけUpdateDatagridViewを呼ぶ
                    await UpdateDatagridView();
                }
                else
                {
                    // 通常モードの場合は従来通りの更新
                    await UpdateDatagridView();
                }

                // タイマーを停止
                stopwatch.Stop();

                var message = $"Finish! Elapsed: {stopwatch.ElapsedMilliseconds / 1000.0:F2}s - Deleted: {deletedCount} records\n";
                // 削除したIDをメッセージボックスで表示
                if (deletedIds.Count > 10)
                {
                    message += $"Deleted ID Count: {deletedIds.Count}";
                }
                else if (deletedIds.Count > 0)
                {
                    message += $"Deleted IDs:\n{string.Join("\n", deletedIds)}";
                }

                MessageBox.Show(message, "Info");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            finally
            {
                dataGridViewResults.ResumeLayout();
                dataGridViewResults.ReadOnly = false;
            }
        }

        /// <summary>
        /// 行削除
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns>task</returns>
        private async Task DeleteRow(JObject jsonObject)
        {
            // JSONオブジェクトからidを取得
            var id = jsonObject["id"].ToString();

            // PartitionKeyを自動的に解決して取得
            var partitionKey = await _cosmosDBService.ResolvePartitionKeyAsync(jsonObject);
            var partitionKeyInfo = _cosmosDBService.GetPartitionKeyValues(jsonObject);

            var response = await _cosmosDBService.DeleteItemAsync<object>(id, partitionKey);
        }

        /// <summary>
        /// APIモードに応じてクエリテキストボックスを設定
        /// </summary>
        private void SetupQueryTextBox()
        {
            try
            {
                if (CosmosDBFactory.IsSqlApiMode())
                {
                    _textBoxQuery.Language = Language.SQL;
                    _textBoxQuery.Text = "SELECT * FROM c";
                    _textBoxQuery.ForeColor = Color.DarkBlue;

                    // カスタムスタイルを設定（FastColoredTextBoxの実装に合わせて）
                    _textBoxQuery.SyntaxHighlighter.StringStyle = new TextStyle(Brushes.Red, null, FontStyle.Regular);
                    _textBoxQuery.SyntaxHighlighter.NumberStyle = new TextStyle(Brushes.DarkGreen, null, FontStyle.Regular);
                    _textBoxQuery.SyntaxHighlighter.KeywordStyle = new TextStyle(Brushes.Blue, null, FontStyle.Bold);
                    _textBoxQuery.SyntaxHighlighter.CommentStyle = new TextStyle(Brushes.Green, null, FontStyle.Italic);
                }
                else // Table API Mode
                {
                    _textBoxQuery.Language = Language.Custom;
                    _textBoxQuery.Text = string.Empty;
                    _textBoxQuery.ForeColor = Color.Black;

                    // テーブルクエリのテキスト入力フィールドに説明書きを追加
                    var queryHelpText =
                        "/* Table APIのクエリ入力欄\n" +
                        " * フィルタリング例:\n" +
                        " * PartitionKey eq 'your-partition-key' and RowKey gt '100'\n" +
                        " * \n" +
                        " * 演算子: eq (=), ne (!=), gt (>), ge (>=), lt (<), le (<=)\n" +
                        " * 論理演算子: and, or, not\n" +
                        " */";

                    _textBoxQuery.Text = queryHelpText;
                    _textBoxQuery.SelectionStart = 0;
                    _textBoxQuery.SelectionLength = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error occurred while setting up query textbox: {ex.Message}");
            }
        }
    }
}
