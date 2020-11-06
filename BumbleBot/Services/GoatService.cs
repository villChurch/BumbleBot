﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services
{
    public class GoatService
    {

        private DBUtils dBUtils = new DBUtils();
        public GoatService()
        {
        }

        public bool CheckExpAgainstNextLevel(ulong userId, decimal expToAdd)
        {
            //n =  ln(FV / PV)ln(1 + r)
            // exp = 10 * 1.05^level
            (int, decimal) goatsLevelAndExp = GetCurrentLevelAndExpOfGoat(userId);
            int startingLevel = goatsLevelAndExp.Item1;
            if (goatsLevelAndExp.Item1 != 0)
            {
                goatsLevelAndExp.Item1 = (int)Math.Floor((Math.Log((double)(goatsLevelAndExp.Item2 / 10)) / Math.Log(1.05)));
                decimal newExp = goatsLevelAndExp.Item2 + expToAdd;
                UpdateGoatLevelAndExperience(goatsLevelAndExp.Item1, newExp, userId);
            }
            return startingLevel != goatsLevelAndExp.Item1;
        }

        private (int, decimal) GetCurrentLevelAndExpOfGoat(ulong userId)
        {
            int goatsLevel = 0;
            decimal goatsExperience = 0;
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "Select level, experience from goats Where ownerID = ?ownerId and equipped = 1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar, 40).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        goatsLevel = reader.GetInt32("level");
                        goatsExperience = reader.GetDecimal("experience");
                    }
                }
                reader.Close();
            }
            return (goatsLevel, goatsExperience);
        }

        private void UpdateGoatLevelAndExperience(int level, decimal experience, ulong userId)
        {
            if (level >= 100)
            {
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Update goats Set level = ?level, experience = ?experience, type = ?type " +
                        "where ownerID = ?ownerId and equipped = 1";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?level", MySqlDbType.Int32).Value = level;
                    command.Parameters.Add("?experience", MySqlDbType.Decimal).Value = experience;
                    command.Parameters.Add("?ownerId", MySqlDbType.VarChar, 40).Value = userId;
                    command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Adult";
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Update goats Set level = ?level, experience = ?experience where ownerID = ?ownerId and equipped = 1";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?level", MySqlDbType.Int32).Value = level;
                    command.Parameters.Add("?experience", MySqlDbType.Decimal).Value = experience;
                    command.Parameters.Add("?ownerId", MySqlDbType.VarChar, 40).Value = userId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool CanGoatFitInBarn(ulong discordId)
        {
            int barnSize = 10;
            int numberOfGoats = 0;
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "Select barnsize from farmers where DiscordID = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        barnSize = reader.GetInt32("barnsize");
                    }
                }
                reader.Close();
            }
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "SELECT COUNT(*) as numberOfGoats FROM goats WHERE ownerID = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        numberOfGoats = reader.GetInt32("numberOfGoats");
                    }
                }
                reader.Close();
            }
            return barnSize != numberOfGoats;
        }

        public List<Goat> ReturnUsersGoats(ulong userId)
        {
            List<Goat> goats = new List<Goat>();
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select * from goats where ownerID = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while(reader.Read())
                    {
                        Goat goat = new Goat();
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goats.Add(goat);
                    }
                }
            }
            return goats;
        }
    }
}
