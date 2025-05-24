using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
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
        private DataTable _originalDataTable; // フィルタリング時のオリジナルデータ保持用
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
            AutoScaleMode = AutoScaleMode.Dpi;            SetupDatagridview();

            _textBoxQuery = new FastColoredTextBox();
            _textBoxQuery.Dock = DockStyle.Fill;
            _textBoxQuery.ImeMode = ImeMode.Hiragana;
            _textBoxQuery.BorderStyle = BorderStyle.Fixed3D;
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
            
            // APIモードに応じたクエリテキストボックスの設定
            SetupQueryTextBox();
            
            _useHyperlinkHandler = configuration.GetValue<bool>("AppSettings:EnableHyperlinkHandler");
            _maxItemCount = configuration.GetValue<int>("AppSettings:MaxItemCount");
            var connectionString = configuration.GetValue<string>("AppSettings:ConnectionString");
              // モードに応じた初期値とUI設定
            if (CosmosDBFactory.IsSqlApiMode())
            {
                var databaseName = configuration.GetValue<string>("AppSettings:DatabaseName");
                var containerName = configuration.GetValue<string>("AppSettings:ContainerName");

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
                        cmbBoxContainerName.Text);
                    
                    if (!string.IsNullOrWhiteSpace(tableName))
                    {
                        LoadTablesIntoComboBox();
                        DisplayTableSettings();
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

            try
            {                _cosmosDBService = new CosmosDBService(textBoxConnectionString.Text, textBoxDatabaseName.Text, cmbBoxContainerName.Text);
                if (!string.IsNullOrWhiteSpace(textBoxDatabaseName.Text))
                {
                    LoadContainersIntoComboBox(textBoxDatabaseName.Text);
                    DisplayContainerSettings();
                }
            }
            catch (Exception)
            {
            }

            // テーブル情報表示用のテキストボックスを初期化
            textBoxInfo = new TextBox();
            textBoxInfo.Multiline = true;
            textBoxInfo.ReadOnly = true;
            textBoxInfo.ScrollBars = ScrollBars.Vertical;
            textBoxInfo.Dock = DockStyle.Fill;
            textBoxInfo.Font = new Font("Yu Gothic UI", 9);
            tabPage1.Controls.Add(textBoxInfo);
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
        }        /// <summary>
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

                // 大量データの場合は進捗表示を行う
                ShowProgressUI(true, "データを読み込んでいます...");

                if (CosmosDBFactory.IsSqlApiMode())
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
                else // Table API Mode
                {
                    _tableAPIService = CosmosDBFactory.CreateTableApiService(
                        textBoxConnectionString.Text,
                        cmbBoxContainerName.Text);

                    await UpdateDatagridViewForTableAPI();
                    DisplayTableSettings();
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
        }        /// <summary>
        /// Table APIのデータでDataGridViewを更新する
        /// </summary>
        private async Task UpdateDatagridViewForTableAPI()
        {
            ShowProgressUI(true, "Table APIからデータを取得中...");

            try
            {
                // テーブル名が空の場合はエラーメッセージを表示
                if (string.IsNullOrWhiteSpace(cmbBoxContainerName.Text))
                {
                    MessageBox.Show("テーブル名が設定されていません。テーブル名を入力して再度実行してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    {
                        MessageBox.Show($"Table APIサービスの初期化に失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    foreach (DataColumn column in result.Data.Columns)
                    {
                        if (_tableAPIService.systemColumns.Contains(column.ColumnName))
                        {
                            column.ReadOnly = true;
                        }
                    }
                    
                    dataGridViewResults.DataSource = result.Data;
                }

                // ステータス更新
                UpdateStatusStrip(result.TotalRequestCharge, result.DocumentCount, result.PageCount, result.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データの取得中にエラーが発生しました：{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        /// ユーザー確認後に、Cosmos DB へアップサート（更新または挿入）される        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private async void buttonUpdate_Click(object sender, EventArgs e)
        {
            if (dataGridViewResults.SelectedRows.Count == 0)
            {
                MessageBox.Show("更新するレコードを選択してください。", "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            int selectedRowIndex = dataGridViewResults.SelectedRows[0].Index;
            
            try
            {
                if (CosmosDBFactory.IsSqlApiMode())
                {
                    // SQL APIモード
                    JObject jsonObject = BuildJsonObjectFromRow(selectedRowIndex);
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
                MessageBox.Show($"更新処理中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        /// レコードを挿入するユーザーがデータを入力し、新しいレコードが Cosmos DB に追加される        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private async void buttonInsert_Click(object sender, EventArgs e)
        {
            if (CosmosDBFactory.IsSqlApiMode())
            {
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
        /// JSON文字列を再帰的にパースし、JTokenオブジェクトに変換する
        /// Cosmos DB で扱える型を考慮し、整数と浮動小数点数を区別
        /// </summary>
        /// <param name="item">Json項目</param>
        /// <returns>JToken</returns>
        private JToken TryParseJson(string item)
        {
            // Null値の判定
            if (string.IsNullOrEmpty(item) || item.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return JValue.CreateNull();
            }

            // 真偽値の判定
            if (bool.TryParse(item, out var boolValue))
            {
                return JToken.FromObject(boolValue);
            }

            // 整数の判定
            if (int.TryParse(item, out var intValue))
            {
                return JToken.FromObject(intValue);
            }

            // 浮動小数点数の判定
            if (double.TryParse(item, out var doubleValue))
            {
                return JToken.FromObject(doubleValue);
            }

            // JSONオブジェクトまたは配列の判定
            try
            {
                var parsedToken = JToken.Parse(item);
                if (parsedToken.Type == JTokenType.Object || parsedToken.Type == JTokenType.Array)
                {
                    // 再帰的に子要素を処理
                    foreach (var child in parsedToken.Children())
                    {
                        if (child is JProperty property)
                        {
                            property.Value = TryParseJson(property.Value.ToString());
                        }
                        else if (child is JArray array)
                        {
                            for (int i = 0; i < array.Count; i++)
                            {
                                array[i] = TryParseJson(array[i].ToString());
                            }
                        }
                    }
                }
                return parsedToken;
            }
            catch (JsonReaderException)
            {
                // JSONとしてパースできない場合はそのまま文字列として扱う
            }

            // 文字列として扱う
            return JToken.FromObject(item);
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
        }        /// <summary>
        /// 行削除処理
        /// </summary>
        /// <param name="selectedRows">選択行</param>
        /// <returns>task</returns>
        private async Task DeleteSelectedRows(DataGridViewSelectedRowCollection selectedRows)
        {
            // 削除確認ダイアログを表示
            DialogResult result = MessageBox.Show(
                $"選択したレコードを削除しますか？ 件数:{selectedRows.Count}",
                "確認",
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
                        {                            if (CosmosDBFactory.IsSqlApiMode())
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
                            }
                            else // Table APIモード
                            {
                                // 行データを取得
                                DataRow rowData = null;
                                if (_virtualModeEnabled && _virtualDataTable != null)
                                {
                                    if (rowIndex < _virtualDataTable.Rows.Count)
                                    {
                                        rowData = _virtualDataTable.Rows[rowIndex];
                                    }
                                }
                                else if (dataGridViewResults.DataSource is DataTable dataTable)
                                {
                                    if (rowIndex < dataTable.Rows.Count)
                                    {
                                        rowData = dataTable.Rows[rowIndex];
                                    }
                                }

                                if (rowData != null)
                                {
                                    // PartitionKeyとRowKeyを取得して識別子として記録
                                    var partitionKey = rowData["PartitionKey"]?.ToString();
                                    var rowKey = rowData["RowKey"]?.ToString();
                                    
                                    if (!string.IsNullOrEmpty(partitionKey) && !string.IsNullOrEmpty(rowKey))
                                    {
                                        lock (deletedIds)
                                        {
                                            deletedIds.Add($"PK:{partitionKey}, RK:{rowKey}");
                                        }
                                    }
                                    
                                    await DeleteTableRow(rowData);
                                }
                            }

                            // 削除件数をカウント
                            Interlocked.Increment(ref deletedCount);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);                // APIモードに応じてデータを更新
                if (CosmosDBFactory.IsSqlApiMode())
                {
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
                }
                else // Table APIモード
                {
                    // Table APIのデータを更新
                    await UpdateDatagridViewForTableAPI();
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
        /// Table APIモード用にテーブル一覧をコンボボックスにロードする
        /// </summary>
        private async void LoadTablesIntoComboBox()
        {
            try
            {
                ShowProgressUI(true, "テーブル一覧を取得中...");

                if (_tableAPIService == null)
                {
                    _tableAPIService = CosmosDBFactory.CreateTableApiService(
                        textBoxConnectionString.Text,
                        cmbBoxContainerName.Text);
                }

                var tableNames = await _tableAPIService.GetTableNamesAsync();

                cmbBoxContainerName.Items.Clear();
                foreach (var tableName in tableNames)
                {
                    cmbBoxContainerName.Items.Add(tableName);
                }

                if (!string.IsNullOrEmpty(cmbBoxContainerName.Text) && tableNames.Contains(cmbBoxContainerName.Text))
                {
                    cmbBoxContainerName.SelectedItem = cmbBoxContainerName.Text;
                }
                else if (tableNames.Count > 0)
                {
                    cmbBoxContainerName.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"テーブル一覧の取得中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ShowProgressUI(false);
            }
        }
          /// <summary>
        /// テーブルの設定情報を表示
        /// </summary>
        private async void DisplayTableSettings()
        {
            try
            {
                // テーブル名が空の場合は情報を表示しない
                if (string.IsNullOrWhiteSpace(cmbBoxContainerName.Text))
                {
                    if (textBoxInfo != null)
                    {
                        textBoxInfo.Text = "テーブル名が設定されていません。テーブル名を入力して再度実行してください。";
                    }
                    return;
                }

                // TableAPIServiceが未初期化の場合は初期化を試みる
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
                        if (textBoxInfo != null)
                        {
                            textBoxInfo.Text = $"Table APIサービスの初期化に失敗しました: {ex.Message}";
                        }
                        return;
                    }
                }

                ShowProgressUI(true, "テーブル情報を取得中...");

                dynamic tableProperties = await _tableAPIService.GetTablePropertiesAsync();

                // プロパティ表示用のリストを作成
                var infoText = new List<string>
                {
                    $"テーブル名: {tableProperties.Name}",
                    $"存在: {(tableProperties.Exists ? "はい" : "いいえ")}"
                };
                
                if (tableProperties.Exists)
                {
                    infoText.Add($"URI: {tableProperties.Uri}");
                }                // テキストボックスに表示
                if (textBoxInfo != null)
                {
                    textBoxInfo.Text = string.Join(Environment.NewLine, infoText);
                }
            }
            catch (Exception ex)
            {
                if (textBoxInfo != null)
                {
                    textBoxInfo.Text = $"テーブル情報の取得中にエラーが発生しました: {ex.Message}";
                }
            }
            finally
            {
                ShowProgressUI(false);
            }
        }

        /// <summary>
        /// Table APIのデータ行を削除する
        /// </summary>
        /// <param name="rowData">削除する行データ</param>
        /// <returns>削除結果の成否</returns>
        private async Task<bool> DeleteTableRow(DataRow rowData)
        {
            try
            {
                if (_tableAPIService == null)
                {
                    MessageBox.Show("Table APIサービスが初期化されていません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                // PartitionKeyとRowKeyが必須
                if (!rowData.Table.Columns.Contains("PartitionKey") || !rowData.Table.Columns.Contains("RowKey"))
                {
                    MessageBox.Show("必須のキー情報（PartitionKey、RowKey）が見つかりません。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                
                string partitionKey = rowData["PartitionKey"].ToString();
                string rowKey = rowData["RowKey"].ToString();
                
                // Table APIでエンティティを削除
                await _tableAPIService.DeleteEntityAsync(partitionKey, rowKey);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データ削除中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// appsettings.jsonファイルの設定を更新する
        /// </summary>
        /// <param name="containerOrTableName">選択されたコンテナ/テーブル名</param>
        private void UpdateAppSettings(string containerOrTableName)
        {
            try
            {
                // appsettings.jsonのパスを取得
                string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                // JSONファイルを読み込む
                string json = File.ReadAllText(appSettingsPath);
                var jsonObject = JObject.Parse(json);
                
                // 接続設定の更新
                jsonObject["AppSettings"]["ConnectionString"] = textBoxConnectionString.Text;
                
                if (CosmosDBFactory.IsSqlApiMode())
                {
                    // SQL APIモードの設定更新
                    jsonObject["AppSettings"]["DatabaseName"] = textBoxDatabaseName.Text;
                    jsonObject["AppSettings"]["ContainerName"] = containerOrTableName;
                }
                else
                {
                    // Table APIモードの設定更新
                    jsonObject["AppSettings"]["TableName"] = containerOrTableName;
                }
                
                // 更新したJSONを書き込み
                File.WriteAllText(appSettingsPath, jsonObject.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定ファイルの更新中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// コンボボックスでコンテナ/テーブルの選択が変更された時の処理
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private async void cmbBoxContainerName_SelectedIndexChanged(object sender, EventArgs e)
        {
            string containerOrTableName = cmbBoxContainerName.Text;
            if (string.IsNullOrEmpty(containerOrTableName)) return;
            
            try
            {
                if (CosmosDBFactory.IsSqlApiMode())
                {
                    // SQL APIモード - コンテナを更新
                    _cosmosDBService = CosmosDBFactory.CreateSqlApiService(
                        textBoxConnectionString.Text,
                        textBoxDatabaseName.Text,
                        containerOrTableName);
                        
                    DisplayContainerSettings();
                }
                else
                {
                    // Table APIモード - テーブルを更新
                    _tableAPIService = CosmosDBFactory.CreateTableApiService(
                        textBoxConnectionString.Text,
                        containerOrTableName);
                        
                    DisplayTableSettings();
                }
                
                // ConfigファイルのAppSettingsを更新
                UpdateAppSettings(containerOrTableName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"コンテナ/テーブル設定の更新中にエラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// APIモードに応じてクエリテキストボックスを設定する
        /// </summary>
        private void SetupQueryTextBox()
        {
            if (CosmosDBFactory.IsSqlApiMode())
            {
                // SQL APIモードのクエリ例
                _textBoxQuery.Language = Language.SQL;
                _textBoxQuery.Text = "SELECT\n    * \nFROM\n    c \nWHERE\n    1 = 1";
            }
            else
            {
                // Table APIモードのクエリ例（ODataクエリ形式）
                _textBoxQuery.Language = Language.Custom;
                _textBoxQuery.Text = "PartitionKey eq 'your_partition_key'";
            }
        }
    }
}
