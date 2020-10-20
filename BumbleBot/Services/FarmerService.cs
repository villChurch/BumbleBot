using System;
using BumbleBot.Models;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services
{
    public class FarmerService
    {
        private DBUtils dBUtils = new DBUtils();
        public FarmerService()
        {
        }

        public Farmer ReturnFarmerInfo(ulong discordId)
        {
            try
            {
                Farmer farmer = new Farmer();
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "select * from farmers where DiscordID = ?discordID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = discordId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            //farmer.discordID = discordId;
                            farmer.credits = reader.GetInt32("credits");
                            farmer.barnspace = reader.GetInt32("barnsize");
                            farmer.grazingspace = reader.GetInt32("grazesize");
                        }
                    }
                    reader.Close();
                }
                return farmer;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                return new Farmer();
            }
        }
    }
}
