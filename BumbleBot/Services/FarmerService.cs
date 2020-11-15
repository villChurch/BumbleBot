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

        public void DeductCreditsFromFarmer(ulong farmerId, int credits)
        {
            try
            {
                int farmerCredits = 0;
                using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "select credits from farmers where DiscordID = ?farmerId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?farmerId", MySqlDbType.VarChar).Value = farmerId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while(reader.Read())
                        {
                            farmerCredits = reader.GetInt32("credits");
                        }
                    }
                    reader.Close();
                }
                farmerCredits -= credits;
                using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Update farmers Set credits = ?credits where DiscordID = ?farmerId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmerCredits;
                    command.Parameters.Add("?farmerId", MySqlDbType.VarChar).Value = farmerId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        public void AddCreditsToFarmer(ulong farmerId, int credits)
        {
            try
            {
                int farmerCredits = 0;
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "select credits from farmers where DiscordID = ?farmerId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?farmerId", MySqlDbType.VarChar).Value = farmerId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            farmerCredits = reader.GetInt32("credits");
                        }
                    }
                    reader.Close();
                }
                farmerCredits += credits;
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Update farmers Set credits = ?credits where DiscordID = ?farmerId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmerCredits;
                    command.Parameters.Add("?farmerId", MySqlDbType.VarChar).Value = farmerId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}
