using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Type = BumbleBot.Models.Type;

namespace BumbleBot.Commands.Game
{
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class GameCommands : BaseCommandModule
    {
        private readonly DBUtils dbUtils = new DBUtils();
        private Timer equipTimer;
        private bool equipTimerrunning;

        public GameCommands(GoatService goatService, FarmerService farmerService)
        {
            this.goatService = goatService;
            this.farmerService = farmerService;
        }

        private FarmerService farmerService { get; }
        private GoatService goatService { get; }


        private void SetEquipTimer()
        {
            equipTimer = new Timer(240000);
            equipTimer.Elapsed += FinishTimer;
            equipTimer.Enabled = true;
            equipTimerrunning = true;
        }

        private void FinishTimer(object source, ElapsedEventArgs e)
        {
            equipTimerrunning = false;
            equipTimer.Stop();
            equipTimer.Dispose();
        }

        [Command("givec")]
        [Hidden]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task GiveCredits(CommandContext ctx, DiscordUser member, int credits)
        {
            farmerService.AddCreditsToFarmer(member.Id, credits);
            await ctx.Channel.SendMessageAsync($"{credits} have been given to {member.Mention}").ConfigureAwait(false);
        }

        [Command("spawn")]
        [Hidden]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task Spawn(CommandContext ctx)
        {
            await ctx.Message.DeleteAsync();
            var random = new Random();
            int number;
            if (AreChristmasSpawnsEnabled())
            {
                number = random.Next(5);
                if (number == 3)
                {
                    _ = SpawnChristmasGoat(ctx);
                    return;
                }
            }

            number = random.Next(100);
            //number = 69;
            if (number == 69)
                _ = SpawnSpecialGoat(ctx);
            else
                _ = SpawnRandomGoat(ctx);
        }

        [Command("daily")]
        [Description("Collect your daily reward")]
        public async Task CollectDaily(CommandContext ctx)
        {
            try
            {
                var uri = $"http://localhost:8080/daily/{ctx.User.Id}";
                var request = (HttpWebRequest) WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (var response = (HttpWebResponse) await request.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var jsonReader = new JsonTextReader(reader);
                    var serializer = new JsonSerializer();
                    var dailyResponse = serializer.Deserialize<DailyResponse>(jsonReader);
                    goatService.UpdateGoatImagesForKidsThatAreAdults(ctx.User.Id);
                    var interactivity = ctx.Client.GetInteractivity();
                    await ctx.Channel.SendMessageAsync(dailyResponse.message).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("create")]
        [Description("Create Your Character")]
        public async Task CreateCharacter(CommandContext ctx)
        {
            if (DoesUserHaveCharacter(ctx.Member.Id))
            {
                await ctx.Channel.SendMessageAsync("You already have an account").ConfigureAwait(false);
                return;
            }

            try
            {
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "INSERT INTO farmers (DiscordID) VALUES (?discordID)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync("Your character has now been created!").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("profile")]
        [Description("Shows your game profile")]
        public async Task ShowProfile(CommandContext ctx)
        {
            try
            {
                if (!DoesUserHaveCharacter(ctx.Member.Id))
                {
                    await ctx.Channel.SendMessageAsync("You do not have an account yet. Use gb?create to create one.")
                        .ConfigureAwait(false);
                    return;
                }

                var credits = 10;
                var barnSize = 10;
                var grazingSize = 0;
                decimal milkAmount = 0;
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "select * from farmers where DiscordID = ?discordID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        credits = reader.GetInt32("credits");
                        barnSize = reader.GetInt32("barnsize");
                        grazingSize = reader.GetInt32("grazesize");
                        milkAmount = reader.GetDecimal("milk");
                    }

                    reader.Close();
                }

                var numberOfGoats = 0;

                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "select COUNT(*) as amount from goats where ownerID = ?discordID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read()) numberOfGoats = reader.GetInt32("amount");
                    reader.Close();
                }

                var goat = goatService.GetEquippedGoat(ctx.User.Id);
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"Farmer {ctx.Member.DisplayName}'s Profile",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                    {
                        Url = ctx.Member.AvatarUrl
                    },
                    Color = DiscordColor.Aquamarine
                };
                embed.AddField("Credits", credits.ToString());
                embed.AddField("Herd Size", numberOfGoats.ToString());
                embed.AddField("Barn Space", barnSize.ToString(), true);
                embed.AddField("Pasture Available", $"Space For {grazingSize} Goats", true);
                embed.AddField("Milk in Storage", $"{milkAmount} lbs", true);
                if (goat.name != null)
                {
                    var url = "http://williamspires.com/";
                    embed.AddField("Goat in hand", goat.name);
                    embed.AddField("Goats level", goat.level.ToString(), true);
                    embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed).Replace("_", " "), true);
                    embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
                    embed.ImageUrl = url + Uri.EscapeUriString(goat.filePath); //.Replace(" ", "%20");
                }

                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        [Command("handle")]
        [Description("Select a Goat to Handle")]
        public async Task EquipGoat(CommandContext ctx)
        {
            try
            {
                var goats = new List<Goat>();
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Select * from goats where ownerID = ?ownerId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ctx.Member.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                        {
                            var goat = new Goat();
                            goat.id = reader.GetInt32("id");
                            goat.level = reader.GetInt32("level");
                            goat.name = reader.GetString("name");
                            goat.type = (Type) Enum.Parse(typeof(Type), reader.GetString("type"));
                            goat.breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed"));
                            goat.baseColour =
                                (BaseColour) Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                            goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                            goat.equiped = reader.GetBoolean("equipped");
                            goat.experience = reader.GetDecimal("experience");
                            goat.filePath = reader.GetString("imageLink");
                            goats.Add(goat);
                        }

                    reader.Close();
                }

                if (goats.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("You don't currently own any goats that can be handled")
                        .ConfigureAwait(false);
                }
                else
                {
                    var url = "http://williamspires.com/";
                    var pages = new List<Page>();
                    var interactivity = ctx.Client.GetInteractivity();
                    var backward = DiscordEmoji.FromName(ctx.Client, ":arrow_backward:");
                    var forward = DiscordEmoji.FromName(ctx.Client, ":arrow_forward:");
                    var equipBarn = DiscordEmoji.FromName(ctx.Client, ":1barn:");
                    foreach (var goat in goats)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{goat.id}",
                            ImageUrl = url + goat.filePath.Replace(" ", "%20")
                        };
                        embed.AddField("Name", goat.name, true);
                        embed.AddField("Level", goat.level.ToString(), true);
                        embed.AddField("Experience", goat.experience.ToString(), true);
                        var page = new Page
                        {
                            Embed = embed
                        };
                        pages.Add(page);
                    }

                    var pageCounter = 0;
                    var msg = await ctx.Channel.SendMessageAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                    SetEquipTimer();
                    while (equipTimerrunning)
                    {
                        await msg.CreateReactionAsync(backward).ConfigureAwait(false);
                        await msg.CreateReactionAsync(forward).ConfigureAwait(false);
                        await msg.CreateReactionAsync(equipBarn).ConfigureAwait(false);

                        var result = await interactivity.WaitForReactionAsync(x => x.Channel == ctx.Channel &&
                                x.User == ctx.User
                                && (x.Emoji == backward || x.Emoji == forward || x.Emoji == equipBarn),
                            TimeSpan.FromMinutes(4));

                        if (result.TimedOut)
                        {
                            equipTimerrunning = false;
                        }
                        else if (result.Result.Emoji == backward)
                        {
                            if (pageCounter - 1 < 0)
                                pageCounter = pages.Count - 1;
                            else
                                pageCounter--;
                            await msg.DeleteReactionAsync(backward, ctx.User).ConfigureAwait(false);
                            await msg.ModifyAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                        }
                        else if (result.Result.Emoji == forward)
                        {
                            if (pageCounter + 1 >= pages.Count)
                                pageCounter = 0;
                            else
                                pageCounter++;
                            await msg.DeleteReactionAsync(forward, ctx.User).ConfigureAwait(false);
                            await msg.ModifyAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                        }
                        else if (result.Result.Emoji == equipBarn)
                        {
                            if (!int.TryParse(pages[pageCounter].Embed.Title, out var id))
                            {
                                await ctx.Channel
                                    .SendMessageAsync("Something went wrong while trying to handle this goat.")
                                    .ConfigureAwait(false);
                                return;
                            }

                            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                            {
                                var query = "Update goats set equipped = 0 where ownerID = ?ownerId";
                                var command = new MySqlCommand(query, connection);
                                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ctx.User.Id;
                                connection.Open();
                                command.ExecuteNonQuery();
                            }

                            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                            {
                                var query = "Update goats set equipped = 1 where id = ?id";
                                var command = new MySqlCommand(query, connection);
                                command.Parameters.Add("?id", MySqlDbType.Int32).Value = id;
                                connection.Open();
                                command.ExecuteNonQuery();
                            }

                            await ctx.Channel.SendMessageAsync("Goat is now in hand.").ConfigureAwait(false);
                            //pages.RemoveAt(pageCounter);
                            await msg.DeleteAllReactionsAsync().ConfigureAwait(false);
                            equipTimerrunning = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        public async Task SpawnSpecialGoat(CommandContext ctx)
        {
            try
            {
                var specialGoat = GenerateSpecialGoat();
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{specialGoat.name} has become available, type purchase to add her to your herd",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = $"attachment://{specialGoat.filePath}"
                };
                embed.AddField("Cost", (specialGoat.level - 1).ToString());
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), specialGoat.baseColour));
                embed.AddField("Breed", Enum.GetName(typeof(Breed), specialGoat.breed).Replace("_", " "), true);
                embed.AddField("Level", (specialGoat.level - 1).ToString(), true);

                var interactivtiy = ctx.Client.GetInteractivity();
                var goatMsg = await ctx.Channel.SendFileAsync(
                    $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Special Variations" +
                    $"/{specialGoat.filePath}", embed: embed).ConfigureAwait(false);

                var msg = await interactivtiy.WaitForMessageAsync(x => x.Channel == ctx.Channel
                                                                       && x.Content.ToLower().Trim() == "purchase",
                    TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                await goatMsg.DeleteAsync();
                var goatService = new GoatService();
                if (msg.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync($"No one decided to purchase {specialGoat.name}")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanGoatFitInBarn(msg.Result.Author.Id))
                {
                    var member = await ctx.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanFarmerAffordGoat(specialGoat.level - 1, msg.Result.Author.Id))
                {
                    var member = await ctx.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"Unfortunately {member.DisplayName} you can't afford this goat and the it has gone back to market!")
                        .ConfigureAwait(false);
                }
                else
                {
                    await msg.Result.DeleteAsync();
                    try
                    {
                        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                        {
                            var query =
                                "INSERT INTO goats (level, name, type, breed, baseColour, ownerID, experience, imageLink) " +
                                "VALUES (?level, ?name, ?type, ?breed, ?baseColour, ?ownerID, ?exp, ?imageLink)";
                            var command = new MySqlCommand(query, connection);
                            command.Parameters.Add("?level", MySqlDbType.Int32).Value = specialGoat.level - 1;
                            command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = specialGoat.name;
                            command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                            command.Parameters.Add("?breed", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(Breed), specialGoat.breed);
                            command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(BaseColour), specialGoat.baseColour);
                            command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                            command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                                (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.level - 1));
                            command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value =
                                $"Goat_Images/Special Variations/{specialGoat.filePath}";
                            connection.Open();
                            command.ExecuteNonQuery();
                        }

                        var fs = new FarmerService();
                        fs.DeductCreditsFromFarmer(msg.Result.Author.Id, specialGoat.level - 1);

                        await ctx.Channel.SendMessageAsync("Congrats " +
                                                           $"{ctx.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
                                                           $"{specialGoat.name} for {(specialGoat.level - 1).ToString()} credits")
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine(ex.Message);
                        Console.Out.WriteLine(ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        private Goat GenerateSpecialGoat()
        {
            var random = new Random();
            var number = random.Next(0, 3);
            var goat = new Goat();

            if (number == 0)
            {
                goat.breed = Breed.Bumble;
                goat.filePath = "BumbleKid.png";
            }
            else if (number == 1)
            {
                goat.breed = Breed.Minx;
                goat.filePath = "MinxKid.png";
            }
            else
            {
                goat.breed = Breed.Zenyatta;
                goat.filePath = "ZenyattaKid.png";
            }

            goat.baseColour = BaseColour.Special;
            goat.level = RandomLevel.GetRandomLevel();
            goat.levelMulitplier = 1;
            goat.type = Type.Kid;
            goat.name = "Special Goat";

            return goat;
        }

        public async Task SpawnChristmasGoat(CommandContext ctx)
        {
            try
            {
                var randomGoat = new Goat();
                randomGoat.baseColour = BaseColour.Special;
                randomGoat.breed = Breed.Christmas;
                randomGoat.type = Type.Kid;
                randomGoat.levelMulitplier = 1;
                randomGoat.level = RandomLevel.GetRandomLevel();
                randomGoat.name = "Christmas Special";

                var goatImage = GetChristmasKidImage();
                randomGoat.filePath = $"Goat_Images/Special Variations/{goatImage}";
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{randomGoat.name} has become available, type purchase to add her to your herd",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = $"attachment://{goatImage}"
                };
                embed.AddField("Cost", (randomGoat.level - 1).ToString());
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), randomGoat.baseColour));
                embed.AddField("Breed", Enum.GetName(typeof(Breed), randomGoat.breed).Replace("_", " "), true);
                embed.AddField("Level", (randomGoat.level - 1).ToString(), true);

