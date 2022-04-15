using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using Dapper;
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

         public (Goat, string) GenerateSpecialGoatToSpawnNew()
         {
             List<SpecialGoats> variations;
             using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
             {
                 connection.Open();
                 variations = connection.Query<SpecialGoats>("select * from specialgoats where enabled = 1 group by variation").ToList();
             }
             if (variations.Count < 1)
             {
                 return GenerateNormalGoatToSpawn();
             }
             var chosenSpecialGoat = variations[new Random().Next(variations.Count)];
             return GenerateSpecialGoatToSpawn(chosenSpecialGoat.kidFileLink, chosenSpecialGoat.variation);
         }
         public (Goat, string) GenerateSpecialGoatToSpawn(string goatFilePath, string variation)
         {
             var specialGoat = new Goat();
             specialGoat.Breed = (Breed) Enum.Parse(typeof(Breed), variation);
             specialGoat.BaseColour = BaseColour.Special;
             specialGoat.Level = new Random().Next(76, 100);
             specialGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
             specialGoat.Name = $"{variation} Goat";
             specialGoat.FilePath = goatFilePath;
             var filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{specialGoat.FilePath}";
             return (specialGoat, filePath);
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
