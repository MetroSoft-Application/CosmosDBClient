using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing.Drawing2D;
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
        private readonly int _maxItemCount;
        private readonly bool _useHyperlinkHandler;
        private HyperlinkHandler _hyperlinkHandler;
        private TabControl _queryTabControl;
        private FastColoredTextBox _jsonData;
        private AdvancedDataGridView dataGridViewResults;
        private TextStyle jsonStringStyle = new TextStyle(Brushes.Black, null, FontStyle.Regular);
        private DataTable _virtualDataTable;
        private DataTable _originalDataTable;
        private List<string> _columnNames;
        private bool _virtualModeEnabled = true;
        private HashSet<(int Row, int Column)> _modifiedCells = new HashSet<(int, int)>();
        private HashSet<int> _modifiedRows = new HashSet<int>();

        // コンテキストメニュー関連
        private ContextMenuStrip _cellContextMenu;
        private int _rightClickedRowIndex = -1;
        private int _rightClickedColumnIndex = -1;

        // ページング関連のフィールド
        private List<DataTable> _pageCache = new List<DataTable>();
        private List<string> _pageContinuationTokens = new List<string>();
        private int _currentPageIndex = 0;
        private int _totalFetchedDocuments = 0;
        private bool _isPagingMode = false;

        /// <summary>
        /// ページサイズを取得する
        /// </summary>
        private int GetPageSize()
        {
            return (int)numericUpDownPageSize.Value;
        }

        /// <summary>
        /// FormMain クラスのコンストラクタ設定を読み込み、CosmosDBServiceのインスタンスを初期化する
        /// </summary>
        public FormMain()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.Dpi;

            SetupDatagridview();
            SetupQueryTabs();

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
            dataGridViewResults.CellValueChanged += dataGridViewResults_CellValueChanged;
            dataGridViewResults.RowPostPaint += dataGridViewResults_RowPostPaint;
            dataGridViewResults.KeyUp += dataGridViewResults_KeyUp;
            dataGridViewResults.CellMouseClick += dataGridViewResults_CellMouseClick;

            // セル用コンテキストメニューの設定
            SetupCellContextMenu();

            splitContainer2.Panel1.Controls.Add(dataGridViewResults);
        }

        /// <summary>
        /// セル用コンテキストメニューの設定を行う
        /// </summary>
        private void SetupCellContextMenu()
        {
            _cellContextMenu = new ContextMenuStrip();
            _cellContextMenu.Opening += CellContextMenu_Opening;
        }

        /// <summary>
        /// コンテキストメニューが開かれる際の処理
        /// </summary>
        private void CellContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _cellContextMenu.Items.Clear();

            // 右クリックされた位置が有効かチェック
            if (_rightClickedRowIndex < 0 || _rightClickedColumnIndex < 0 ||
                _rightClickedRowIndex >= dataGridViewResults.RowCount ||
                _rightClickedColumnIndex >= dataGridViewResults.ColumnCount)
            {
                e.Cancel = true;
                return;
            }

            // 列名と値を取得
            string columnName = _columnNames[_rightClickedColumnIndex];
            object cellValue = _virtualDataTable.Rows[_rightClickedRowIndex][_rightClickedColumnIndex];
            string displayValue = cellValue?.ToString() ?? string.Empty;

            // 表示用の値を短縮
            if (displayValue.Length > 50)
            {
                displayValue = displayValue.Substring(0, 47) + "...";
            }

            // メニュー項目を作成
            var equalMenuItem = new ToolStripMenuItem($"Add to WHERE: {columnName} = '{displayValue}'");
            equalMenuItem.Click += (s, args) => AddWhereCondition(columnName, cellValue?.ToString() ?? string.Empty, "=");

            var notEqualMenuItem = new ToolStripMenuItem($"Add to WHERE: {columnName} != '{displayValue}'");
            notEqualMenuItem.Click += (s, args) => AddWhereCondition(columnName, cellValue?.ToString() ?? string.Empty, "!=");

            var likeMenuItem = new ToolStripMenuItem($"Add to WHERE: {columnName} LIKE '%{displayValue}%'");
            likeMenuItem.Click += (s, args) => AddWhereCondition(columnName, cellValue?.ToString() ?? string.Empty, "LIKE");

            var notLikeMenuItem = new ToolStripMenuItem($"Add to WHERE: {columnName} NOT LIKE '%{displayValue}%'");
            notLikeMenuItem.Click += (s, args) => AddWhereCondition(columnName, cellValue?.ToString() ?? string.Empty, "NOT LIKE");

            _cellContextMenu.Items.Add(equalMenuItem);
            _cellContextMenu.Items.Add(notEqualMenuItem);
            _cellContextMenu.Items.Add(likeMenuItem);
            _cellContextMenu.Items.Add(notLikeMenuItem);
        }

        /// <summary>
        /// クエリタブの設定を行う
        /// </summary>
        private void SetupQueryTabs()
        {
            _queryTabControl = new TabControl();
            _queryTabControl.Dock = DockStyle.Fill;
            _queryTabControl.Alignment = TabAlignment.Top;
            _queryTabControl.HotTrack = true;
            _queryTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;

            // タブの描画イベントを追加（アクティブなタブを強調表示）
            _queryTabControl.DrawItem += QueryTabControl_DrawItem;

            // ダブルクリックで新しいタブを追加
            _queryTabControl.DoubleClick += (sender, e) => AddNewQueryTab();

            // キーボードイベントの処理
            _queryTabControl.KeyDown += QueryTabControl_KeyDown;

            // タブ選択変更時の再描画
            _queryTabControl.SelectedIndexChanged += (sender, e) => _queryTabControl.Invalidate();

            // 右クリックメニューを追加
            var contextMenu = new ContextMenuStrip();
            var addTabMenuItem = new ToolStripMenuItem("Add New Tab (Ctrl+T)");
            var closeTabMenuItem = new ToolStripMenuItem("Close Tab (Ctrl+W)");
            var closeAllTabsMenuItem = new ToolStripMenuItem("Close All Tabs");
            var renameTabMenuItem = new ToolStripMenuItem("Rename Tab (F2)");

            addTabMenuItem.Click += (sender, e) => AddNewQueryTab();
            closeTabMenuItem.Click += (sender, e) => CloseCurrentTab();
            closeAllTabsMenuItem.Click += (sender, e) => CloseAllTabs();
            renameTabMenuItem.Click += (sender, e) => RenameCurrentTab();

            contextMenu.Items.AddRange(new ToolStripItem[] {
                addTabMenuItem,
                new ToolStripSeparator(),
                closeTabMenuItem,
                closeAllTabsMenuItem,
                new ToolStripSeparator(),
                renameTabMenuItem
            });
            _queryTabControl.ContextMenuStrip = contextMenu;

            panel1.Controls.Add(_queryTabControl);

            // 初期タブを作成
            AddNewQueryTab("Query 1", "SELECT\n    * \nFROM\n    c \nWHERE\n    1 = 1");
        }

        /// <summary>
        /// クエリタブコントロールのキーボードイベントを処理
        /// </summary>
        private void QueryTabControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.T)
            {
                // Ctrl+T: 新しいタブを追加
                AddNewQueryTab();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                // Ctrl+W: 現在のタブを閉じる
                CloseCurrentTab();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                // F2: タブ名を変更
                RenameCurrentTab();
                e.Handled = true;
            }
        }

        /// <summary>
        /// タブのカスタム描画処理（アクティブなタブを強調表示）
        /// </summary>
        private void QueryTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabControl = sender as TabControl;
            var tabPage = tabControl.TabPages[e.Index];
            var tabRect = tabControl.GetTabRect(e.Index);

            // アクティブなタブかどうか判定
            bool isSelected = (e.Index == tabControl.SelectedIndex);

            // 標準的なWindows Formsのテーマに合わせた色設定
            var backColor = isSelected ? SystemColors.Window : SystemColors.Control;
            var textColor = isSelected ? SystemColors.WindowText : SystemColors.ControlText;
            var borderColor = SystemColors.ControlDark;

            // 背景を描画
            using (var brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, tabRect);
            }

            // 境界線を描画（Windowsの標準的なスタイル）
            using (var pen = new Pen(borderColor))
            {
                // 上と左右の境界線
                e.Graphics.DrawLine(pen, tabRect.Left, tabRect.Bottom, tabRect.Left, tabRect.Top + 2);
                e.Graphics.DrawLine(pen, tabRect.Left, tabRect.Top + 2, tabRect.Left + 2, tabRect.Top);
                e.Graphics.DrawLine(pen, tabRect.Left + 2, tabRect.Top, tabRect.Right - 3, tabRect.Top);
                e.Graphics.DrawLine(pen, tabRect.Right - 3, tabRect.Top, tabRect.Right - 1, tabRect.Top + 2);
                e.Graphics.DrawLine(pen, tabRect.Right - 1, tabRect.Top + 2, tabRect.Right - 1, tabRect.Bottom);

                // 非アクティブタブのみ下線を描画
                if (!isSelected)
                {
                    e.Graphics.DrawLine(pen, tabRect.Left, tabRect.Bottom - 1, tabRect.Right - 1, tabRect.Bottom - 1);
                }
            }

            // アクティブなタブの強調効果（内側の明るいハイライト）
            if (isSelected)
            {
                using (var pen = new Pen(SystemColors.ControlLightLight))
                {
                    e.Graphics.DrawLine(pen, tabRect.Left + 1, tabRect.Bottom - 1, tabRect.Left + 1, tabRect.Top + 3);
                    e.Graphics.DrawLine(pen, tabRect.Left + 1, tabRect.Top + 3, tabRect.Left + 3, tabRect.Top + 1);
                    e.Graphics.DrawLine(pen, tabRect.Left + 3, tabRect.Top + 1, tabRect.Right - 4, tabRect.Top + 1);
                }
            }

            // テキストを描画
            var textFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
            var font = tabControl.Font;

            // アクティブタブは若干太字で
            if (isSelected)
            {
                font = new Font(tabControl.Font, FontStyle.Bold);
            }

            TextRenderer.DrawText(e.Graphics, tabPage.Text, font, tabRect, textColor, textFlags);

            // フォントをクリーンアップ
            if (isSelected && font != tabControl.Font)
            {
                font.Dispose();
            }
        }

        /// <summary>
        /// 新しいクエリタブを追加する
        /// </summary>
        /// <param name="tabName">タブ名（省略時は自動生成）</param>
        /// <param name="initialQuery">初期クエリ（省略時はデフォルトクエリ）</param>
        private void AddNewQueryTab(string tabName = null, string initialQuery = null)
        {
            if (tabName == null)
            {
                tabName = $"Query {_queryTabControl.TabPages.Count + 1}";
            }

            if (initialQuery == null)
            {
                initialQuery = "SELECT\n    * \nFROM\n    c \nWHERE\n    1 = 1";
            }

            var tabPage = new TabPage(tabName);

            var textBoxQuery = new FastColoredTextBox();
            textBoxQuery.Language = Language.SQL;
            textBoxQuery.Dock = DockStyle.Fill;
            textBoxQuery.ImeMode = ImeMode.Hiragana;
            textBoxQuery.BorderStyle = BorderStyle.Fixed3D;
            textBoxQuery.Text = initialQuery;
            textBoxQuery.TabLength = 4;
            textBoxQuery.WordWrap = false;
            textBoxQuery.ShowLineNumbers = true;

            tabPage.Controls.Add(textBoxQuery);
            _queryTabControl.TabPages.Add(tabPage);
            _queryTabControl.SelectedTab = tabPage;
        }

        /// <summary>
        /// 現在のタブを閉じる
        /// </summary>
        private void CloseCurrentTab()
        {
            if (_queryTabControl.TabPages.Count <= 1)
            {
                MessageBox.Show("At least one tab is required.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedTab = _queryTabControl.SelectedTab;
            if (selectedTab != null)
            {
                _queryTabControl.TabPages.Remove(selectedTab);
            }
        }

        /// <summary>
        /// すべてのタブを閉じて初期状態に戻す
        /// </summary>
        private void CloseAllTabs()
        {
            _queryTabControl.TabPages.Clear();
            AddNewQueryTab("Query 1", "SELECT\n    * \nFROM\n    c \nWHERE\n    1 = 1");
        }

        /// <summary>
        /// 現在のタブ名を変更する
        /// </summary>
        private void RenameCurrentTab()
        {
            var selectedTab = _queryTabControl.SelectedTab;
            if (selectedTab == null) return;

            using (var inputForm = new Form())
            {
                inputForm.Text = "Rename Tab";
                inputForm.Size = new Size(300, 120);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var textBox = new TextBox
                {
                    Text = selectedTab.Text,
                    Location = new Point(20, 20),
                    Size = new Size(240, 20)
                };

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(110, 50),
                    Size = new Size(75, 23)
                };

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(190, 50),
                    Size = new Size(75, 23)
                };

                inputForm.Controls.AddRange(new Control[] { textBox, okButton, cancelButton });
                inputForm.AcceptButton = okButton;
                inputForm.CancelButton = cancelButton;

                if (inputForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    selectedTab.Text = textBox.Text;
                }
            }
        }

        /// <summary>
        /// 現在選択されているクエリテキストを取得する
        /// </summary>
        /// <returns>現在選択されているタブのクエリテキスト</returns>
        private string GetCurrentQueryText()
        {
            var selectedTab = _queryTabControl.SelectedTab;
            if (selectedTab?.Controls[0] is FastColoredTextBox textBox)
            {
                return textBox.Text;
            }
            return "SELECT * FROM c";
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

                // ページング状態をリセット
                _pageCache.Clear();
                _pageContinuationTokens.Clear();
                _currentPageIndex = 0;
                _totalFetchedDocuments = 0;

                // ページングモードの状態を読み取る
                _isPagingMode = checkBoxPagingMode.Checked;

                // 大量データの場合は進捗表示を行う
                ShowProgressUI(true, "Loading data...");

                _cosmosDBService = new CosmosDBService(textBoxConnectionString.Text, textBoxDatabaseName.Text, cmbBoxContainerName.Text);

                if (_isPagingMode)
                {
                    // ページングモードの場合
                    var query = BuildQuery(GetCurrentQueryText(), GetMaxItemCount());
                    var result = await _cosmosDBService.FetchDataPageAsync(query, GetPageSize());

                    // 最初のページをキャッシュに追加
                    _pageCache.Add(result.Data.Copy());
                    _pageContinuationTokens.Add(result.ContinuationToken);
                    _totalFetchedDocuments = result.DocumentCount;

                    // データを表示
                    await UpdateGridWithPageData(result);

                    // ボタンの状態を更新
                    UpdatePagingButtons();

                    UpdateStatusStrip(result.TotalRequestCharge, result.DocumentCount, result.PageCount, result.ElapsedMilliseconds);
                }
                else if (_virtualModeEnabled && GetMaxItemCount() > 1000)
                {
                    // 大量データの場合はバッファリングを使用
                    await UpdateVirtualDataGridView(BuildQuery(GetCurrentQueryText(), GetMaxItemCount()), 1000);
                }
                else
                {
                    // 通常の更新処理
                    await UpdateDatagridView();
                }

                DisplayContainerSettings();
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

                // カラムの並び替えを実施する（システムカラムを末尾に移動）
                var pkPaths = await _cosmosDBService.GetPartitionKeyPathsAsync();
                _cosmosDBService.MoveSystemColumnsToEnd(newDataTable, pkPaths);

                // 読み込み完了したデータで更新
                _virtualDataTable = newDataTable;

                ClearModificationMarks();

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
            var query = BuildQuery(GetCurrentQueryText(), GetMaxItemCount());
            var result = await _cosmosDBService.FetchDataWithStatusAsync(query, GetMaxItemCount());

            if (_virtualModeEnabled)
            {
                // 仮想モードの場合、DataTableを内部に保持して手動で管理
                _virtualDataTable = result.Data;
                _originalDataTable = null;

                ClearModificationMarks();

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

                ClearModificationMarks();
            }

            UpdateStatusStrip(result.TotalRequestCharge, result.DocumentCount, result.PageCount, result.ElapsedMilliseconds);
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

                    object originalValue = _virtualDataTable.Rows[e.RowIndex][columnName];
                    bool valueChanged = !object.Equals(originalValue, e.Value);

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

                    _virtualDataTable.Rows[e.RowIndex][columnName] = typedValue;

                    if (valueChanged)
                    {
                        _modifiedCells.Add((e.RowIndex, e.ColumnIndex));
                        _modifiedRows.Add(e.RowIndex);
                    }

                    if (!_virtualDataTable.Rows[e.RowIndex].RowState.HasFlag(DataRowState.Modified))
                    {
                        _virtualDataTable.Rows[e.RowIndex].SetModified();
                    }

                    // セルのスタイル更新
                    dataGridViewResults.InvalidateCell(e.ColumnIndex, e.RowIndex);
                    dataGridViewResults.InvalidateRow(e.RowIndex);

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
        /// セルと行の変更マークをクリアする
        /// </summary>
        private void ClearModificationMarks()
        {
            _modifiedCells.Clear();
            _modifiedRows.Clear();
        }

        /// <summary>
        /// 通常モード時のセル値変更イベントハンドラ
        /// </summary>
        private void dataGridViewResults_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && !_virtualModeEnabled)
            {
                _modifiedCells.Add((e.RowIndex, e.ColumnIndex));
                _modifiedRows.Add(e.RowIndex);

                dataGridViewResults.InvalidateCell(e.ColumnIndex, e.RowIndex);
                dataGridViewResults.InvalidateRow(e.RowIndex);
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
                        column.HeaderCell.Style.BackColor = System.Drawing.Color.DarkGray;
                        column.HeaderCell.Style.ForeColor = System.Drawing.Color.White;
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
        /// セルのマウスクリックイベントハンドラ
        /// 右クリック時にコンテキストメニューを表示
        /// </summary>
        private void dataGridViewResults_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // 右クリックかつ有効なセル位置の場合のみ処理
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                // データがロードされているか確認
                if (_virtualDataTable == null || _columnNames == null ||
                    e.RowIndex >= _virtualDataTable.Rows.Count ||
                    e.ColumnIndex >= _columnNames.Count)
                {
                    return;
                }

                // セルを選択状態にする
                dataGridViewResults.CurrentCell = dataGridViewResults[e.ColumnIndex, e.RowIndex];

                // 右クリックされたセルの位置を記憶
                _rightClickedRowIndex = e.RowIndex;
                _rightClickedColumnIndex = e.ColumnIndex;

                // セルの実際の位置を計算してコンテキストメニューを表示
                var cellBounds = dataGridViewResults.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                var menuPosition = new Point(cellBounds.Right, cellBounds.Bottom);

                // DataGridView内での位置をスクリーン座標に変換
                menuPosition = dataGridViewResults.PointToScreen(menuPosition);

                // コンテキストメニューを表示
                _cellContextMenu.Show(menuPosition);
            }
        }

        /// <summary>
        /// WHERE句に条件を追加する
        /// </summary>
        /// <param name="columnName">列名</param>
        /// <param name="value">値</param>
        /// <param name="operator">演算子 (=, !=, LIKE, または NOT LIKE)</param>
        private void AddWhereCondition(string columnName, string value, string @operator)
        {
            try
            {
                // 現在のクエリテキストを取得
                var currentQuery = GetCurrentQueryText();
                if (string.IsNullOrWhiteSpace(currentQuery))
                {
                    MessageBox.Show("Query is empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 値をエスケープ（シングルクォートを二重に）
                string escapedValue = value.Replace("'", "''");

                // FROM句からエイリアスを抽出
                string alias = ExtractAliasFromQuery(currentQuery);

                // WHERE句の条件文を作成（エイリアスがあれば付加）
                string condition;
                if (string.Equals(@operator, "LIKE", StringComparison.OrdinalIgnoreCase))
                {
                    // LIKE演算子の場合、ワイルドカードで囲む
                    condition = string.IsNullOrEmpty(alias)
                        ? $"{columnName} LIKE '%{escapedValue}%'"
                        : $"{alias}.{columnName} LIKE '%{escapedValue}%'";
                }
                else if (string.Equals(@operator, "NOT LIKE", StringComparison.OrdinalIgnoreCase))
                {
                    // NOT LIKE演算子の場合、ワイルドカードで囲む
                    condition = string.IsNullOrEmpty(alias)
                        ? $"{columnName} NOT LIKE '%{escapedValue}%'"
                        : $"{alias}.{columnName} NOT LIKE '%{escapedValue}%'";
                }
                else
                {
                    condition = string.IsNullOrEmpty(alias)
                        ? $"{columnName} {@operator} '{escapedValue}'"
                        : $"{alias}.{columnName} {@operator} '{escapedValue}'";
                }

                // WHERE句の検出と追加
                string updatedQuery;
                var whereMatch = System.Text.RegularExpressions.Regex.Match(
                    currentQuery,
                    @"\bWHERE\b",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (whereMatch.Success)
                {
                    // 既存のWHERE句がある場合、ANDで条件を追加
                    // WHERE句の終わりを見つける（ORDER BY、GROUP BY、またはクエリの終わりまで）
                    var endMatch = System.Text.RegularExpressions.Regex.Match(
                        currentQuery.Substring(whereMatch.Index),
                        @"\b(ORDER\s+BY|GROUP\s+BY)\b",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

                    int insertPosition;
                    if (endMatch.Success)
                    {
                        // ORDER BYやGROUP BYがある場合、その前に挿入
                        insertPosition = whereMatch.Index + endMatch.Index;
                        // 前の空白を確認して適切にインデント
                        var precedingText = currentQuery.Substring(0, insertPosition).TrimEnd();
                        var trailingWhitespace = currentQuery.Substring(precedingText.Length, insertPosition - precedingText.Length);
                        updatedQuery = precedingText + $"\n    AND {condition}" + trailingWhitespace + currentQuery.Substring(insertPosition);
                    }
                    else
                    {
                        // クエリの最後に追加
                        updatedQuery = currentQuery.TrimEnd() + $"\n    AND {condition}";
                    }
                }
                else
                {
                    // WHERE句がない場合、FROM句の後に追加
                    // FROM句を広範にマッチ（テーブル名、エイリアス、改行を含む）
                    var fromMatch = System.Text.RegularExpressions.Regex.Match(
                        currentQuery,
                        @"\bFROM\s+[\w.]+(?:\s+(?:AS\s+)?\w+)?",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

                    if (fromMatch.Success)
                    {
                        int insertPosition = fromMatch.Index + fromMatch.Length;

                        // FROM句の後の空白・改行をスキップして次の句の前に挿入
                        while (insertPosition < currentQuery.Length &&
                               (currentQuery[insertPosition] == ' ' ||
                                currentQuery[insertPosition] == '\t' ||
                                currentQuery[insertPosition] == '\r' ||
                                currentQuery[insertPosition] == '\n'))
                        {
                            insertPosition++;
                        }

                        // 適切な改行とインデントでWHERE句を挿入
                        updatedQuery = currentQuery.Substring(0, insertPosition) +
                                     $"WHERE\n    {condition}\n" +
                                     currentQuery.Substring(insertPosition);
                    }
                    else
                    {
                        // FROMが見つからない場合はクエリの最後に追加
                        updatedQuery = currentQuery.TrimEnd() + $"\nWHERE\n    {condition}";
                    }
                }

                // 現在のタブのテキストボックスを更新
                var selectedTab = _queryTabControl.SelectedTab;
                if (selectedTab?.Controls[0] is FastColoredTextBox textBox)
                {
                    textBox.Text = updatedQuery;
                    textBox.SelectionStart = textBox.Text.Length;
                    textBox.DoSelectionVisible();
                    textBox.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding WHERE condition: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// クエリからFROM句のエイリアスを抽出
        /// </summary>
        /// <param name="query">SQLクエリ文字列</param>
        /// <returns>エイリアス（存在しない場合は空文字列）</returns>
        private string ExtractAliasFromQuery(string query)
        {
            // パターン1: FROM table_name [AS] alias (2つの識別子、かつエイリアスがSQLキーワードでない)
            var matchWithAlias = System.Text.RegularExpressions.Regex.Match(
                query,
                @"\bFROM\s+([\w.]+)\s+(?:AS\s+)?(?!(?:WHERE|ORDER|GROUP|HAVING|LIMIT|OFFSET|UNION|INTERSECT|EXCEPT)\b)(\w+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (matchWithAlias.Success && matchWithAlias.Groups.Count > 2)
            {
                // 2番目のグループがエイリアス
                return matchWithAlias.Groups[2].Value;
            }

            // パターン2: FROM identifier のみ (1つの識別子のみの場合)
            var matchSingle = System.Text.RegularExpressions.Regex.Match(
                query,
                @"\bFROM\s+(\w+)\s*(?:\r|\n|$)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (matchSingle.Success && matchSingle.Groups.Count > 1)
            {
                return matchSingle.Groups[1].Value;
            }

            return string.Empty;
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
                        dataGridViewResults.Columns[column.ColumnName].HeaderCell.Style.BackColor = System.Drawing.Color.DarkGray;
                        dataGridViewResults.Columns[column.ColumnName].HeaderCell.Style.ForeColor = System.Drawing.Color.White;
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
        /// 変更された行のみを対象として、Cosmos DB へアップサート（更新または挿入）される
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private async void buttonUpdate_Click(object sender, EventArgs e)
        {
            if (_modifiedRows.Count == 0)
            {
                MessageBox.Show(
                    "No modified records found. Please modify data before updating.",
                    "Info",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Do you want to update {_modifiedRows.Count} modified records?",
                "Info",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes)
            {
                return;
            }

            var successCount = 0;
            var errorCount = 0;
            var totalRequestCharge = 0.0;
            var errorMessages = new List<string>();

            try
            {
                foreach (var rowIndex in _modifiedRows.ToList())
                {
                    try
                    {
                        // JSONオブジェクトを行データから構築
                        var jsonObject = BuildJsonObjectFromRow(rowIndex);

                        // JSONオブジェクトからidを取得
                        var id = jsonObject["id"]?.ToString();

                        // PartitionKeyを自動的に解決して取得
                        var partitionKey = await _cosmosDBService.ResolvePartitionKeyAsync(jsonObject);

                        // Cosmos DBにUpsert処理を実行
                        var response = await _cosmosDBService.UpsertItemAsync(jsonObject, partitionKey);
                        totalRequestCharge += response.RequestCharge;
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errorMessages.Add($"行 {rowIndex + 1}: {ex.Message}");
                    }
                }

                var message = $"Update completed!\n\nSuccess: {successCount} records\nFailed: {errorCount} records\nTotal request charge: {totalRequestCharge:F2}";

                if (errorMessages.Count > 0)
                {
                    message += "\n\nError details:\n" + string.Join("\n", errorMessages.Take(5));
                    if (errorMessages.Count > 5)
                    {
                        message += $"\n... {errorMessages.Count - 5} more errors";
                    }
                }

                MessageBox.Show(message, "Update Result");

                if (successCount > 0)
                {
                    ClearModificationMarks();
                    await UpdateDatagridView();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred during update process: {ex.Message}", "Error");
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
            var formInsert = new FormInsert(_cosmosDBService, _jsonData.Text);
            formInsert.FormClosed += async (s, args) => await UpdateDatagridView();
            formInsert.Show(this);
        }

        /// <summary>
        /// DataGridView の行描画後に行番号を表示するための処理
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">行描画イベントデータ</param>
        private void dataGridViewResults_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var brush = new System.Drawing.SolidBrush(dataGridViewResults.RowHeadersDefaultCellStyle.ForeColor);
            var backgroundColor = dataGridViewResults.RowHeadersDefaultCellStyle.BackColor;

            if (_modifiedRows.Contains(e.RowIndex))
            {
                backgroundColor = System.Drawing.Color.IndianRed;
                brush = new System.Drawing.SolidBrush(System.Drawing.Color.White);

                using (var backgroundBrush = new System.Drawing.SolidBrush(backgroundColor))
                {
                    var headerRect = new Rectangle(e.RowBounds.X, e.RowBounds.Y,
                                                 dataGridViewResults.RowHeadersWidth, e.RowBounds.Height);
                    e.Graphics.FillRectangle(backgroundBrush, headerRect);
                }

                e.Graphics.DrawString("*" + (e.RowIndex + 1).ToString(),
                                      new Font(dataGridViewResults.DefaultCellStyle.Font, FontStyle.Bold),
                                      brush,
                                      e.RowBounds.Location.X + 5,
                                      e.RowBounds.Location.Y + 4);
            }
            else
            {
                e.Graphics.DrawString((e.RowIndex + 1).ToString(),
                                      dataGridViewResults.DefaultCellStyle.Font,
                                      brush,
                                      e.RowBounds.Location.X + 15,
                                      e.RowBounds.Location.Y + 4);
            }

            brush.Dispose();
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

            if (_modifiedCells.Contains((e.RowIndex, e.ColumnIndex)))
            {
                e.CellStyle.BackColor = System.Drawing.Color.LightCoral;
                e.CellStyle.ForeColor = System.Drawing.Color.Black;
                return;
            }

            if (_modifiedRows.Contains(e.RowIndex))
            {
                e.CellStyle.BackColor = System.Drawing.Color.MistyRose;
                e.CellStyle.ForeColor = System.Drawing.Color.Black;
            }

            if (_virtualModeEnabled)
            {
                // 仮想モードの場合、列の読み取り専用状態を直接確認
                var column = dataGridViewResults.Columns[e.ColumnIndex];
                if (column.ReadOnly)
                {
                    if (!_modifiedCells.Contains((e.RowIndex, e.ColumnIndex)) && !_modifiedRows.Contains(e.RowIndex))
                    {
                        e.CellStyle.BackColor = System.Drawing.Color.DarkGray;
                        e.CellStyle.ForeColor = System.Drawing.Color.White;
                    }
                }
            }
            else
            {
                // 通常モードの場合、従来通りの処理
                foreach (DataGridViewColumn column in dataGridViewResults.Columns)
                {
                    if (column.ReadOnly)
                    {
                        if (!_modifiedCells.Contains((e.RowIndex, e.ColumnIndex)) && !_modifiedRows.Contains(e.RowIndex))
                        {
                            dataGridViewResults.Rows[e.RowIndex].Cells[column.Index].Style.BackColor = System.Drawing.Color.DarkGray;
                            dataGridViewResults.Rows[e.RowIndex].Cells[column.Index].Style.ForeColor = System.Drawing.Color.White;
                        }
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
        /// ページングモードチェックボックス変更イベント
        /// </summary>
        private void checkBoxPagingMode_CheckedChanged(object sender, EventArgs e)
        {
            _isPagingMode = checkBoxPagingMode.Checked;
            _pageCache.Clear();
            _pageContinuationTokens.Clear();
            _currentPageIndex = 0;
            _totalFetchedDocuments = 0;
            buttonPrevPage.Enabled = false;
            buttonNextPage.Enabled = false;
            labelPageInfo.Text = "0 / 0";
        }

        /// <summary>
        /// 前のページを表示
        /// </summary>
        private async void buttonPrevPage_Click(object sender, EventArgs e)
        {
            if (_currentPageIndex <= 0)
                return;

            try
            {
                ShowProgressUI(true, "Loading previous page...");

                // ページインデックスを戻す
                _currentPageIndex--;

                // キャッシュからデータを取得（再クエリなし）
                var cachedData = _pageCache[_currentPageIndex];

                // 累計ドキュメント数を再計算
                _totalFetchedDocuments = 0;
                for (int i = 0; i <= _currentPageIndex; i++)
                {
                    _totalFetchedDocuments += _pageCache[i].Rows.Count;
                }

                // データを表示
                await UpdateGridWithCachedPageData(cachedData);

                // ボタンの状態を更新
                UpdatePagingButtons();

                // ステータスバーを更新
                toolStripStatusLabel2.Text = $"Documents: {cachedData.Rows.Count}";
                toolStripStatusLabel4.Text = $"Pages: {_currentPageIndex + 1}";
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
        /// 次のページを表示
        /// </summary>
        private async void buttonNextPage_Click(object sender, EventArgs e)
        {
            try
            {
                ShowProgressUI(true, "Loading next page...");

                // 既にキャッシュがあるか確認
                if (_currentPageIndex + 1 < _pageCache.Count)
                {
                    // キャッシュから取得（再クエリなし）
                    _currentPageIndex++;
                    var cachedData = _pageCache[_currentPageIndex];

                    // 累計ドキュメント数を再計算
                    _totalFetchedDocuments = 0;
                    for (int i = 0; i <= _currentPageIndex; i++)
                    {
                        _totalFetchedDocuments += _pageCache[i].Rows.Count;
                    }

                    await UpdateGridWithCachedPageData(cachedData);
                    toolStripStatusLabel2.Text = $"Documents: {cachedData.Rows.Count}";
                    toolStripStatusLabel4.Text = $"Pages: {_currentPageIndex + 1}";
                }
                else
                {
                    // 新しいページをCosmos DBから取得
                    if (_currentPageIndex >= _pageContinuationTokens.Count)
                    {
                        MessageBox.Show("No more pages available.", "Info");
                        return;
                    }

                    var continuationToken = _pageContinuationTokens[_currentPageIndex];
                    if (string.IsNullOrEmpty(continuationToken))
                    {
                        MessageBox.Show("No more pages available.", "Info");
                        return;
                    }

                    var query = BuildQuery(GetCurrentQueryText(), GetMaxItemCount());
                    var result = await _cosmosDBService.FetchDataPageAsync(query, GetPageSize(), continuationToken);

                    // ページをキャッシュに追加
                    _pageCache.Add(result.Data.Copy());
                    _pageContinuationTokens.Add(result.ContinuationToken);
                    _currentPageIndex++;

                    // 累計ドキュメント数を更新
                    _totalFetchedDocuments += result.DocumentCount;

                    // データを表示
                    await UpdateGridWithPageData(result);

                    UpdateStatusStrip(result.TotalRequestCharge, result.DocumentCount, result.PageCount, result.ElapsedMilliseconds);
                }

                // ボタンの状態を更新
                UpdatePagingButtons();
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
        /// ページングボタンの有効/無効を更新
        /// </summary>
        private void UpdatePagingButtons()
        {
            if (!_isPagingMode)
            {
                buttonPrevPage.Enabled = false;
                buttonNextPage.Enabled = false;
                return;
            }

            buttonPrevPage.Enabled = _currentPageIndex > 0;

            // 次へボタンの有効化条件
            bool hasMorePages = _currentPageIndex < _pageContinuationTokens.Count &&
                                !string.IsNullOrEmpty(_pageContinuationTokens[_currentPageIndex]);
            bool withinMaxLimit = _totalFetchedDocuments < GetMaxItemCount();
            buttonNextPage.Enabled = hasMorePages && withinMaxLimit;

            labelPageInfo.Text = $"{_totalFetchedDocuments} / {GetMaxItemCount()}";
        }

        /// <summary>
        /// ページデータでDataGridViewを更新
        /// </summary>
        private async Task UpdateGridWithPageData(FetchDataResult result)
        {
            if (_virtualModeEnabled)
            {
                // 仮想モードの場合
                _virtualDataTable = result.Data;
                _originalDataTable = null;

                ClearModificationMarks();

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
                // 通常モードの場合
                dataGridViewResults.DataSource = null;
                SetReadOnlyColumns(result.Data);
                dataGridViewResults.DataSource = result.Data;

                ClearModificationMarks();
            }
        }

        /// <summary>
        /// キャッシュされたページデータでDataGridViewを更新
        /// </summary>
        private async Task UpdateGridWithCachedPageData(DataTable cachedData)
        {
            if (_virtualModeEnabled)
            {
                _virtualDataTable = cachedData;
                _originalDataTable = null;

                ClearModificationMarks();

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
                dataGridViewResults.DataSource = null;
                SetReadOnlyColumns(cachedData);
                dataGridViewResults.DataSource = cachedData;

                ClearModificationMarks();
            }
        }
    }
}
