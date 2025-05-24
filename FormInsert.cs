using CosmosDBClient.CosmosDB;
using CosmosDBClient.TableAPI;
using FastColoredTextBoxNS;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json.Linq;
using System.Data;

namespace CosmosDBClient
{
    /// <summary>
    /// Cosmos DB クライアントのデータを表示および管理するためのフォーム
    /// </summary>
    public partial class FormInsert : Form
    {
        private CosmosDBService _cosmosDBService;
        private TableAPIService _tableAPIService;
        private FastColoredTextBox _jsonData;
        private ApiMode _apiMode;
        private DataRow _tableRow;

        /// <summary>
        /// 新しい <see cref="FormInsert"/> クラスのインスタンスを初期化する（SQL APIモード用）
        /// </summary>
        /// <param name="cosmosDBService">CosmosDBService</param>
        /// <param name="json">jsonテキスト</param>
        public FormInsert(CosmosDBService cosmosDBService, string json)
        {
            InitializeComponent();
            _cosmosDBService = cosmosDBService;
            _apiMode = ApiMode.Sql;
            var jsonObject = default(JObject);

            _jsonData = new FastColoredTextBox();
            _jsonData.Language = Language.JSON;
            _jsonData.Dock = DockStyle.Fill;
            _jsonData.ImeMode = ImeMode.Hiragana;
            _jsonData.BorderStyle = BorderStyle.Fixed3D;
            _jsonData.BackColor = SystemColors.Window;
            _jsonData.Font = new Font("Yu Gothic UI", 9);
            _jsonData.WordWrap = true;
            _jsonData.ShowLineNumbers = false;
            panel1.Controls.Add(_jsonData);

            try
            {
                // JSONデータをパース
                jsonObject = JObject.Parse(json);
            }
            catch (Exception)
            {
            }            // 新規作成用のJSONオブジェクトを準備
            var filteredObject = new JObject();

            // 既存のJSONオブジェクトが有効な場合はIDを保持、それ以外は新規生成
            filteredObject["id"] = Guid.NewGuid().ToString("N");

            try
            {
                foreach (var property in jsonObject.Properties())
                {
                    // システム項目に該当しない場合のみ追加
                    if (!_cosmosDBService.systemColumns.Contains(property.Name))
                    {
                        filteredObject.Add(property.Name, null);
                    }
                }
            }
            catch (Exception)
            {
            }

            _jsonData.Text = filteredObject.ToString();
        }

