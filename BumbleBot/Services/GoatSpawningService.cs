using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;
using Type = BumbleBot.Models.Type;

namespace BumbleBot.Services
{
    public class GoatSpawningService
    {
        private readonly DbUtils dbUtils = new();
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
                "/Goat_Images/Shamrock_Special_Variations/ShamrockKid.png",
                "/Goat_Images/Shamrock_Special_Variations/LeprechaunKid.png",
                "/Goat_Images/Shamrock_Special_Variations/KissMeKid.png"
             };
             var rnd = new Random();
             specialGoat.FilePath = paddysGoats[rnd.Next(0,3)];
             var filePath =
                $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{specialGoat.FilePath}";
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
                 "/Goat_Images/Spring Specials/GardenKid.png",
                 "/Goat_Images/Spring Specials/SpringNubianKid.png",
                 "/Goat_Images/Spring Specials/SpringKiddingKid.png"
             };
             var rnd = new Random();
             specialGoat.FilePath = springGoats[rnd.Next(0, 3)];
             var filePath =
                 $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{specialGoat.FilePath}";
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
             bestGoat.FilePath = "/Goat_Images/DazzleSpecialKid.png";
             var filePath =
                 $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{bestGoat.FilePath}";
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
                 "/Goat_Images/MemberSpecialEponaKid.png",
                 "/Goat_Images/MemberSpecialGiuhKid.png",
                 "/Goat_Images/MemberSpecialKimdolKid.png"
             };
             memberGoat.FilePath = memberGoats[new Random().Next(0, 3)];
             var filePath =
                 $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{memberGoat.FilePath}";
             return (memberGoat, filePath);
         }

         public (Goat, string) GenerateTaillessSpecialGoatToSpawn()
         {
             var specialGoat = new Goat();
             specialGoat.Breed = Breed.Tailless;
             specialGoat.BaseColour = BaseColour.Special;
             specialGoat.Level = new Random().Next(76, 100);
             specialGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
             specialGoat.Name = "Tailless Goat";
             specialGoat.FilePath = "/Goat_Images/Special Variations/taillesskid.png";
             var filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{specialGoat.FilePath}";
             return (specialGoat, filePath);
         }

         public (Goat, string) GenerateValentinesGoatToSpawn()
         {
             var specialGoat = new Goat();
             specialGoat.Breed = Breed.Valentines;
             specialGoat.BaseColour = BaseColour.Special;
             specialGoat.Level = new Random().Next(76, 100);
             specialGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
             specialGoat.Name = "Valentines Goat";
             var valentineGoats = new List<String>()
             {
                 "/Goat_Images/Valentine_Special_Variations/CupidKid.png",
                 "/Goat_Images/Valentine_Special_Variations/HeartKid.png",
                 "/Goat_Images/Valentine_Special_Variations/RosesKid.png"  
             };
             var rnd = new Random();
             specialGoat.FilePath = valentineGoats[rnd.Next(0,3)];
             var filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{specialGoat.FilePath}";
             return (specialGoat, filePath);
         }

         public (Goat, string) GenerateChristmasSpecialToSpawn()
         {
             var specialGoat = new Goat();
             specialGoat.BaseColour = BaseColour.Special;
             specialGoat.Breed = Breed.Christmas;
             specialGoat.Type = Type.Kid;
             specialGoat.LevelMulitplier = 1;
             specialGoat.Level = RandomLevel.GetRandomLevel();
             specialGoat.Name = "Christmas Special";
             var christmasGoats = new List<String>()
             {
                 "/Goat_Images/Special Variations/AngelLightsKid.png",
                 "/Goat_Images/Special Variations/GrinchKid.png",
                 "/Goat_Images/Special Variations/SantaKid.png"
             };
             var rnd = new Random();
             specialGoat.FilePath = christmasGoats[rnd.Next(0,3)];
             var filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{specialGoat.FilePath}";
             return (specialGoat, filePath);
         }

         public (Goat, string) GenerateNormalGoatToSpawn()
         {
             var rnd = new Random();
             var breed = rnd.Next(0, 3);
             var baseColour = rnd.Next(0, 5);
             var randomGoat = new Goat();
             randomGoat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour),
                 Enum.GetName(typeof(BaseColour), baseColour) ?? throw new InvalidOperationException());
             randomGoat.Breed = (Breed) Enum.Parse(typeof(Breed), Enum.GetName(typeof(Breed), breed) ?? throw new InvalidOperationException());
             randomGoat.Type = Type.Kid;
             randomGoat.Level = RandomLevel.GetRandomLevel();
             randomGoat.LevelMulitplier = 1;
             randomGoat.Name = "Unregistered Goat";
             randomGoat.Special = false;
             randomGoat.FilePath = $"/Goat_Images/Kids/{GetKidImage(randomGoat.Breed, randomGoat.BaseColour)}";
             var filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{randomGoat.FilePath}";
             return (randomGoat, filePath);
         }

         private string GetKidImage(Breed breed, BaseColour baseColour)
         {
             string goat;
             if (breed.Equals(Breed.Nubian))
                 goat = "NBkid";
             else if (breed.Equals(Breed.Nigerian_Dwarf))
                 goat = "NDkid";
             else
                 goat = "LMkid";

             string colour;
             if (baseColour.Equals(BaseColour.Black))
                 colour = "black";
             else if (baseColour.Equals(BaseColour.Chocolate))
                 colour = "chocolate";
             else if (baseColour.Equals(BaseColour.Gold))
                 colour = "gold";
             else if (baseColour.Equals(BaseColour.Red))
                 colour = "red";
             else
                 colour = "white";

             return $"{goat}{colour}.png";
         }
         public (Goat, string) GenerateSpecialGoatToSpawn()
         {
             var random = new Random();
             var number = random.Next(0, 3);
             var goat = new Goat();
             switch (number)
             {
                 case 0:
                     goat.Breed = Breed.Bumble;
                     goat.FilePath = "/Goat_Images/Special Variations/BumbleKid.png";
                     break;
                 case 1:
                     goat.Breed = Breed.Minx;
                     goat.FilePath = "/Goat_Images/Special Variations/MinxKid.png";
                     break;
                 default:
                     goat.Breed = Breed.Zenyatta;
                     goat.FilePath = "/Goat_Images/Special Variations/ZenyattaKid.png";
                     break;
             }

             goat.BaseColour = BaseColour.Special;
             goat.Level = RandomLevel.GetRandomLevel();
             goat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, goat.Level - 1));
             goat.LevelMulitplier = 1;
             goat.Type = Type.Kid;
             goat.Name = "Special Goat";
             var filePath =
                 $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{goat.FilePath}";
             return (goat, filePath);
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

        public async Task SpawnGoatFromGoatObject(DiscordChannel channel, DiscordGuild guild, (Goat, string) goatObject, DiscordClient client)
        {
            await SpawnGoatFromGoatObject(channel, guild, goatObject.Item1, goatObject.Item2, client);
        }
        public async Task SpawnGoatFromGoatObject(DiscordChannel channel, DiscordGuild guild, Goat goatToSpawn, string fullFilePath, DiscordClient client)
        {
            try
            {
                var url = "https://williamspires.com/";
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{goatToSpawn.Name} has become available, type purchase to add her to your herd",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = url + Uri.EscapeUriString(goatToSpawn.FilePath) //.Replace(" ", "%20")
                };
                embed.AddField("Cost", (goatToSpawn.Level - 1).ToString());
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goatToSpawn.BaseColour));
                embed.AddField("Breed", Enum.GetName(typeof(Breed), goatToSpawn.Breed)?.Replace("_", " "), true);
                embed.AddField("Level", (goatToSpawn.Level - 1).ToString(), true);
                
                var interactivity = client.GetInteractivity();
                var goatMsg = await new DiscordMessageBuilder()
                    .WithEmbed(embed)
                    .SendAsync(channel);
                
                var msg = await interactivity.WaitForMessageAsync(x => x.Channel == channel
                && x.Content.ToLower().Trim() == "purchase", TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                await goatMsg.DeleteAsync();
                var goatService = new GoatService();
                if (msg.TimedOut)
                {
                    await channel
                        .SendMessageAsync($"No one decided to purchase {goatToSpawn.Name}")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanGoatsFitInBarn(msg.Result.Author.Id, 1))
                {
                    var member = await guild.GetMemberAsync(msg.Result.Author.Id);
                    await channel
                        .SendMessageAsync(
                            $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanFarmerAffordGoat(goatToSpawn.Level - 1, msg.Result.Author.Id))
                {
                    var member = await guild.GetMemberAsync(msg.Result.Author.Id);
                    await channel
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
                        command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = goatToSpawn.FilePath;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    var fs = new FarmerService();
                    fs.DeductCreditsFromFarmer(msg.Result.Author.Id, goatToSpawn.Level - 1);

                    await channel.SendMessageAsync("Congrats " +
                                                   $"{guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
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