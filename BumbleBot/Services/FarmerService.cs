using System;
using System.Collections.Generic;
using System.Linq;
using BumbleBot.Models;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services
{
    public class FarmerService
    {
        private readonly DbUtils dBUtils = new DbUtils();

        public void AddAlfalfaToFarmer(ulong userId)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "insert into items (name, amount, ownerId) values (?item, 1, ?ownerId) on duplicate key update amount = 1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?ownerId", userId);
                command.Parameters.AddWithValue("?item", "alfalfa");
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void AddOatsToFarmer(ulong userId)
        {
            var farmer = ReturnFarmerInfo(userId);
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "update farmers set oats = 1, credits = ?credits where DiscordID = ?discordID";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordID", MySqlDbType.VarChar).Value = userId;
                command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.Credits - 250;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        public bool DoesFarmerHaveAlfalfa(ulong userId)
        {
            var hasAlfalfa = false;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select amount from items where ownerId = ?ownerId and name = ?item";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?ownerId", userId);
                command.Parameters.AddWithValue("?item", "alfalfa");
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        hasAlfalfa = reader.GetInt16("amount") > 0;
                    }
                }
                reader.Close();
            }

            return hasAlfalfa;
        }
        
        public bool DoesFarmerHaveOats(ulong userId)
        {
            var hasOats = false;
            using (var conncetion = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select oats from farmers where DiscordID = ?discordID";
                var command = new MySqlCommand(query, conncetion);
                command.Parameters.Add("?discordID", MySqlDbType.VarChar).Value = userId;
                conncetion.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        hasOats = reader.GetBoolean("oats");
                reader.Close();
            }

            return hasOats;
        }

        public bool DoesFarmerHaveOatsOrAlfalfa(ulong userId)
        {
            return DoesFarmerHaveAlfalfa(userId) || DoesFarmerHaveOats(userId);
        }
        public bool DoesFarmerHaveKidsInKiddingPen(ulong discordId)
        {
            var hasKids = false;

            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from newbornkids where ownerID = ?ownerId";
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
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "update kiddingpens set capacity = ?capacity where ownerID = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?capacity", MySqlDbType.Int32).Value = currentCapcity + increaseBy;
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public int GetKiddingPenCapacity(ulong discordId)
        {
            var capacity = 1;

            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from kiddingpens where ownerID = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        capacity = reader.GetInt32("capacity");
            }

            return capacity;
        }

        public bool DoesFarmerHaveDairy(ulong discordId)
        {
            var hasDairy = false;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from dairy where ownerID = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                hasDairy = reader.HasRows;
                reader.Close();
            }

            return hasDairy;
        }

        public bool DoesFarmerHaveAKiddingPen(ulong discordId)
        {
            var haspen = false;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select count(*) as pens from kiddingpens where ownerId = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                var count = 0;
                while (reader.Read()) count = reader.GetInt32("pens");
                haspen = count > 0;
            }

            return haspen;
        }

        public bool DoesFarmerHaveAdultsInKiddingPen(List<Goat> usersGoats)
        {
            var hasAdultsInPen = false;
            var goatIds = new List<int>();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select goatId from cookingdoes where ready = 0";
                var command = new MySqlCommand(query, connection);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        goatIds.Add(reader.GetInt32("goatId"));
            }

            var ids = usersGoats.Select(goat => goat.Id).ToList();
            return ids.Any(x => goatIds.Contains(x));
        }

        public Farmer ReturnFarmerInfo(ulong discordId)
        {
            try
            {
                var farmer = new Farmer();
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "select * from farmers where DiscordID = ?discordID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = discordId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                        {
                            //farmer.discordID = discordId;
                            farmer.Credits = reader.GetInt32("credits");
                            farmer.Barnspace = reader.GetInt32("barnsize");
                            farmer.Grazingspace = reader.GetInt32("grazesize");
                            farmer.Milk = reader.GetDecimal("milk");
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
                var farmerCredits = 0;
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "select credits from farmers where DiscordID = ?farmerId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?farmerId", MySqlDbType.VarChar).Value = farmerId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                            farmerCredits = reader.GetInt32("credits");
                    reader.Close();
                }

                farmerCredits -= credits;
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Update farmers Set credits = ?credits where DiscordID = ?farmerId";
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
                var farmerCredits = 0;
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "select credits from farmers where DiscordID = ?farmerId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?farmerId", MySqlDbType.VarChar).Value = farmerId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                            farmerCredits = reader.GetInt32("credits");
                    reader.Close();
                }

                farmerCredits += credits;
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Update farmers Set credits = ?credits where DiscordID = ?farmerId";
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