using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;
using MySql.Data.MySqlClient;
using Type = BumbleBot.Models.Type;

namespace BumbleBot.Services
{
    public class GoatSpawningService
    {
        private readonly DbUtils dbUtils = new();

        public (Goat, string) GenerateNovemberGoatToSpawn()
        {
            var novGoat = new Goat
            {
                Breed = Breed.November, BaseColour = BaseColour.Special, Level = new Random().Next(76, 100)
            };
            novGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, novGoat.Level - 1));
            novGoat.Name = "November Special";
            var novSpecials = new List<string>
            {
                "/Goat_Images/November/LingerieKid.png",
                "/Goat_Images/November/BlueAngelKid.png",
                "/Goat_Images/November/BBQKid.png"
            };
            var rnd = new Random();
            novGoat.FilePath = novSpecials[rnd.Next(0, novSpecials.Count)];
            var filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{novGoat.FilePath}";
            return (novGoat, filePath);
        }
        
        public (Goat, string) GenerateSpecialDairyGoatToSpawn()
        {
            var specialGoat = new Goat
            {
                Breed = Breed.DairySpecial, BaseColour = BaseColour.Special, Level = new Random().Next(76, 100)
            };
            specialGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
            specialGoat.Name = "Dairy Special";
            var dairySpecials = new List<string>()
            {
                "/Goat_Images/Dairy_Specials/CheeseKid.png",
                "/Goat_Images/Dairy_Specials/MilkerKid.png",
                "/Goat_Images/Dairy_Specials/MilkshakeKid.png"
            };
            var rnd = new Random();
            specialGoat.FilePath = dairySpecials[rnd.Next(0, dairySpecials.Count)];
            var filePath =
                $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{specialGoat.FilePath}";
            return (specialGoat, filePath);
        }
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

        public (Goat, string) GenerateHalloweenSpecialToSpawn()
        {
            var specialGoat = new Goat();
            specialGoat.Breed = Breed.Halloween;
            specialGoat.BaseColour = BaseColour.Special;
            specialGoat.Level = new Random().Next(76, 100);
            specialGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
            specialGoat.Name = "Halloween Goat";
            var halloweenGoats = new List<String>()
            {
                "/Goat_Images/Halloween Specials/CandyCornKid.png",
                "/Goat_Images/Halloween Specials/PinkyKid.png",
                "/Goat_Images/Halloween Specials/SkeletonKid.png"
            };
            var rnd = new Random();
            specialGoat.FilePath = halloweenGoats[rnd.Next(0, 3)];
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
                 "/Goat_Images/MemberSpecialKimdolKid.png",
                 "/Goat_Images/MemberSpecialKateKid.png",
                 "/Goat_Images/MemberSpecialMinxKid.png",
                 "/Goat_Images/MemberSpecialVenKid.png"
             };
             memberGoat.FilePath = memberGoats[new Random().Next(0, memberGoats.Count)];
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
             var specialGoat = new Goat
             {
                 BaseColour = BaseColour.Special,
                 Breed = Breed.Christmas,
                 Type = Type.Kid,
                 LevelMulitplier = 1,
                 Level = RandomLevel.GetRandomLevel(),
                 Name = "Christmas Special"
             };
             var christmasGoats = new List<string>()
             {
                 "/Goat_Images/Special Variations/AngelLightsKid.png",
                 "/Goat_Images/Special Variations/GrinchKid.png",
                 "/Goat_Images/Special Variations/SantaKid.png",
                 "/Goat_Images/Special Variations/ElfKid.png",
                 "/Goat_Images/Special Variations/LightsKid.png",
                 "/Goat_Images/Special Variations/ReindeerKid.png"
             };
             var rnd = new Random();
             specialGoat.FilePath = christmasGoats[rnd.Next(0, christmasGoats.Count)];
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
             randomGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, randomGoat.Level - 1));
             randomGoat.FilePath = $"/Goat_Images/Kids/{GetKidImage(randomGoat.Breed, randomGoat.BaseColour)}";
             var filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{randomGoat.FilePath}";
             return (randomGoat, filePath);
         }

         public (Goat, string) GenerateSummerGoatToSpawn()
         {
             var rnd = new Random();
             var summerGoat = new Goat();
             summerGoat.Breed = Breed.SummerSpecial;
             summerGoat.BaseColour = BaseColour.Special;
             summerGoat.Name = "Summer Special";
             summerGoat.Level = RandomLevel.GetRandomLevel();
             summerGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, summerGoat.Level - 1));
             var summerGoats = new List<String>()
             {
                 "/Goat_Images/Summer Specials/BeachKid.png",
                 "/Goat_Images/Summer Specials/FireworkKid.png",
                 "/Goat_Images/Summer Specials/WatermelonKid.png"
             };
             summerGoat.FilePath = summerGoats[rnd.Next(0, 3)];
             var filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{summerGoat.FilePath}";
             return (summerGoat, filePath);
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
             var number = random.Next(0, 5);
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
                 case 2:
                     goat.Breed = Breed.Juliet;
                     goat.FilePath = "/Goat_Images/Special Variations/JulietKid.png";
                     break;
                 case 3:
                     goat.Breed = Breed.Percy;
                     goat.FilePath = "/Goat_Images/Special Variations/PercyKid.png";
                     break;
                 case 4:
                     goat.Breed = Breed.Seven;
                     goat.FilePath = "/Goat_Images/Special Variations/SevenKid.png";
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

         public (Goat, string) GenerateBuckSpecialToSpawn()
         {
             var goat = new Goat();
             goat.Level = RandomLevel.GetRandomLevel();
             goat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, goat.Level - 1));
             goat.LevelMulitplier = 1;
             goat.Type = Type.Kid;
             goat.Name = "Unregistered Buck";
             goat.BaseColour = BaseColour.Special;
             goat.Breed = Breed.Buck;
             goat.FilePath = "/Goat_Images/Buck_Specials/BuckKid.png";
             var filePath =
                 $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{goat.FilePath}";
             return (goat, filePath);
         }
         public bool IsSpecialSpawnEnabled(string special)
         {
             var enabled = false;
             using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
             {
                 const string query = "select boolValue from config where paramName = ?param";
                 var command = new MySqlCommand(query, connection);
                 command.Parameters.AddWithValue("?param", special);
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
         public (Goat, string) GenerateBotBirthdaySpecialToSpawn()
         {
             var goat = new Goat();
             goat.Level = RandomLevel.GetRandomLevel();
             goat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, goat.Level - 1));
             goat.LevelMulitplier = 1;
             goat.Type = Type.Kid;
             goat.Name = "Bot Anniversary Kid";
             goat.BaseColour = BaseColour.Special;
             goat.Breed = Breed.BotAnniversarySpecial;
             goat.FilePath = "/Goat_Images/Special Variations/BirthdayBumble/FirstBirthdayBumbleKid.png";
             var filePath =
                 $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{goat.FilePath}";
             return (goat, filePath);
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
                    Title = $"{goatToSpawn.Name} has become available, click purchase below to add her to your herd",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = url + Uri.EscapeUriString(goatToSpawn.FilePath) //.Replace(" ", "%20")
                };
                embed.AddField("Cost", (goatToSpawn.Level - 1).ToString());
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goatToSpawn.BaseColour));
                embed.AddField("Breed", Enum.GetName(typeof(Breed), goatToSpawn.Breed)?.Replace("_", " "), true);
                embed.AddField("Level", (goatToSpawn.Level - 1).ToString(), true);
                
                var interactivity = client.GetInteractivity();
                var sellEmoji = DiscordEmoji.FromName(client, ":dollar:");
                var purchaseButton = new DiscordButtonComponent(ButtonStyle.Success, "purchase", "Purchase", 
                    false, new DiscordComponentEmoji(sellEmoji));
                var goatMsg = await new DiscordMessageBuilder()
                    .WithEmbed(embed)
                    .AddComponents(purchaseButton)
                    .SendAsync(channel);

                var buttonResult = await interactivity
                    .WaitForButtonAsync(goatMsg, TimeSpan.FromSeconds(45))
                    .ConfigureAwait(false);
                var goatService = new GoatService();
                if (buttonResult.TimedOut)
                {
                    await goatMsg.DeleteAsync();
                    await channel
                        .SendMessageAsync($"No one decided to purchase {goatToSpawn.Name}")
                        .ConfigureAwait(false);
                    return;
                }
                var perkService = new PerkService();
                var usersPerks = await perkService.GetUsersPerks(buttonResult.Result.User.Id);
                if (!goatService.CanGoatsFitInBarn(buttonResult.Result.User.Id, 1, usersPerks, client.Logger))
                {
                    await buttonResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                    await buttonResult.Result.Message.DeleteAsync();
                    var member = await guild.GetMemberAsync(buttonResult.Result.User.Id);
                    await channel
                        .SendMessageAsync(
                            $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanFarmerAffordGoat(goatToSpawn.Level - 1, buttonResult.Result.User.Id))
                {
                    await buttonResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                    await buttonResult.Result.Message.DeleteAsync();
                    var member = await guild.GetMemberAsync(buttonResult.Result.User.Id);
                    await channel
                        .SendMessageAsync(
                            $"Unfortunately {member.DisplayName} you can't afford this goat and the it has gone back to market!")
                        .ConfigureAwait(false);
                }
                else
                {
                    await buttonResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                    await buttonResult.Result.Message.DeleteAsync();
                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
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
                        command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = buttonResult.Result.User.Id;
                        command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                            (int) Math.Ceiling(10 * Math.Pow(1.05, goatToSpawn.Level - 1));
                        command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = goatToSpawn.FilePath;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    var fs = new FarmerService();
                    fs.DeductCreditsFromFarmer(buttonResult.Result.User.Id, goatToSpawn.Level - 1);

                    await channel.SendMessageAsync("Congrats " +
                                                   $"{guild.GetMemberAsync(buttonResult.Result.User.Id).Result.DisplayName} you purchased " +
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