        /// <summary>
        /// 新しい <see cref="FormInsert"/> クラスのインスタンスを初期化する（Table APIモード用）
        /// </summary>
        /// <param name="tableAPIService">TableAPIService</param>
        /// <param name="tableRow">編集対象のTableAPIエンティティ（DataRow形式）、新規作成時はnull</param>
        public FormInsert(TableAPIService tableAPIService, DataRow tableRow = null)
        {
            InitializeComponent();
            _tableAPIService = tableAPIService;
            _tableRow = tableRow;
            _apiMode = ApiMode.Table;

            _jsonData = new FastColoredTextBox();
            _jsonData.Language = Language.JSON;
            _jsonData.Dock = DockStyle.Fill;
            _jsonData.ImeMode = ImeMode.Hiragana;
            _jsonData.BorderStyle = BorderStyle.Fixed3D;
            _jsonData.BackColor = SystemColors.Window;
            _jsonData.Font = new Font("Yu Gothic UI", 9);
            _jsonData.WordWrap = true;
            _jsonData.ShowLineNumbers = false;
            panel1.Controls.Add(_jsonData);            // フォームのタイトルを変更
            this.Text = "Table API Entity Insert/Update";

            if (tableRow != null)
            {
                // 既存エンティティの編集
                var jsonEntity = new JObject();
                foreach (DataColumn column in tableRow.Table.Columns)
                {
                    string columnName = column.ColumnName;
                    if (!_tableAPIService.systemColumns.Contains(columnName))
                    {
                        var value = tableRow[columnName];
                        jsonEntity[columnName] = value is DBNull ? null : JToken.FromObject(value);
                    }
                }
                // システム列（PartitionKeyとRowKey）は別途追加
                jsonEntity["PartitionKey"] = tableRow["PartitionKey"].ToString();
                jsonEntity["RowKey"] = tableRow["RowKey"].ToString();

                _jsonData.Text = jsonEntity.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            else
            {            // 新規エンティティの作成
                var jsonEntity = new JObject
                {
                    ["PartitionKey"] = "",  // 必須フィールド
                    ["RowKey"] = "",        // 必須フィールド
                    ["SampleProperty"] = "Enter value"
                };
                _jsonData.Text = jsonEntity.ToString(Newtonsoft.Json.Formatting.Indented);
            }
        }

        /// <summary>
        /// レコードを挿入する
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private async void buttonJsonInsert_Click(object sender, EventArgs e)
        {            DialogResult result = MessageBox.Show(
                "Do you want to insert/update the record?",
                "Confirmation",
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
                MessageBox.Show($"JSON parse error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (_apiMode == ApiMode.Sql)
                {
                    // SQL APIモード
                    await InsertSqlApiDocument(jsonObject);
                }
                else
                {
                    // Table APIモード
                    await InsertTableApiEntity(jsonObject);
                }

                // 挿入成功したらダイアログを閉じる
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// SQL APIにドキュメントを挿入する
        /// </summary>
        /// <param name="jsonObject">挿入するJSONオブジェクト</param>
        private async Task InsertSqlApiDocument(JObject jsonObject)
        {
            // JSONオブジェクトからidを取得
            var id = jsonObject["id"]?.ToString();
            if (string.IsNullOrEmpty(id))
            {
                throw new Exception("ID field is missing or invalid.");
            }

            // PartitionKeyを自動的に解決して取得
            var partitionKey = await _cosmosDBService.ResolvePartitionKeyAsync(jsonObject);

            // PartitionKeyに対応するキー項目を取得
            var partitionKeyInfo = _cosmosDBService.GetPartitionKeyValues(jsonObject);

            // Cosmos DBにUpsert処理を実行
            var response = await _cosmosDBService.UpsertItemAsync(jsonObject, partitionKey);            var message = $"Successfully inserted/updated!\n\nId: {id}\nPartition Key: {partitionKeyInfo}\n\nRequest Charge: {response.RequestCharge}";
            MessageBox.Show(message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Table APIにエンティティを挿入する
        /// </summary>
        /// <param name="jsonObject">挿入するJSONオブジェクト</param>
        private async Task InsertTableApiEntity(JObject jsonObject)
        {
            // PartitionKeyとRowKeyの検証
            var partitionKey = jsonObject["PartitionKey"]?.ToString();
            var rowKey = jsonObject["RowKey"]?.ToString();

            if (string.IsNullOrEmpty(partitionKey))
            {
                throw new Exception("PartitionKey field is missing or invalid.");
            }

            if (string.IsNullOrEmpty(rowKey))
            {
                throw new Exception("RowKey field is missing or invalid.");
            }

            // DynamicTableEntityの作成
            var entity = new DynamicTableEntity(partitionKey, rowKey);

            // プロパティを追加
            foreach (var property in jsonObject.Properties())
            {
                if (property.Name != "PartitionKey" && property.Name != "RowKey")
                {
                    entity.Properties[property.Name] = CreateEntityProperty(property.Value);
                }
            }

            // Tableにエンティティを挿入
            await _tableAPIService.InsertOrReplaceEntityAsync(entity);            var message = $"Entity successfully inserted/updated!\n\nPartition Key: {partitionKey}\nRow Key: {rowKey}";
            MessageBox.Show(message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// JTokenからEntityPropertyを作成する
        /// </summary>
        /// <param name="token">変換元のJToken</param>
        /// <returns>EntityProperty</returns>
        private EntityProperty CreateEntityProperty(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Null:
                    return EntityProperty.GeneratePropertyForString(null);
                case JTokenType.String:
                    return EntityProperty.GeneratePropertyForString(token.ToString());
                case JTokenType.Boolean:
                    return EntityProperty.GeneratePropertyForBool(token.Value<bool>());
                case JTokenType.Integer:
                    return EntityProperty.GeneratePropertyForInt(token.Value<int>());
                case JTokenType.Float:
                    return EntityProperty.GeneratePropertyForDouble(token.Value<double>());
                case JTokenType.Date:
                    return EntityProperty.GeneratePropertyForDateTimeOffset(token.Value<DateTimeOffset>());
                default:
                    // その他の型は文字列に変換
                    return EntityProperty.GeneratePropertyForString(token.ToString());
            }
        }
    }
}
