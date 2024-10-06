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
    public partial class FormInsert : Form
    {
        private CosmosDBService _cosmosDBService;

        /// <summary>
        /// 新しい <see cref="FormInsert"/> クラスのインスタンスを初期化する
        /// </summary>
        /// <param name="cosmosDBService">CosmosDBService</param>
        /// <param name="json">jsonテキスト</param>
        public FormInsert(CosmosDBService cosmosDBService, string json)
        {
            InitializeComponent();
            _cosmosDBService = cosmosDBService;
            var jsonObject = default(JObject);

            try
            {
                // JSONデータをパース
                jsonObject = JObject.Parse(json);
            }
            catch (Exception)
            {
                return;
            }

            // システムフィールドを除外する
            var filteredObject = new JObject();
            foreach (var property in jsonObject.Properties())
            {
                // システム項目に該当しない場合のみ追加
                if (!_cosmosDBService.systemColumns.Contains(property.Name))
                {
                    filteredObject.Add(property.Name, null);
                }

                filteredObject["id"] = Guid.NewGuid().ToString("N");
            }

            richTextBoxInsertJson.Text = filteredObject.ToString();
        }

        /// <summary>
        /// レコードを挿入する
        /// </summary>
        /// <param name="sender">イベントの送信元オブジェクト</param>
        /// <param name="e">イベントデータ</param>
        private async void buttonJsonInsert_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Do you want to Insert your records?",
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
                jsonObject = JObject.Parse(richTextBoxInsertJson.Text);
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
                var partitionKeyInfo = _cosmosDBService.GetPartitionKeyValues(jsonObject);

                // Cosmos DBにUpsert処理を実行
                var response = await _cosmosDBService.UpsertItemAsync(jsonObject, partitionKey);

                var message = $"Upsert successful!\n\nId:{id}\nPartitionKey:\n{partitionKeyInfo}\n\nRequest charge:{response.RequestCharge}";
                MessageBox.Show(message, "Info");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }
    }
}
