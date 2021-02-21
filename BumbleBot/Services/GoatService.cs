﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;
using Type = BumbleBot.Models.Type;

namespace BumbleBot.Services
{
    public class GoatService
    {
        private readonly DbUtils dBUtils = new DbUtils();
        private readonly string deathPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        public int GetNumberOfShowContestants()
        {
            var numberOfContestants = 0;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select count(*) as contestants from showGoats";
                var command = new MySqlCommand(query, connection);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        numberOfContestants = reader.GetInt32("contestants");
                    }
                }
            }
            return numberOfContestants;
        }
        public void ResetContest()
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "delete from showGoats";
                var command = new MySqlCommand(query, connection);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public Dictionary<int, int> GetContestsVotes()
        {
            var dictionary = new Dictionary<int, int>();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select * from showGoats";
                var command = new MySqlCommand(query, connection);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        dictionary.Add(reader.GetInt32("id"), reader.GetInt32("votes"));
                    }
                }
            }

            return dictionary;
        }

        public (int, ulong) GetShowWinner()
        {
            var goatId = 0;
            ulong ownerId = 0;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "SELECT * FROM showGoats order BY votes DESC LIMIT 1";
                var command = new MySqlCommand(query, connection);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        goatId = reader.GetInt32("goatId");
                        ownerId = reader.GetUInt64("ownerId");
                    }
                }
                reader.Close();
            }

            return (goatId, ownerId);
        }
        public void IncreaseContestantsVote(int contestantId, int votesToAdd)
        {
            int votes = 0;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select votes from showGoats where id = ?contestantId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?contestantId", MySqlDbType.Int32).Value = contestantId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        votes = reader.GetInt32("votes");
                    }
                }
            }
            votes += votesToAdd;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "Update showGoats Set votes = ?votes where id = ?contestantId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?votes", MySqlDbType.Int32).Value = votes;
                command.Parameters.Add("?contestantId", MySqlDbType.Int32).Value = contestantId;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        public bool DoesShowGoatExistByContestantId(int contestantId)
        {
            bool exists;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select * from showGoats where id = ?contestantId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?contestantId", MySqlDbType.Int32).Value = contestantId;
                connection.Open();
                var reader = command.ExecuteReader();
                exists = reader.HasRows;
                reader.Close();
            }
            return exists;
        }
        public List<Goat> GetAllGoats()
        {
            var goats = new List<Goat>();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats";
                var command = new MySqlCommand(query, connection);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        var goat = new Goat();
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("id");
                        goat.Level = reader.GetInt32("level");
                        goat.LevelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.Experience = reader.GetDecimal("experience");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                        goat.Type = (Type) Enum.Parse(typeof(Type), reader.GetString("type"));
                        goats.Add(goat);
                    }
            }

            return goats;
        }
        public (List<int>, Dictionary<int, int>) GetAllShowGoatIds()
        {
            var goatIds = new List<int>();
            var goatDictionary = new Dictionary<int, int>();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select * from showGoats";
                var command = new MySqlCommand(query, connection);
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    goatIds.Add(reader.GetInt32("goatId"));
                    goatDictionary.Add(reader.GetInt32("goatId"),reader.GetInt32("id"));
                }
                reader.Close();
            }

            return (goatIds, goatDictionary);
        }
        public bool DoesUserHaveGoatShowing(ulong ownerId)
        {
            var hasShowGoats = false;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select * from showGoats where ownerId = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("ownerId", MySqlDbType.VarChar).Value = ownerId;
                connection.Open();
                var reader = command.ExecuteReader();
                hasShowGoats = reader.HasRows;
                reader.Close();
            }
            return hasShowGoats;
        }
        public void AddGoatToShow(int goatId, ulong ownerId)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "insert into showGoats(goatId, ownerId) values (?goatId, ?ownerId)";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goatId;
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ownerId;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        public (bool, string) CheckExpAgainstNextLevel(ulong userId, decimal expToAdd)
        {
            //n =  ln(FV / PV)ln(1 + r)
            // exp = 10 * 1.05^level
            var msg = "";
            var goatsLevelAndExp = GetCurrentLevelAndExpOfGoat(userId);
            var startingLevel = goatsLevelAndExp.Item1;
            if (goatsLevelAndExp.Item1 == 0) return (startingLevel != goatsLevelAndExp.Item1, msg);
            goatsLevelAndExp.Item1 =
                (int) Math.Floor(Math.Log((double) ((goatsLevelAndExp.Item2 + expToAdd) / 10)) / Math.Log(1.05));
            var newExp = goatsLevelAndExp.Item2 + expToAdd;
            // chance to die
            var rnd = new Random();
            var number = IsSpecialGoat(userId) ? 0 : rnd.Next(0, 71);
            if (startingLevel != goatsLevelAndExp.Item1 && number == 69)
            {
                // kill goat
                msg = KillGoat(userId);
                return (startingLevel != goatsLevelAndExp.Item1, msg);
            }

            msg = UpdateGoatLevelAndExperience(startingLevel, goatsLevelAndExp.Item1, newExp, userId);

            return (startingLevel != goatsLevelAndExp.Item1, msg);
        }

        private bool IsSpecialGoat(ulong userId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where ownerID = ?userId and equipped = 1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("id");
                        goat.Level = reader.GetInt32("level");
                        goat.LevelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.Experience = reader.GetDecimal("experience");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.BaseColour == BaseColour.Special;
        }

        public bool IsGoatSpecialByGoatId(int goatId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.VarChar).Value = goatId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("id");
                        goat.Level = reader.GetInt32("level");
                        goat.LevelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.Experience = reader.GetDecimal("experience");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.BaseColour == BaseColour.Special;
        }

        public bool IsGoatMinxByGoatId(int goatId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.VarChar).Value = goatId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("id");
                        goat.Level = reader.GetInt32("level");
                        goat.LevelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.Experience = reader.GetDecimal("experience");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.Breed == Breed.Minx;
        }

        public bool IsGoatBumbleByGoatId(int goatId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.VarChar).Value = goatId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("id");
                        goat.Level = reader.GetInt32("level");
                        goat.LevelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.Experience = reader.GetDecimal("experience");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.Breed == Breed.Bumble;
        }

        public bool IsGoatTaillessByGoatId(int goatId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.Int32).Value = goatId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("id");
                        goat.Level = reader.GetInt32("level");
                        goat.LevelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.Experience = reader.GetDecimal("experience");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.Breed == Breed.Tailless;
        }
        
        public bool IsGoatZenByGoatId(int goatId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.VarChar).Value = goatId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("id");
                        goat.Level = reader.GetInt32("level");
                        goat.LevelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.Experience = reader.GetDecimal("experience");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.Breed == Breed.Zenyatta;
        }

        private bool IsChristmasGoat(int goatId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.VarChar).Value = goatId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("id");
                        goat.Level = reader.GetInt32("level");
                        goat.LevelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.Experience = reader.GetDecimal("experience");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.Breed == Breed.Christmas;
        }

        private bool IsValentinesGoat(int goatId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select * from goats where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.Int32).Value = goatId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                    }
                }

                reader.Close();
            }

            return goat.Breed == Breed.Valentines;
        }
        private string KillGoat(ulong userId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where ownerID = ?userId and equipped = 1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("id");
                        goat.Level = reader.GetInt32("level");
                        goat.LevelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.Experience = reader.GetDecimal("experience");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "insert into deadgoats (goatid, baseColour, breed, level, name, ownerID, imageLink) " +
                            "values (?goatid, ?baseColour, ?breed, ?level, ?name, ?ownerID, ?imageLink)";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goat.Id;
                command.Parameters.Add("?breed", MySqlDbType.VarChar).Value = Enum.GetName(typeof(Breed), goat.Breed);
                command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                    Enum.GetName(typeof(BaseColour), goat.BaseColour);
                command.Parameters.Add("?level", MySqlDbType.Int32).Value = goat.Level;
                command.Parameters.Add("?name", MySqlDbType.VarChar).Value = goat.Name;
                command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = goat.FilePath;
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                command.ExecuteNonQuery();
            }

            DeleteGoat(goat.Id);
            var lines = File.ReadLines(deathPath + "/deaths.txt").ToList();
            var rnd = new Random();
            var msg =
                $"Oh no! Your goat has unfortunately died from {lines.ElementAt(rnd.Next(0, lines.Count))} - rest in peace {goat.Name}";
            return msg;
        }

        private (int, decimal) GetCurrentLevelAndExpOfGoat(ulong userId)
        {
            var goatsLevel = 0;
            decimal goatsExperience = 0;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "Select level, experience from goats Where ownerID = ?ownerId and equipped = 1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar, 40).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goatsLevel = reader.GetInt32("level");
                        goatsExperience = reader.GetDecimal("experience");
                    }

                reader.Close();
            }

            return (goatsLevel, goatsExperience);
        }

        public void UpdateGoatImagesForKidsThatAreAdults(ulong userId)
        {
            var goats = new List<Goat>();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "Select * from goats where ownerID = ?ownerId and type = ?type and level > ?level";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = userId;
                command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                command.Parameters.Add("?level", MySqlDbType.Int32).Value = 99;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        var goat = new Goat();
                        goat.Id = reader.GetInt32("id");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                        goat.FilePath = reader.GetString("imageLink");
                        goats.Add(goat);
                    }

                reader.Close();
            }

            goats.ForEach(goat => UpdateGoatImage(goat));
        }

        private void UpdateGoatImage(Goat goat)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "Update goats Set imageLink = ?imageLink, type = ?type Where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value =
                    $"Goat_Images/{GetAdultGoatImageUrlFromGoatObject(goat)}";
                command.Parameters.Add("?id", MySqlDbType.Int32).Value = goat.Id;
                command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Adult";
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private string UpdateGoatLevelAndExperience(int oldLevel, int level, decimal experience, ulong userId)
        {
            if (level >= 100 && oldLevel < 100)
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Update goats Set level = ?level, experience = ?experience, type = ?type, " +
                                "imageLink = ?imageLink where ownerID = ?ownerId and equipped = 1";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?level", MySqlDbType.Int32).Value = level;
                    command.Parameters.Add("?experience", MySqlDbType.Decimal).Value = experience;
                    command.Parameters.Add("?ownerId", MySqlDbType.VarChar, 40).Value = userId;
                    command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Adult";
                    command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value =
                        $"Goat_Images/{GetAdultGoatImageUrl(userId)}";
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            else
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query =
                        "Update goats Set level = ?level, experience = ?experience where ownerID = ?ownerId and equipped = 1";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?level", MySqlDbType.Int32).Value = level;
                    command.Parameters.Add("?experience", MySqlDbType.Decimal).Value = experience;
                    command.Parameters.Add("?ownerId", MySqlDbType.VarChar, 40).Value = userId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

            return "Congrats your current goat has just gained a level";
        }

        private string GetAdultGoatImageUrlFromGoatObject(Goat goat)
        {
            if (IsChristmasGoat(goat.Id))
            {
                if (goat.FilePath.EndsWith("SantaKid.png"))
                    return "Special Variations/SantaAdult.png";
                if (goat.FilePath.EndsWith("AngelLightsKid.png"))
                    return "Special Variations/AngelLightsAdult.png";
                if (goat.FilePath.EndsWith("GrinchKid.png")) return "Special Variations/GrinchAdult.png";
            }
            else if (IsValentinesGoat(goat.Id))
            {
                if (goat.FilePath.Contains("Heart"))
                    return "Valentine_Special_Variations/HeartAdult.png";
                if (goat.FilePath.Contains("Cupid"))
                    return "Valentine_Special_Variations/CupidAdult.png";
                if (goat.FilePath.Contains("Roses"))
                    return "Valentine_Special_Variations/RosesAdult.png";
            }
            else if (IsGoatBumbleByGoatId(goat.Id))
            {
                return "Special Variations/BumbleAdult.png";
            }
            else if (IsGoatMinxByGoatId(goat.Id))
            {
                return "Special Variations/MinxAdult.png";
            }
            else if (IsGoatZenByGoatId(goat.Id))
            {
                return "Special Variations/ZenyattaAdult.png";
            }
            else if (IsGoatTaillessByGoatId(goat.Id))
            {
                return "Special Variations/taillessadult.png";
            }

            var goatColour = "";

            string goatName;
            if (goat.Breed.Equals(Breed.La_Mancha))
                goatName = "LM";
            else if (goat.Breed.Equals(Breed.Nigerian_Dwarf))
                goatName = "ND";
            else
                goatName = "NB";

            if (goat.BaseColour.Equals(BaseColour.Black))
                goatColour = "black";
            else if (goat.BaseColour.Equals(BaseColour.Chocolate))
                goatColour = "chocolate";
            else if (goat.BaseColour.Equals(BaseColour.Gold))
                goatColour = "gold";
            else if (goat.BaseColour.Equals(BaseColour.Red))
                goatColour = "red";
            else if (goat.BaseColour.Equals(BaseColour.White)) goatColour = "white";
            var random = new Random();
            var count = Directory.GetFiles(
                $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/NB White", "*",
                SearchOption.TopDirectoryOnly).Length;
            var randomNumber = random.Next(count);
            if (randomNumber == 0)
                return $"{goatName} {FirstCharToUpper(goatColour)}/{goatName}adult{goatColour}.png";
            return $"{goatName} {FirstCharToUpper(goatColour)}/{goatName}adult{goatColour}{randomNumber}.png";
        }

        private string GetAdultGoatImageUrl(ulong userId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "Select * from goats where equipped = 1 and ownerID = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        Enum.TryParse(reader.GetString("baseColour"), out BaseColour colour);
                        Enum.TryParse(reader.GetString("breed"), out Breed breed);
                        goat.Id = reader.GetInt32("id");
                        goat.BaseColour = colour;
                        goat.Breed = breed;
                        goat.FilePath = reader.GetString("imageLink");
                    }
            }

            if (IsChristmasGoat(goat.Id))
            {
                if (goat.FilePath.EndsWith("SantaKid.png"))
                    return "Special Variations/SantaAdult.png";
                if (goat.FilePath.EndsWith("AngelLightsKid.png"))
                    return "Special Variations/AngelLightsAdult.png";
                if (goat.FilePath.EndsWith("GrinchKid.png")) return "Special Variations/GrinchAdult.png";
            }
            else if (IsValentinesGoat(goat.Id))
            {
                if (goat.FilePath.Contains("Heart"))
                    return "Valentine_Special_Variations/HeartAdult.png";
                if (goat.FilePath.Contains("Cupid"))
                    return "Valentine_Special_Variations/CupidAdult.png";
                if (goat.FilePath.Contains("Roses"))
                    return "Valentine_Special_Variations/RosesAdult.png";
            }
            else if (IsGoatBumbleByGoatId(goat.Id))
            {
                return "Special Variations/BumbleAdult.png";
            }
            else if (IsGoatMinxByGoatId(goat.Id))
            {
                return "Special Variations/MinxAdult.png";
            }
            else if (IsGoatZenByGoatId(goat.Id))
            {
                return "Special Variations/ZenyattaAdult.png";
            }
            else if (IsGoatTaillessByGoatId(goat.Id))
            {
                return "Special Variations/taillessadult.png";
            }

            var goatColour = "";

            string goatName;
            if (goat.Breed.Equals(Breed.La_Mancha))
                goatName = "LM";
            else if (goat.Breed.Equals(Breed.Nigerian_Dwarf))
                goatName = "ND";
            else
                goatName = "NB";

            if (goat.BaseColour.Equals(BaseColour.Black))
                goatColour = "black";
            else if (goat.BaseColour.Equals(BaseColour.Chocolate))
                goatColour = "chocolate";
            else if (goat.BaseColour.Equals(BaseColour.Gold))
                goatColour = "gold";
            else if (goat.BaseColour.Equals(BaseColour.Red))
                goatColour = "red";
            else if (goat.BaseColour.Equals(BaseColour.White)) goatColour = "white";
            var random = new Random();
            var count = Directory.GetFiles(
                $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/NB White", "*",
                SearchOption.TopDirectoryOnly).Length;
            var randomNumber = random.Next(count);
            if (randomNumber == 0)
                return $"{goatName} {FirstCharToUpper(goatColour)}/{goatName}adult{goatColour}.png";
            return $"{goatName} {FirstCharToUpper(goatColour)}/{goatName}adult{goatColour}{randomNumber}.png";
        }


        public string FirstCharToUpper(string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }

        public bool CanGoatsFitInBarn(ulong discordId, int numberOfGoatsToAdd)
        {
            var barnSize = 10;
            var numberOfGoats = 0;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "Select barnsize from farmers where DiscordID = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        barnSize = reader.GetInt32("barnsize");
                reader.Close();
            }

            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "SELECT COUNT(*) as numberOfGoats FROM goats WHERE ownerID = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        numberOfGoats = reader.GetInt32("numberOfGoats");
                reader.Close();
            }

            return barnSize != numberOfGoats && barnSize >= numberOfGoats + numberOfGoatsToAdd;
        }

        public List<Goat> ReturnUsersGoats(ulong userId)
        {
            var goats = new List<Goat>();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where ownerID = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        var goat = new Goat();
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("id");
                        goat.Level = reader.GetInt32("level");
                        goat.LevelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.Experience = reader.GetDecimal("experience");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                        goat.Type = (Type) Enum.Parse(typeof(Type), reader.GetString("type"));
                        goats.Add(goat);
                    }
            }

            return goats;
        }

        public List<Goat> ReturnUsersDeadGoats(ulong userId)
        {
            var goats = new List<Goat>();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from deadgoats where ownerID = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        var goat = new Goat();
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("goatid");
                        goat.Level = reader.GetInt32("level");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                        goats.Add(goat);
                    }
            }

            return goats;
        }

        public (List<int>,Dictionary<int, string>) ReturnUsersAdultGoatIdsInKiddingPen(ulong userId)
        {
            var goatIds = new List<int>();
            var dictionary = new Dictionary<int, string>();
            var ownedGoats = ReturnUsersGoats(userId);
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select goatId, dueDate from cookingdoes where ready = 0";
                var command = new MySqlCommand(query, connection);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        goatIds.Add(reader.GetInt32("goatId"));
                        dictionary.Add(reader.GetInt32("goatId"), reader.GetString("dueDate"));
                    }
                }

            }

            return (ownedGoats.Select(goat => goat.Id).ToList().Where(id => goatIds.Contains(id)).ToList(), dictionary);
        }

        public List<Goat> ReturnUsersKidsInKiddingPen(ulong userId)
        {
            var kids = new List<Goat>();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from newbornkids where ownerID = ?ownerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var goat = new Goat();
                    goat.Id = reader.GetInt32("id");
                    goat.Name = reader.GetString("name");
                    goat.Level = reader.GetInt32("level");
                    goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                    goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    goat.FilePath = $"{reader.GetString("imageLink")}";
                    kids.Add(goat);
                }
            }

            return kids;
        }

        public Goat GetEquippedGoat(ulong userId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where ownerID = ?ownerID and equipped = 1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.Name = reader.GetString("name");
                        goat.Id = reader.GetInt32("id");
                        goat.Level = reader.GetInt32("level");
                        goat.LevelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.Experience = reader.GetDecimal("experience");
                        goat.FilePath = reader.GetString("imageLink");
                        goat.Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }
            }

            return goat;
        }

        public bool CanFarmerAffordGoat(int buyPrice, ulong userId)
        {
            var credits = 0;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select credits from farmers where DiscordID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        credits = reader.GetInt32("credits");
                reader.Close();
            }

            return credits >= buyPrice;
        }

        public void DeleteKidFromKiddingPen(int id)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "delete from newbornkids where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.Int32).Value = id;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void MoveKidIntoGoatPen(Goat goat, ulong userId)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query =
                    "insert into goats (level, name, type, breed, baseColour, ownerID, experience, imageLink) " +
                    "values (?level, ?name, ?type, ?breed, ?baseColour, ?ownerID, ?experience, ?imageLink)";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?level", MySqlDbType.Int32).Value = goat.Level;
                command.Parameters.Add("?name", MySqlDbType.VarChar).Value = goat.Name;
                command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                command.Parameters.Add("?breed", MySqlDbType.VarChar).Value = Enum.GetName(typeof(Breed), goat.Breed);
                command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                    Enum.GetName(typeof(BaseColour), goat.BaseColour);
                command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = userId;
                command.Parameters.Add("?experience", MySqlDbType.Decimal).Value =
                    (int) Math.Ceiling(10 * Math.Pow(1.05, goat.Level - 1));
                command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = goat.FilePath;
                connection.Open();
                command.ExecuteNonQuery();
            }

            DeleteKidFromKiddingPen(goat.Id);
        }

        public void DeleteGoat(int id)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "delete from goats where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.Int32).Value = id;
                connection.Open();
                command.ExecuteNonQuery();
            }

            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "delete from grazing where goatId = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.Int32).Value = id;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public bool IsGoatCooking(int goatId)
        {
            var cooking = false;

            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "Select * from cookingdoes where goatId = ?goatId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goatId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows) cooking = true;
                reader.Close();
            }

            return cooking;
        }
    }
}