                var interactivtiy = ctx.Client.GetInteractivity();

                var goatMsg = await ctx.Channel.SendFileAsync(
                        $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/{randomGoat.filePath}",
                        embed: embed)
                    .ConfigureAwait(false);
                var msg = await interactivtiy.WaitForMessageAsync(x => x.Channel == ctx.Channel
                                                                       && x.Content.ToLower().Trim() == "purchase",
                    TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                await goatMsg.DeleteAsync();
                var goatService = new GoatService();
                if (msg.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync($"No one decided to purchase {randomGoat.name}")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanGoatFitInBarn(msg.Result.Author.Id))
                {
                    var member = await ctx.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanFarmerAffordGoat(randomGoat.level - 1, msg.Result.Author.Id))
                {
                    var member = await ctx.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"Unfortunately {member.DisplayName} you can't afford this goat and the it has gone back to market!")
                        .ConfigureAwait(false);
                }
                else
                {
                    await msg.Result.DeleteAsync();
                    try
                    {
                        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                        {
                            var query =
                                "INSERT INTO goats (level, name, type, breed, baseColour, ownerID, experience, imageLink) " +
                                "VALUES (?level, ?name, ?type, ?breed, ?baseColour, ?ownerID, ?exp, ?imageLink)";
                            var command = new MySqlCommand(query, connection);
                            command.Parameters.Add("?level", MySqlDbType.Int32).Value = randomGoat.level - 1;
                            command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = randomGoat.name;
                            command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                            command.Parameters.Add("?breed", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(Breed), randomGoat.breed);
                            command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(BaseColour), randomGoat.baseColour);
                            command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                            command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                                (int) Math.Ceiling(10 * Math.Pow(1.05, randomGoat.level - 1));
                            command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = $"{randomGoat.filePath}";
                            connection.Open();
                            command.ExecuteNonQuery();
                        }

                        var fs = new FarmerService();
                        fs.DeductCreditsFromFarmer(msg.Result.Author.Id, randomGoat.level - 1);

                        await ctx.Channel.SendMessageAsync("Congrats " +
                                                           $"{ctx.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
                                                           $"{randomGoat.name} for {(randomGoat.level - 1).ToString()} credits")
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine(ex.Message);
                        Console.Out.WriteLine(ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        private string GetChristmasKidImage()
        {
            var random = new Random();
            var number = random.Next(0, 3);
            if (number == 0)
                return "AngelLightsKid.png";
            if (number == 1)
                return "GrinchKid.png";
            return "SantaKid.png";
        }

        private bool AreChristmasSpawnsEnabled()
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

        public async Task SpawnRandomGoat(CommandContext ctx)
        {
            try
            {
                var rnd = new Random();
                var breed = rnd.Next(0, 3);
                var baseColour = rnd.Next(0, 5);
                var randomGoat = new Goat();
                randomGoat.baseColour =
                    (BaseColour) Enum.Parse(typeof(BaseColour), Enum.GetName(typeof(BaseColour), baseColour));
                randomGoat.breed = (Breed) Enum.Parse(typeof(Breed), Enum.GetName(typeof(Breed), breed));
                randomGoat.type = Type.Kid;
                randomGoat.level = RandomLevel.GetRandomLevel();
                randomGoat.levelMulitplier = 1;
                randomGoat.name = "Unregistered Goat";
                randomGoat.special = false;

                var goatImageUrl = GetKidImage(randomGoat.breed, randomGoat.baseColour);
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{randomGoat.name} has become available, type purchase to add her to your herd.",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = $"attachment://{goatImageUrl}"
                };
                embed.AddField("Cost", randomGoat.level - 1 + " credits");
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), randomGoat.baseColour));
                embed.AddField("Breed", Enum.GetName(typeof(Breed), randomGoat.breed).Replace("_", " "), true);
                embed.AddField("Level", (randomGoat.level - 1).ToString(), true);

