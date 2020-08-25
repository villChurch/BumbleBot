using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace BumbleBot.Utilities
{
    public class DBUtils
    {
        private string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public async Task<MySqlConnection> GetDbConnectionAsync()
        {
            DBConnection dbCon = DBConnection.Instance();
            var json = string.Empty;

            using (var fs =
                File.OpenRead(configFilePath + "/config.json")
            )
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);
            dbCon.DatabaseName = configJson.databaseName;
            dbCon.Password = configJson.databasePassword;
            dbCon.databaseUser = configJson.databaseUser;
            dbCon.databasePort = configJson.databasePort;
            return new MySqlConnection(dbCon.connectionString);
        }
    }

    public sealed class DBConnection
    {
        private string databaseName = string.Empty;
        public string DatabaseName
        {
            get { return databaseName; }
            set { databaseName = value; }
        }

        public string databaseUser { get; set; }
        public string Password { get; set; }

        private static DBConnection _instance = null;
        public static DBConnection Instance()
        {
            if (_instance == null)
                _instance = new DBConnection();
            return _instance;
        }

        public string databasePort { get; set; }
        public string connectionString
        {
            get { return $"Server=williamspires.co.uk; Port={databasePort}; database={databaseName}; UID={databaseUser}; password={Password}"; }
        }
    }
}
