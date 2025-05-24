using CosmosDBClient.CosmosDB;
using CosmosDBClient.TableAPI;
using Microsoft.Extensions.Configuration;

namespace CosmosDBClient
{
    /// <summary>
    /// CosmosDBへの接続を管理するファクトリクラス
    /// </summary>
    public static class CosmosDBFactory
    {
        /// <summary>
        /// 現在使用しているAPIモード
        /// </summary>
        public static ApiMode CurrentApiMode { get; private set; }

        /// <summary>
        /// 設定に基づいてAPIのインスタンスを作成する
        /// </summary>
        /// <param name="configuration">設定ファイル情報</param>
        /// <returns>現在のAPIモード</returns>
        public static ApiMode InitializeFromConfig(IConfiguration configuration)
        {
            var apiMode = configuration.GetValue<string>("AppSettings:ApiMode");
            CurrentApiMode = string.Equals(apiMode, "Table", StringComparison.OrdinalIgnoreCase) ?
                ApiMode.Table : ApiMode.Sql;

            // SQL API モードの場合の接続情報を読み込む
            if (IsSqlApiMode())
            {
                var sqlApiSettings = configuration.GetSection("AppSettings:SqlApi");
                SqlConnectionString = sqlApiSettings.GetValue<string>("ConnectionString");
                SqlDatabaseName = sqlApiSettings.GetValue<string>("DatabaseName");
                SqlContainerName = sqlApiSettings.GetValue<string>("ContainerName");
            }
            // Table API モードの場合の接続情報を読み込む
            else
            {
                var tableApiSettings = configuration.GetSection("AppSettings:TableApi");
                TableConnectionString = tableApiSettings.GetValue<string>("ConnectionString");
                TableName = tableApiSettings.GetValue<string>("TableName");
            }

            return CurrentApiMode;
        }

        /// <summary>
        /// SQL API用の接続文字列
        /// </summary>
        public static string SqlConnectionString { get; private set; }

        /// <summary>
        /// SQL API用のデータベース名
        /// </summary>
        public static string SqlDatabaseName { get; private set; }

        /// <summary>
        /// SQL API用のコンテナ名
        /// </summary>
        public static string SqlContainerName { get; private set; }

        /// <summary>
        /// Table API用の接続文字列
        /// </summary>
        public static string TableConnectionString { get; private set; }

        /// <summary>
        /// Table API用のテーブル名
        /// </summary>
        public static string TableName { get; private set; }

        /// <summary>
        /// SQL API用サービスを作成する
        /// </summary>
        /// <param name="connectionString">接続文字列</param>
        /// <param name="databaseName">データベース名</param>
        /// <param name="containerName">コンテナ名</param>
        /// <returns>SQL API用サービスインスタンス</returns>
        public static CosmosDBService CreateSqlApiService(string connectionString, string databaseName, string containerName)
        {
            return new CosmosDBService(connectionString, databaseName, containerName);
        }

        /// <summary>
        /// Table API用サービスを作成する
        /// </summary>
        /// <param name="connectionString">接続文字列</param>
        /// <param name="tableName">テーブル名</param>
        /// <returns>Table API用サービスインスタンス</returns>
        public static TableAPIService CreateTableApiService(string connectionString, string tableName)
        {
            return new TableAPIService(connectionString, tableName);
        }

        /// <summary>
        /// SQL APIモードかどうかを確認する
        /// </summary>
        /// <returns>SQL APIモードの場合はtrue、それ以外はfalse</returns>
        public static bool IsSqlApiMode()
        {
            return CurrentApiMode == ApiMode.Sql;
        }

        /// <summary>
        /// Table APIモードかどうかを確認する
        /// </summary>
        /// <returns>Table APIモードの場合はtrue、それ以外はfalse</returns>
        public static bool IsTableApiMode()
        {
            return CurrentApiMode == ApiMode.Table;
        }
    }
}
