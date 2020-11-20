using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services
{
    public class GoatService
    {

        private DBUtils dBUtils = new DBUtils();
        private readonly string deathPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public GoatService()
        {
        }

        public (bool, string) CheckExpAgainstNextLevel(ulong userId, decimal expToAdd)
        {
            //n =  ln(FV / PV)ln(1 + r)
            // exp = 10 * 1.05^level
            string msg = "";
            (int, decimal) goatsLevelAndExp = GetCurrentLevelAndExpOfGoat(userId);
            int startingLevel = goatsLevelAndExp.Item1;
            if (goatsLevelAndExp.Item1 != 0)
            {
                goatsLevelAndExp.Item1 = (int)Math.Floor(Math.Log((double)((goatsLevelAndExp.Item2 + expToAdd) / 10)) / Math.Log(1.05));
                decimal newExp = goatsLevelAndExp.Item2 + expToAdd;
                // chance to die
                Random rnd = new Random();
                int number = rnd.Next(0, 71);
                //number = 69;
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

        private string KillGoat(ulong userId)
        {
            Goat goat = new Goat();
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select * from goats where ownerID = ?userId and equipped = 1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while(reader.Read())
                    {
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed)Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour)Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }
                }
                reader.Close();
            }

            using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "insert into deadgoats (goatid, baseColour, breed, level, name, ownerID, imageLink) " +
                    "values (?goatid, ?baseColour, ?breed, ?level, ?name, ?ownerID, ?imageLink)";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goat.id;
                command.Parameters.Add("?breed", MySqlDbType.VarChar).Value = Enum.GetName(typeof(Breed), goat.breed);
                command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value = Enum.GetName(typeof(BaseColour), goat.baseColour);
                command.Parameters.Add("?level", MySqlDbType.Int32).Value = goat.level;
                command.Parameters.Add("?name", MySqlDbType.VarChar).Value = goat.name;
                command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = goat.filePath;
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                command.ExecuteNonQuery();
            }
            DeleteGoat(goat.id);
            List<string> lines = File.ReadLines(deathPath + "/deaths.txt").ToList();
            Random rnd = new Random();
            string msg = $"Oh no! Your goat has unfortunately died from {lines.ElementAt(rnd.Next(0, lines.Count))} - rest in peace {goat.name}";
            return msg;
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

        public void UpdateGoatImagesForKidsThatAreAdults(ulong userId)
        {
            List<Goat> goats = new List<Goat>();
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "Select * from goats where ownerID = ?ownerId and type = ?type and level > ?level";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = userId;
                command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                command.Parameters.Add("?level", MySqlDbType.Int32).Value = 99;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while(reader.Read())
                    {
                        Goat goat = new Goat();
                        goat.id = reader.GetInt32("id");
                        goat.breed = (Breed)Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour)Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                        goats.Add(goat);
                    }
                }
                reader.Close();
            }
            goats.ForEach(goat => UpdateGoatImage(goat));

        }

        private void UpdateGoatImage(Goat goat)
        {
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "Update goats Set imageLink = ?imageLink, type = ?type Where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = $"Goat_Images/{GetAdultGoatImageUrlFromGoatObject(goat)}";
                command.Parameters.Add("?id", MySqlDbType.Int32).Value = goat.id;
                command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Adult";
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private string UpdateGoatLevelAndExperience(int oldLevel, int level, decimal experience, ulong userId)
        {
            if (level >= 100 && oldLevel < 100)
            {
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Update goats Set level = ?level, experience = ?experience, type = ?type, " +
                        "imageLink = ?imageLink where ownerID = ?ownerId and equipped = 1";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?level", MySqlDbType.Int32).Value = level;
                    command.Parameters.Add("?experience", MySqlDbType.Decimal).Value = experience;
                    command.Parameters.Add("?ownerId", MySqlDbType.VarChar, 40).Value = userId;
                    command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Adult";
                    command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = $"Goat_Images/{GetAdultGoatImageUrl(userId)}";
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
            return "Congrats your current goat has just gained a level";
        }

        private string GetAdultGoatImageUrlFromGoatObject(Goat goat)
        {
            string imageUrl = "";
            string goatName = "";
            string goatColour = "";

            if (goat.breed.Equals(Breed.La_Mancha))
            {
                goatName = "LM";
            }
            else if (goat.breed.Equals(Breed.Nigerian_Dwarf))
            {
                goatName = "ND";
            }
            else
            {
                goatName = "NB";
            }

            if (goat.baseColour.Equals(BaseColour.Black))
            {
                goatColour = "black";
            }
            else if (goat.baseColour.Equals(BaseColour.Chocolate))
            {
                goatColour = "chocolate";
            }
            else if (goat.baseColour.Equals(BaseColour.Gold))
            {
                goatColour = "gold";
            }
            else if (goat.baseColour.Equals(BaseColour.Red))
            {
                goatColour = "red";
            }
            else if (goat.baseColour.Equals(BaseColour.White))
            {
                goatColour = "white";
            }
            Random random = new Random();
            int randomNumber = random.Next(9);
            if (randomNumber == 0)
            {
                return $"{goatName} {FirstCharToUpper(goatColour)}/{goatName}adult{goatColour}.png";
            }
            else
            {
                return $"{goatName} {FirstCharToUpper(goatColour)}/{goatName}adult{goatColour}{randomNumber}.png";
            }
        }
        private string GetAdultGoatImageUrl(ulong userId)
        {
            Goat goat = new Goat();
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "Select * from goats where equipped = 1 and ownerID = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Enum.TryParse(reader.GetString("baseColour"), out BaseColour colour);
                        Enum.TryParse(reader.GetString("breed"), out Breed breed);
                        goat.baseColour = colour;
                        goat.breed = breed;
                    }
                }
            }
            string imageUrl = "";
            string goatName = "";
            string goatColour = "";

            if (goat.breed.Equals(Breed.La_Mancha))
            {
                goatName = "LM";
            }
            else if (goat.breed.Equals(Breed.Nigerian_Dwarf))
            {
                goatName = "ND";
            }
            else
            {
                goatName = "NB";
            }

            if (goat.baseColour.Equals(BaseColour.Black))
            {
                goatColour = "black";
            }
            else if (goat.baseColour.Equals(BaseColour.Chocolate))
            {
                goatColour = "chocolate";
            }
            else if (goat.baseColour.Equals(BaseColour.Gold))
            {
                goatColour = "gold";
            }
            else if (goat.baseColour.Equals(BaseColour.Red))
            {
                goatColour = "red";
            }
            else if (goat.baseColour.Equals(BaseColour.White))
            {
                goatColour = "white";
            }
            Random random = new Random();
            int randomNumber = random.Next(9);
            if (randomNumber == 0)
            {
                return $"{goatName} {FirstCharToUpper(goatColour)}/{goatName}adult{goatColour}.png";
            }
            else
            {
                return $"{goatName} {FirstCharToUpper(goatColour)}/{goatName}adult{goatColour}{randomNumber}.png";
            }
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
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed)Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour)Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                        goats.Add(goat);
                    }
                }
            }
            return goats;
        }

        public Goat GetEquippedGoat(ulong userId)
        {
            Goat goat = new Goat();
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select * from goats where ownerID = ?ownerID and equipped = 1";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while(reader.Read())
                    {
                        goat.name = reader.GetString("name");
                        goat.id = reader.GetInt32("id");
                        goat.level = reader.GetInt32("level");
                        goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                        goat.experience = reader.GetDecimal("experience");
                        goat.filePath = reader.GetString("imageLink");
                        goat.breed = (Breed)Enum.Parse(typeof(Breed), reader.GetString("breed"));
                        goat.baseColour = (BaseColour)Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                    }
                }
            }
            return goat;
        }

        public bool CanFarmerAffordGoat(int buyPrice, ulong userId)
        {
            int credits = 0;
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select credits from farmers where DiscordID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while(reader.Read())
                    {
                        credits = reader.GetInt32("credits");
                    }
                }
                reader.Close();
            }

            return credits >= buyPrice;
        }

        public void DeleteGoat(int id)
        {
            using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "delete from goats where id = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.Int32).Value = id;
                connection.Open();
                command.ExecuteNonQuery();
            }

            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "delete from grazing where goatId = ?id";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?id", MySqlDbType.Int32).Value = id;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}
