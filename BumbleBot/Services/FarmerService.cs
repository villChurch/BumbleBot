using System;
using System.Collections.Generic;
using BumbleBot.Models;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;
using System.Linq;

namespace BumbleBot.Services
{
    public class FarmerService
    {
        private DBUtils dBUtils = new DBUtils();
        public FarmerService()
        {
        }

        public Boolean DoesFarmerHaveKidsInKiddingPen(ulong discordId)
        {
            bool hasKids = false;

            using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select * from newbornkids where ownerID = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                hasKids = reader.HasRows;
                reader.Close();
            }
            return hasKids;
        }

        public void IncreaseKiddingPenCapacity(ulong discordId, int currentCapcity, int increaseBy)
        {
            using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "update kiddingpens set capacity = ?capacity where ownerID = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?capacity", MySqlDbType.Int32).Value = currentCapcity + increaseBy;
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public int GetKiddingPenCapacity(ulong discordId)
        {
            int capacity = 1;

            using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select * from kiddingpens where ownerID = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        capacity = reader.GetInt32("capacity");
                    }
                }
            }

            return capacity;
        }

        public Boolean DoesFarmerHaveDairy(ulong discordId)
        {
            bool hasDairy = false;
            using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select * from dairy where ownerID = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                hasDairy = reader.HasRows;
                reader.Close();
            }
            return hasDairy;
        }

        public Boolean DoesFarmerHaveAKiddingPen(ulong discordId)
        {
            bool haspen = false;
            using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select count(*) as pens from kiddingpens where ownerId = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                int count = 0;
                while (reader.Read())
                {
                    count = reader.GetInt32("pens");
                }
                haspen = count > 0;
            }
            return haspen;
        }

        public Boolean DoesFarmerHaveAdultsInKiddingPen(List<Goat> usersGoats)
        {
            bool hasAdultsInPen = false;
            List<int> goatIds = new List<int>();
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select goatId from cookingdoes where ready = 0";
                var command = new MySqlCommand(query, connection);
                connection.Open();
                var reader = command.ExecuteReader();
                if(reader.HasRows)
                {
                    while (reader.Read())
                    {
                        goatIds.Add(reader.GetInt32("goatId"));
                    }
                }
            }
            List<int> ids = usersGoats.Select(goat => goat.id).ToList();
            return ids.Any(x => goatIds.Contains(x));
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
