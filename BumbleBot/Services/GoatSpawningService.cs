using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services
{
    public class GoatSpawningService
    {
        private readonly DbUtils dbUtils = new();
        private ulong goatSpawnChannelId = 762230405784272916;
         public (Goat, string) GenerateSpecialPaddyGoatToSpawn()
        {
             var specialGoat = new Goat();
             specialGoat.Breed = Breed.Shamrock;
             specialGoat.BaseColour = BaseColour.Special;
             specialGoat.Level = new Random().Next(76, 100);
             specialGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
             specialGoat.Name = "Shamrock Goat";
             var paddysGoats = new List<String>()
             {
                "ShamrockKid.png",
                "LeprechaunKid.png",
                "KissMeKid.png"
             };
             var rnd = new Random();
             specialGoat.FilePath = paddysGoats[rnd.Next(0,3)];
             var filePath =
                $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Shamrock_Special_Variations/" +
                $"/{specialGoat.FilePath}";
             return (specialGoat, filePath);
         }
         
         public (Goat, string) GenerateSpecialSpringGoatToSpawn()
         {
             var specialGoat = new Goat();
             specialGoat.Breed = Breed.Spring;
             specialGoat.BaseColour = BaseColour.Special;
             specialGoat.Level = new Random().Next(76, 100);
             specialGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
             specialGoat.Name = "Spring Goat";
             var springGoats = new List<String>()
             {
                 "GardenKid.png",
                 "SpringNubianKid.png",
                 "SpringKiddingKid.png"
             };
             var rnd = new Random();
             specialGoat.FilePath = springGoats[rnd.Next(0, 3)];
             var filePath =
                 $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Spring Specials/" +
                 $"/{specialGoat.FilePath}";
             return (specialGoat, filePath);
         }
         
         public (Goat, string) GenerateBestestGoatToSpawn()
         {
             var bestGoat = new Goat();
             bestGoat.Breed = Breed.Dazzle;
             bestGoat.BaseColour = BaseColour.Special;
             bestGoat.Level = new Random().Next(76, 100);
             bestGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, bestGoat.Level - 1));
             bestGoat.Name = "Dazzle aka bestest goat of them all";
             bestGoat.FilePath = "DazzleSpecialKid.png";
             var filePath =
                 $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/" +
                 $"/{bestGoat.FilePath}";
             return (bestGoat, filePath);
         }
 
         public (Goat, string) GenerateMemberSpecialGoatToSpawn()
         {
             var memberGoat = new Goat();
             memberGoat.Breed = Breed.MemberSpecial;
             memberGoat.BaseColour = BaseColour.Special;
             memberGoat.Level = new Random().Next(76, 100);
             memberGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, memberGoat.Level - 1));
             memberGoat.Name = "Member Special";
             var memberGoats = new List<String>()
             {
                 "MemberSpecialEponaKid.png",
                 "MemberSpecialGiuhKid.png",
                 "MemberSpecialKimdolKid.png"
             };
             memberGoat.FilePath = memberGoats[new Random().Next(0, 3)];
             var filePath =
                 $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/" +
                 $"{memberGoat.FilePath}";
             return (memberGoat, filePath);
         }
         
        public bool AreMemberSpawnsEnabled()
        {
            var enabled = false;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select boolValue from config where paramName = ?param";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?param", MySqlDbType.VarChar).Value = "memberSpecials";
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        enabled = reader.GetBoolean("boolValue");
                    }
                }
                reader.Close();
                connection.Close();
            }
            return enabled;
        }
        
        public bool ArePaddysSpawnsEnabled()
        {
            var enabled = false;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select boolValue from config where paramName = ?param";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?param", MySqlDbType.VarChar).Value = "paddysSpecials";
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        enabled = reader.GetBoolean("boolValue");
                reader.Close();
            }

            return enabled;
        }

        public bool AreSpringSpawnsEnabled()
        {
            var enabled = false;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select boolValue from config where paramName = ?param";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?param", "springSpecials");
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        enabled = reader.GetBoolean("boolValue");
                    }
                reader.Close();
            }

            return enabled;
        }

        public bool AreDazzleSpawnsEnabled()
        {
            var enabled = false;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select boolValue from config where paramName = ?param";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?param", "bestestGoat");
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        enabled = reader.GetBoolean("boolValue");
                    }
                reader.Close();
                connection.Close();
            }

            return enabled;
        }
        public bool AreValentinesSpawnsEnabled()
        {
            var enabled = false;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select boolValue from config where paramName = ?param";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?param", MySqlDbType.VarChar).Value = "valentinesSpecials";
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        enabled = reader.GetBoolean("boolValue");
                reader.Close();
            }
            return enabled;
        }
        public bool AreChristmasSpawnsEnabled()
        {
            var enabled = false;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select boolValue from config where paramName = ?param";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?param", MySqlDbType.VarChar).Value = "christmasSpecials";
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        enabled = reader.GetBoolean("boolValue");
            }

            return enabled;
        }
        
        public bool AreTaillessSpawnsEnabled()
        {
            var enabled = false;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select boolValue from config where paramName = ?param";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?param", MySqlDbType.VarChar).Value = "taillessEnabled";
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        enabled = reader.GetBoolean("boolValue");
            }

            return enabled;
        }
        
        public async Task SpawnGoatFromGoatObject(MessageCreateEventArgs e, Goat goatToSpawn, string fullFilePath, DiscordClient Client)
        {
            try
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{goatToSpawn.Name} has become available, type purchase to add her to your herd",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = $"attachment://{goatToSpawn.FilePath}"
                };
                embed.AddField("Cost", (goatToSpawn.Level - 1).ToString());
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goatToSpawn.BaseColour));
                embed.AddField("Breed", Enum.GetName(typeof(Breed), goatToSpawn.Breed)?.Replace("_", " "), true);
                embed.AddField("Level", (goatToSpawn.Level - 1).ToString(), true);

                var fileStream = File.OpenRead(fullFilePath);
                var interactivity = Client.GetInteractivity();
                var goatMsg = await new DiscordMessageBuilder()
                    .WithEmbed(embed)
                    .WithFile(goatToSpawn.FilePath, fileStream)
                    .SendAsync(e.Guild.GetChannel(goatSpawnChannelId));
                
                var msg = await interactivity.WaitForMessageAsync(x => x.Channel == e.Guild.GetChannel(goatSpawnChannelId)
                && x.Content.ToLower().Trim() == "purchase", TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                await goatMsg.DeleteAsync();
                var goatService = new GoatService();
                if (msg.TimedOut)
                {
                    await e.Guild.GetChannel(goatSpawnChannelId)
                        .SendMessageAsync($"No one decided to purchase {goatToSpawn.Name}")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanGoatsFitInBarn(msg.Result.Author.Id, 1))
                {
                    var member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await e.Guild.GetChannel(goatSpawnChannelId)
                        .SendMessageAsync(
                            $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanFarmerAffordGoat(goatToSpawn.Level - 1, msg.Result.Author.Id))
                {
                    var member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await e.Guild.GetChannel(goatSpawnChannelId)
                        .SendMessageAsync(
                            $"Unfortunately {member.DisplayName} you can't afford this goat and the it has gone back to market!")
                        .ConfigureAwait(false);
                }
                else
                {
                    await msg.Result.DeleteAsync();
                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = 
                            "INSERT INTO goats (level, name, type, breed, baseColour, ownerID, experience, imageLink)" +
                            "VALUES (?level, ?name, ?type, ?breed, ?baseColour, ?ownerID, ?exp, ?imageLink)";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?level", MySqlDbType.Int32).Value = goatToSpawn.Level - 1;
                        command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = goatToSpawn.Name;
                        command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                        command.Parameters.Add("?breed", MySqlDbType.VarChar).Value =
                            Enum.GetName(typeof(Breed), goatToSpawn.Breed);
                        command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                            Enum.GetName(typeof(BaseColour), goatToSpawn.BaseColour);
                        command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                        command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                            (int) Math.Ceiling(10 * Math.Pow(1.05, goatToSpawn.Level - 1));
                        command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value =
                            $"Goat_Images/Special Variations/{goatToSpawn.FilePath}";
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    var fs = new FarmerService();
                    fs.DeductCreditsFromFarmer(msg.Result.Author.Id, goatToSpawn.Level - 1);

                    await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync("Congrats " +
                            $"{e.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
                            $"{goatToSpawn.Name} for {(goatToSpawn.Level - 1).ToString()} credits")
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                await Console.Out.WriteLineAsync(ex.StackTrace);
            }
        }
    }
}