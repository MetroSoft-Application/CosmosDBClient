using System.Diagnostics;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace CosmosDBClient
{
    /// <summary>
    /// ハイパーリンク処理に関連する機能を提供するクラス
    /// RichTextBoxのテキストからリンクを検出し、クリックイベントを処理する
    /// </summary>
    public class HyperlinkHandler
    {
        /// <summary>
        /// 指定されたJSONテキストからリンク情報を抽出し、RichTextBox内のテキストをハイパーリンクとしてマークする
        /// </summary>
        /// <param name="richTextBox">ハイパーリンクを処理する対象の <see cref="RichTextBox"/> コントロール</param>
        public void MarkLinkTextFromJson(RichTextBox richTextBox)
        {
            if (string.IsNullOrEmpty(richTextBox.Text))
            {
                return;
            }

            try
            {
                var jsonObject = JObject.Parse(richTextBox.Text);
                var filePath = jsonObject["fullPath"]?.ToString();
                var folderPath = jsonObject["folderName"]?.ToString();

                // リンクのスタイルを設定
                SetLinkStyle(filePath, richTextBox);
                SetLinkStyle(folderPath, richTextBox);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error parsing JSON: " + ex.Message);
            }
        }

        /// <summary>
        /// 指定されたテキストからリンク情報を抽出し、RichTextBox内のテキストをハイパーリンクとしてマークする
        /// </summary>
        /// <param name="richTextBox">ハイパーリンクを処理する対象の <see cref="RichTextBox"/> コントロール</param>
        public void MarkLinkTextFromText(RichTextBox richTextBox)
        {
            if (string.IsNullOrEmpty(richTextBox.Text))
            {
                return;
            }

            try
            {
                // リンクのスタイルを設定
                SetLinkStyle(richTextBox.Text, richTextBox);
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
        /// <param name="richTextBox">ハイパーリンクを処理する対象の <see cref="RichTextBox"/> コントロール</param>
        private void SetLinkStyle(string linkText, RichTextBox richTextBox)
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
        /// <param name="richTextBox">ハイパーリンクを処理する対象の <see cref="RichTextBox"/> コントロール</param>
        public void HandleMouseUpJson(MouseEventArgs e, RichTextBox richTextBox)
        {
            if (string.IsNullOrEmpty(richTextBox.Text)) return;

            var charIndex = richTextBox.GetCharIndexFromPosition(e.Location);
            var clickedLink = GetLinkAtPositionFromJson(richTextBox.Text, charIndex, richTextBox);

            if (!string.IsNullOrEmpty(clickedLink))
            {
                HandleLinkClick(clickedLink);
            }
        }

        /// <summary>
        /// RichTextBox内のクリックイベントを処理し、クリックされた位置にリンクが存在するかを確認する
        /// </summary>
        /// <param name="e">マウスイベントデータ</param>
        /// <param name="richTextBox">ハイパーリンクを処理する対象の <see cref="RichTextBox"/> コントロール</param>
        public void HandleMouseUpText(MouseEventArgs e, RichTextBox richTextBox)
        {
            if (string.IsNullOrEmpty(richTextBox.Text)) return;

            var charIndex = richTextBox.GetCharIndexFromPosition(e.Location);
            var clickedLink = GetLinkAtPositionFromText(richTextBox.Text, charIndex, richTextBox);

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
        /// <param name="richTextBox">ハイパーリンクを処理する対象の <see cref="RichTextBox"/> コントロール</param>
        /// <returns>リンクの文字列リンクが存在しない場合は空文字列</returns>
        private string GetLinkAtPositionFromJson(string jsonText, int charIndex, RichTextBox richTextBox)
        {
            try
            {
                var jsonObject = JObject.Parse(jsonText);
                var filePath = jsonObject["fullPath"]?.ToString();
                var folderPath = jsonObject["folderName"]?.ToString();

                if (IsLinkAtPosition(charIndex, filePath, richTextBox))
                {
                    return filePath;
                }
                else if (IsLinkAtPosition(charIndex, folderPath, richTextBox))
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
        /// 指定された位置にあるリンクを取得する
        /// </summary>
        /// <param name="jsonText">JSON形式の文字列</param>
        /// <param name="charIndex">文字のインデックス</param>
        /// <param name="richTextBox">ハイパーリンクを処理する対象の <see cref="RichTextBox"/> コントロール</param>
        /// <returns>リンクの文字列リンクが存在しない場合は空文字列</returns>
        private string GetLinkAtPositionFromText(string text, int charIndex, RichTextBox richTextBox)
        {
            try
            {
                if (IsLinkAtPosition(charIndex, text, richTextBox))
                {
                    return text;
                }
            }
            catch (Exception)
            {
                //MessageBox.Show("Error parsing JSON: " + ex.Message);
            }

            return string.Empty;
        }

        /// <summary>
        /// 指定された位置にリンクが存在するかどうかを判定する
        /// </summary>
        /// <param name="charIndex">文字のインデックス</param>
        /// <param name="linkText">リンクテキスト</param>
        /// <param name="richTextBox">ハイパーリンクを処理する対象の <see cref="RichTextBox"/> コントロール</param>
        /// <returns>リンクが存在する場合は trueそれ以外の場合は false</returns>
        private bool IsLinkAtPosition(int charIndex, string linkText, RichTextBox richTextBox)
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