                var interactivtiy = ctx.Client.GetInteractivity();

                var goatMsg = await ctx.Channel.SendFileAsync(
                        $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Kids/{goatImageUrl}",
                        embed: embed)
                    .ConfigureAwait(false);
                var msg = await interactivtiy.WaitForMessageAsync(x => x.Channel == ctx.Channel
                                                                       && x.Content.ToLower().Trim() == "purchase",
                    TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                await goatMsg.DeleteAsync();
                var goatService = new GoatService();
                if (msg.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync($"No one decided to purchase {randomGoat.name}")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanGoatFitInBarn(msg.Result.Author.Id))
                {
                    var member = await ctx.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanFarmerAffordGoat(randomGoat.level - 1, msg.Result.Author.Id))
                {
                    var member = await ctx.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"Unfortunately {member.DisplayName} you can't afford this goat and the it has gone back to market!")
                        .ConfigureAwait(false);
                }
                else
                {
                    await msg.Result.DeleteAsync();
                    try
                    {
                        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                        {
                            var query =
                                "INSERT INTO goats (level, name, type, breed, baseColour, ownerID, experience, imageLink) " +
                                "VALUES (?level, ?name, ?type, ?breed, ?baseColour, ?ownerID, ?exp, ?imageLink)";
                            var command = new MySqlCommand(query, connection);
                            command.Parameters.Add("?level", MySqlDbType.Int32).Value = randomGoat.level - 1;
                            command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = randomGoat.name;
                            command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                            command.Parameters.Add("?breed", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(Breed), randomGoat.breed);
                            command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(BaseColour), randomGoat.baseColour);
                            command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                            command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                                (int) Math.Ceiling(10 * Math.Pow(1.05, randomGoat.level - 1));
                            command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value =
                                $"Goat_Images/Kids/{goatImageUrl}";
                            connection.Open();
                            command.ExecuteNonQuery();
                        }

                        var fs = new FarmerService();
                        fs.DeductCreditsFromFarmer(msg.Result.Author.Id, randomGoat.level - 1);

                        await ctx.Channel.SendMessageAsync("Congrats " +
                                                           $"{ctx.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
                                                           $"{randomGoat.name} for {(randomGoat.level - 1).ToString()} credits")
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine(ex.Message);
                        Console.Out.WriteLine(ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
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

        private bool DoesUserHaveCharacter(ulong discordID)
        {
            var hasCharacter = false;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from farmers where DiscordID = ?discordID";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = discordID;
                connection.Open();
                var reader = command.ExecuteReader();
                hasCharacter = reader.HasRows;
                reader.Close();
            }

            return hasCharacter;
        }
    }
}