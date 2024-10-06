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
        private Container _cosmosContainer;
        private readonly string[] systemColumns = { "id", "_etag", "_rid", "_self", "_attachments", "_ts" };

        public FormInsert(Container cosmosContainer, string json)
        {
            InitializeComponent();
            _cosmosContainer = cosmosContainer;
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
                if (!systemColumns.Contains(property.Name))
                {
                    filteredObject.Add(property.Name, property.Value);
                }

                filteredObject["id"] = Guid.NewGuid().ToString("N");
            }

            richTextBoxInsertJson.Text = filteredObject.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                // PartitionKeyを自動的に解決して取得
                var partitionKey = await ResolvePartitionKeyAsync(jsonObject);

                // PartitionKeyに対応するキー項目を取得
                var partitionKeyInfo = GetPartitionKeyValues(jsonObject);

                // Cosmos DBにUpsert処理を実行
                var response = await _cosmosContainer.UpsertItemAsync(jsonObject, partitionKey);

                // 成功メッセージを表示
                var id = jsonObject["id"].ToString();
                var message = $"Upsert successful!\n\nId: {id}\nPartitionKey:\n{partitionKeyInfo}\n\nRequest charge: {response.RequestCharge}";
                MessageBox.Show(message);
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
            var containerProperties = await _cosmosContainer.ReadContainerAsync();
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
            var containerProperties = _cosmosContainer.ReadContainerAsync().Result;
            var partitionKeyPaths = containerProperties.Resource.PartitionKeyPaths;

            // フィールド名と値を改行で結合して表示
            return string.Join("\n", partitionKeyPaths.Select(path =>
            {
                var key = jsonObject.SelectToken(path.Trim('/'))?.ToString();
                return $"{path.Trim('/')}: {key}";
            }));
        }
    }
}
