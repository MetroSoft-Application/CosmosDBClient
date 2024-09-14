using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace CosmosDBClient
{
    /// <summary>
    /// ハイパーリンク処理に関連する機能を提供するクラス
    /// RichTextBoxのテキストからリンクを検出し、クリックイベントを処理する
    /// </summary>
    public class HyperlinkHandler
    {
        private readonly RichTextBox richTextBox;

        /// <summary>
        /// 指定された <see cref="RichTextBox"/> コントロールを使用して、新しいインスタンスを初期化する
        /// </summary>
        /// <param name="richTextBox">ハイパーリンクを処理するための <see cref="RichTextBox"/> コントロール</param>
        public HyperlinkHandler(RichTextBox richTextBox)
        {
            this.richTextBox = richTextBox;
        }

        /// <summary>
        /// 指定されたJSONテキストからリンク情報を抽出し、RichTextBox内のテキストをハイパーリンクとしてマークする
        /// </summary>
        /// <param name="jsonText">リンク情報を含むJSON形式の文字列</param>
        public void MarkLinkTextFromJson(string jsonText)
        {
            try
            {
                var jsonObject = JObject.Parse(jsonText);

                var filePath = jsonObject["fullPath"]?.ToString();
                var folderPath = jsonObject["folderName"]?.ToString();

                SetLinkStyle(filePath);
                SetLinkStyle(folderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error parsing JSON: " + ex.Message);
            }
        }

        /// <summary>
        /// 指定されたリンクテキストをRichTextBox内でハイパーリンクとして設定する
        /// </summary>
        /// <param name="linkText">ハイパーリンクとして表示するリンクテキスト</param>
        private void SetLinkStyle(string linkText)
        {
            if (!string.IsNullOrEmpty(linkText))
            {
                int startIndex = richTextBox.Text.IndexOf(linkText);
                if (startIndex >= 0)
                {
                    richTextBox.Select(startIndex, linkText.Length);
                    richTextBox.SelectionColor = Color.Blue;
                    richTextBox.SelectionFont = new Font(richTextBox.Font, FontStyle.Underline);
                    richTextBox.Select(0, 0);
                }
            }
        }

        /// <summary>
        /// RichTextBox内のクリックイベントを処理し、クリックされた位置にリンクが存在するかを確認する
        /// </summary>
        /// <param name="e">マウスイベントデータ</param>
        public void HandleMouseUp(MouseEventArgs e)
        {
            if (string.IsNullOrEmpty(richTextBox.Text)) return;

            int charIndex = richTextBox.GetCharIndexFromPosition(e.Location);
            string clickedLink = GetLinkAtPosition(richTextBox.Text, charIndex);

            if (!string.IsNullOrEmpty(clickedLink))
            {
                HandleLinkClick(clickedLink);
            }
        }

        /// <summary>
        /// 指定された位置にあるリンクを取得する
        /// </summary>
        /// <param name="jsonText">JSON形式の文字列</param>
        /// <param name="charIndex">文字のインデックス</param>
        /// <returns>リンクの文字列リンクが存在しない場合は空文字列</returns>
        private string GetLinkAtPosition(string jsonText, int charIndex)
        {
            try
            {
                var jsonObject = JObject.Parse(jsonText);
                var filePath = jsonObject["fullPath"]?.ToString();
                var folderPath = jsonObject["folderName"]?.ToString();

                if (IsLinkAtPosition(charIndex, filePath))
                {
                    return filePath;
                }
                else if (IsLinkAtPosition(charIndex, folderPath))
                {
                    return folderPath;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error parsing JSON: " + ex.Message);
            }

            return string.Empty;
        }

        /// <summary>
        /// 指定された位置にリンクが存在するかどうかを判定する
        /// </summary>
        /// <param name="charIndex">文字のインデックス</param>
        /// <param name="linkText">リンクテキスト</param>
        /// <returns>リンクが存在する場合は trueそれ以外の場合は false</returns>
        private bool IsLinkAtPosition(int charIndex, string linkText)
        {
            if (!string.IsNullOrEmpty(linkText))
            {
                var startIndex = richTextBox.Text.Replace(@"\\", @"\").IndexOf(linkText.Substring(2));
                return charIndex >= startIndex && charIndex < startIndex + linkText.Length;
            }
            return false;
        }

        /// <summary>
        /// 指定されたリンクをクリックした際の動作を処理する
        /// </summary>
        /// <param name="link">クリックされたリンクのパス</param>
        private void HandleLinkClick(string link)
        {
            if (System.IO.File.Exists(link))
            {
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            }
            else if (System.IO.Directory.Exists(link))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", link) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("Path does not exist: " + link);
            }
        }
    }
}
