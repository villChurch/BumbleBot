using System;
using System.IO;
using System.Text;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace BumbleBot.Utilities
{
    public class DBUtils
    {
        private readonly string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public string ReturnPopulatedConnectionStringAsync()
        {
            var json = string.Empty;

            using (var fs =
                File.OpenRead(configFilePath + "/config.json")
            )
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
            {
                json = sr.ReadToEnd();
            }

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);


            var mcsb = new MySqlConnectionStringBuilder
            {
                Database = configJson.databaseName,
                Password = configJson.databasePassword,
                UserID = configJson.databaseUser,
                Port = configJson.databasePort,
                Server = configJson.databaseServer,
                MaximumPoolSize = 300
            };

            return mcsb.ToString();
        }

        public static string ReturnPopulatedConnectionStringStatic()
        {
            var json = string.Empty;

            using (var fs =
                File.OpenRead(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/config.json")
            )
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
            {
                json = sr.ReadToEnd();
            }

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);


            var mcsb = new MySqlConnectionStringBuilder
            {
                Database = configJson.databaseName,
                Password = configJson.databasePassword,
                UserID = configJson.databaseUser,
                Port = configJson.databasePort,
                Server = configJson.databaseServer,
                MaximumPoolSize = 300
            };

            return mcsb.ToString();
        }
    }
}