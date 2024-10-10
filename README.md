# Cosmos DB クライアントツール

このツールは、Cosmos DB インスタンスと対話するための Windows フォームアプリケーションです。以下の機能を提供しています：

- Cosmos DB コンテナデータを `DataGridView` に表示。
- Cosmos DB 内のデータの挿入、更新、削除。
- 指定された Cosmos DB データベースからコンテナ名を `ComboBox` に表示。
- カスタマイズ可能な項目数制限でクエリを実行。
- リクエストチャージや実行時間の表示。

## 機能

1. **Cosmos DB 接続**：
   - Cosmos DB の接続文字列、データベース名、コンテナ名を指定して操作を行います。
   - 指定された設定に基づいて、Cosmos DB からコンテナデータを取得し表示します。

2. **データ操作**：
   - 以下の操作を実行できます：
     - 新しいレコードの挿入。
     - 既存のレコードの更新。
     - レコードの削除。
   - 各操作では、リクエストチャージや実行時間が表示されます。

3. **クエリの実行**：
   - カスタムクエリを指定するか、デフォルトのクエリを使用してデータを取得できます。
   - 取得する最大項目数は調整可能です。

4. **コンテナ一覧の表示**：
   - `appsettings.json`に接続文字列とデータベース名が入力されている場合は指定されたデータベースから自動的にコンテナ名を `ComboBox` に読み込みます。

5. **ステータス情報**：
   - 操作実行後、リクエストチャージ、ドキュメント数、ページ数、経過時間がステータスバーに表示されます。

## 必要条件

- .NET 8
- Azure Cosmos DB アカウント
- Cosmos DB 接続文字列、データベース名、コンテナ名
- Windows 環境

## 設定について

1. `appsettings.json` ファイル内に Cosmos DB の接続情報を更新します：

   ```json
   {
       "AppSettings": {
           "ConnectionString": "<Your Cosmos DB Connection String>",
           "DatabaseName": "<Your Database Name>",
           "ContainerName": "<Your Container Name>",
           "MaxItemCount": 100,
           "EnableHyperlinkHandler": false
       }
   }