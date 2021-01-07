using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BumbleBot.Models;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;
using Type = BumbleBot.Models.Type;

namespace BumbleBot.Services
{
    public class GoatService
    {
        private readonly string deathPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        private readonly DBUtils dBUtils = new DBUtils();

        public (bool, string) CheckExpAgainstNextLevel(ulong userId, decimal expToAdd)
        {
            //n =  ln(FV / PV)ln(1 + r)
            // exp = 10 * 1.05^level
            var msg = "";
            var goatsLevelAndExp = GetCurrentLevelAndExpOfGoat(userId);
            var startingLevel = goatsLevelAndExp.Item1;
            if (goatsLevelAndExp.Item1 != 0)
            {
                goatsLevelAndExp.Item1 =
                    (int) Math.Floor(Math.Log((double) ((goatsLevelAndExp.Item2 + expToAdd) / 10)) / Math.Log(1.05));
                var newExp = goatsLevelAndExp.Item2 + expToAdd;
                // chance to die
                var rnd = new Random();
                int number;
                number = IsSpecialGoat(userId) ? 0 : rnd.Next(0, 71);
                if (startingLevel != goatsLevelAndExp.Item1 && number == 69)
                {
                    // kill goat
                    msg = KillGoat(userId);
                    return (startingLevel != goatsLevelAndExp.Item1, msg);
                }

                msg = UpdateGoatLevelAndExperience(startingLevel, goatsLevelAndExp.Item1, newExp, userId);
            }

            return (startingLevel != goatsLevelAndExp.Item1, msg);
        }

        private bool IsSpeicalGoatMinx(ulong userId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where ownerID = ?userId and equipped =1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.breed == Breed.Minx;
        }

        private bool IsSpeicalGoatBumble(ulong userId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where ownerID = ?userId and equipped =1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.breed == Breed.Bumble;
        }

        private bool IsSpeicalGoatZen(ulong userId)
        {
            var goat = new Goat();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from goats where ownerID = ?userId and equipped =1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.breed == Breed.Zenyatta;
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
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.baseColour == BaseColour.Special;
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
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.baseColour == BaseColour.Special;
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
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.breed == Breed.Minx;
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
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.breed == Breed.Bumble;
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
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.breed == Breed.Zenyatta;
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
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            return goat.breed == Breed.Christmas;
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
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }

