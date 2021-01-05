using System;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services
{
    public class AssholeService
    {
        private int commandsRunBeforeParamChange;
        private readonly DBUtils dBUtils = new DBUtils();

        public bool SetAhConfig()
        {
            commandsRunBeforeParamChange += 1;
            if (commandsRunBeforeParamChange > 5)
            {
                var rnd = new Random();
                if (rnd.Next(0, 10) == 5)
                {
                    commandsRunBeforeParamChange = 0;
                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Update config SET boolValue = ?boolValue where paramName = ?paramName";
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