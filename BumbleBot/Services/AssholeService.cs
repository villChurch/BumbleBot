using System;
using System.Threading.Tasks;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services
{
    public class AssholeService
    {
        private int commandsRunBeforeParamChange;
        private DBUtils dBUtils = new DBUtils();

        public bool SetAhConfig()
        {
            commandsRunBeforeParamChange += 1;
            if (commandsRunBeforeParamChange > 5)
            {
                Random rnd = new Random();
                if (rnd.Next(0, 10) == 5)
                {
                    commandsRunBeforeParamChange = 0;
                    using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "Update config SET boolValue = ?boolValue where paramName = ?paramName";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?boolValue", MySqlDbType.Int16).Value = 1;
                        command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeMode";
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            return false;
        }
    }
}