                reader.Close();
            }

            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "insert into deadgoats (goatid, baseColour, breed, level, name, ownerID, imageLink) " +
                            "values (?goatid, ?baseColour, ?breed, ?level, ?name, ?ownerID, ?imageLink)";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goat.id;
                command.Parameters.Add("?breed", MySqlDbType.VarChar).Value = Enum.GetName(typeof(Breed), goat.breed);
                command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                    Enum.GetName(typeof(BaseColour), goat.baseColour);
                command.Parameters.Add("?level", MySqlDbType.Int32).Value = goat.level;
                command.Parameters.Add("?name", MySqlDbType.VarChar).Value = goat.name;
                command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = goat.filePath;
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                command.ExecuteNonQuery();
            }

            DeleteGoat(goat.id);
            var lines = File.ReadLines(deathPath + "/deaths.txt").ToList();
            var rnd = new Random();
            var msg =
                $"Oh no! Your goat has unfortunately died from {lines.ElementAt(rnd.Next(0, lines.Count))} - rest in peace {goat.name}";
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
                        goat.id = reader.GetInt32("id");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                        goat.filePath = reader.GetString("imageLink");
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
                command.Parameters.Add("?id", MySqlDbType.Int32).Value = goat.id;
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
            if (IsChristmasGoat(goat.id))
            {
                if (goat.filePath.EndsWith("SantaKid.png"))
                    return "Special Variations/SantaAdult.png";
                if (goat.filePath.EndsWith("AngelLightsKid.png"))
                    return "Special Variations/AngelLightsAdult.png";
                if (goat.filePath.EndsWith("GrinchKid.png")) return "Special Variations/GrinchAdult.png";
            }
            else if (IsGoatBumbleByGoatId(goat.id))
            {
                return "Special Variations/BumbleAdult.png";
            }
            else if (IsGoatMinxByGoatId(goat.id))
            {
                return "Special Variations/MinxAdult.png";
            }
            else if (IsGoatZenByGoatId(goat.id))
            {
                return "Special Variations/ZenyattaAdult.png";
            }

            var goatColour = "";

            string goatName;
            if (goat.breed.Equals(Breed.La_Mancha))
                goatName = "LM";
            else if (goat.breed.Equals(Breed.Nigerian_Dwarf))
                goatName = "ND";
            else
                goatName = "NB";

            if (goat.baseColour.Equals(BaseColour.Black))
                goatColour = "black";
            else if (goat.baseColour.Equals(BaseColour.Chocolate))
                goatColour = "chocolate";
            else if (goat.baseColour.Equals(BaseColour.Gold))
                goatColour = "gold";
            else if (goat.baseColour.Equals(BaseColour.Red))
                goatColour = "red";
            else if (goat.baseColour.Equals(BaseColour.White)) goatColour = "white";
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
                        goat.id = reader.GetInt32("id");
                        goat.baseColour = colour;
                        goat.breed = breed;
                        goat.filePath = reader.GetString("imageLink");
                    }
            }

            if (IsChristmasGoat(goat.id))
            {
                if (goat.filePath.EndsWith("SantaKid.png"))
                    return "Special Variations/SantaAdult.png";
                if (goat.filePath.EndsWith("AngelLightsKid.png"))
                    return "Special Variations/AngelLightsAdult.png";
                if (goat.filePath.EndsWith("GrinchKid.png")) return "Special Variations/GrinchAdult.png";
            }
            else if (IsGoatBumbleByGoatId(goat.id))
            {
                return "Special Variations/BumbleAdult.png";
            }
            else if (IsGoatMinxByGoatId(goat.id))
            {
                return "Special Variations/MinxAdult.png";
            }
            else if (IsGoatZenByGoatId(goat.id))
            {
                return "Special Variations/ZenyattaAdult.png";
            }

            var goatColour = "";

            string goatName;
            if (goat.breed.Equals(Breed.La_Mancha))
                goatName = "LM";
            else if (goat.breed.Equals(Breed.Nigerian_Dwarf))
                goatName = "ND";
            else
                goatName = "NB";

            if (goat.baseColour.Equals(BaseColour.Black))
                goatColour = "black";
            else if (goat.baseColour.Equals(BaseColour.Chocolate))
                goatColour = "chocolate";
            else if (goat.baseColour.Equals(BaseColour.Gold))
                goatColour = "gold";
            else if (goat.baseColour.Equals(BaseColour.Red))
                goatColour = "red";
            else if (goat.baseColour.Equals(BaseColour.White)) goatColour = "white";
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

        public bool CanGoatFitInBarn(ulong discordId)
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

            return barnSize != numberOfGoats && barnSize > numberOfGoats;
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
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                        goat.type = (Type) Enum.Parse(typeof(Type), reader.GetString("type"));
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
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("goatid");
                        goat.level = reader.GetInt32("level");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                        goats.Add(goat);
                    }
            }

            return goats;
        }

        public List<int> ReturnUsersAdultGoatIdsInKiddingPen(ulong userId)
        {
            var goatIds = new List<int>();
            var ownedGoats = ReturnUsersGoats(userId);
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

            return ownedGoats.Select(goat => goat.id).ToList().Where(id => goatIds.Contains(id)).ToList();
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
                    goat.id = reader.GetInt32("id");
                    goat.name = reader.GetString("name");
                    goat.level = reader.GetInt32("level");
                    goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                    goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    goat.filePath = $"{reader.GetString("imageLink")}";
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
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
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
                command.Parameters.Add("?level", MySqlDbType.Int32).Value = goat.level;
                command.Parameters.Add("?name", MySqlDbType.VarChar).Value = goat.name;
                command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                command.Parameters.Add("?breed", MySqlDbType.VarChar).Value = Enum.GetName(typeof(Breed), goat.breed);
                command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                    Enum.GetName(typeof(BaseColour), goat.baseColour);
                command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = userId;
                command.Parameters.Add("?experience", MySqlDbType.Decimal).Value =
                    (int) Math.Ceiling(10 * Math.Pow(1.05, goat.level - 1));
                command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = goat.filePath;
                connection.Open();
                command.ExecuteNonQuery();
            }

            DeleteKidFromKiddingPen(goat.id);
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