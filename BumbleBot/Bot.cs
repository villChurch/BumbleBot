using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Type = BumbleBot.Models.Type;

namespace BumbleBot
{
    public class Bot
    {
        private readonly DBUtils dbUtils = new DBUtils();
        private ulong goatSpawnChannelId = 762230405784272916;
        private int gpm; // goats per minute
        private int messageCount;
        private Timer timer;

        private DiscordClient Client { get; set; }
        private CommandsNextExtension Commands { get; set; }
        public InteractivityConfiguration Interactivity { get; private set; }
        private IServiceProvider Services { get; set; }

        private void StartTimer()
        {
            timer = new Timer();
            timer.Interval = 60000 * 5; // One Minute
            timer.Elapsed += ResetMPM;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void ResetMPM(object source, ElapsedEventArgs e) // calculate messages per minute
        {
            messageCount = 0;
            gpm = 0;
        }

        public async Task RunAsync()
        {
            var json = string.Empty;

            var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            using (var fs =
                File.OpenRead(path + "/config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
            {
                json = await sr.ReadToEndAsync().ConfigureAwait(false);
            }

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);


            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug
            };

            // var shardClient = new DiscordShardedClient(config);
            Client = new DiscordClient(config);
            Client.Ready += OnClientReady;
            Client.GuildAvailable += Client_GuildAvailable;
            Client.ClientErrored += Client_ClientError;
            Client.MessageCreated += Client_MessageCreated;

            Services = new ServiceCollection()
                .AddSingleton<AssholeService>()
                .AddSingleton<TriviaServices>()
                .AddTransient<GoatService>()
                .AddTransient<FarmerService>()
                .AddTransient<DairyService>()
                .BuildServiceProvider(true);

#pragma warning disable IDE0058 // Expression value is never used
            Client.UseInteractivity(new InteractivityConfiguration
            {
                // default pagination behaviour to just ignore the reactions
                PaginationBehaviour = PaginationBehaviour.WrapAround,

                // default timeout for other actions to 5 minutes
                Timeout = TimeSpan.FromMinutes(5)
            });
#pragma warning restore IDE0058 // Expression value is never used

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new[] {configJson.Prefix},
                EnableMentionPrefix = true,
                EnableDms = true,
                DmHelp = false,
                Services = Services
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.CommandExecuted += Commands_CommandExecuted;
            Commands.CommandErrored += Commands_CommandErrored;

            Commands.RegisterCommands(Assembly.GetExecutingAssembly());

            await Client.ConnectAsync();

            await Task.Delay(-1);
        }

        private Task OnClientReady(DiscordClient Client, ReadyEventArgs e)
        {
            Client.Logger.Log(LogLevel.Information, "BumbleBot", "Client is ready to process events.", DateTime.Now);
            StartTimer(); // start timer
            return Task.CompletedTask;
        }

        private Task Client_MessageCreated(DiscordClient Client, MessageCreateEventArgs e)
        {
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select stringResponse from config where paramName = ?paramName";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "spawnChannel";
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        goatSpawnChannelId = reader.GetUInt64("stringResponse");
            }

            if (!e.Author.IsBot && e.Channel.ParentId != 725504404995964948) //e.Guild.Id == guildId && 
            {
                messageCount++;
                if (messageCount > 10 && gpm <= 0)
                {
                    var random = new Random();
                    var number = random.Next(5);
                    if (number == 3 && AreChristmasSpawnsEnabled())
                        _ = SpawnChristmasGoat(e);
                    else if (random.Next(0, 100) == 69)
                        _ = SpawnSpecialGoat(e);
                    else
                        _ = SpawnGoat(e);
                    messageCount = 0;
                    gpm++;
                }
            }

            try
            {
                var goatService = new GoatService();
                var hasGoatLevelled = goatService.CheckExpAgainstNextLevel(e.Message.Author.Id, (decimal) 0.5);
                if (hasGoatLevelled.Item1)
                    Task.Run(async () =>
                        {
                            var channelList = await e.Guild.GetChannelsAsync();
                            if (channelList.Any(x => x.Id == 774294465942257715))
                            {
                                var channel = e.Guild.GetChannel(774294465942257715);
                                _ = await channel.SendMessageAsync($"{e.Author.Mention} {hasGoatLevelled.Item2}")
                                    .ConfigureAwait(false);
                            }
                        }
                    );

                var assholeMode = false;
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Select boolValue from config where paramName = ?paramName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?paramName", MySqlDbType.VarChar, 2550).Value = "assholeMode";
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                            assholeMode = reader.GetBoolean("boolValue");
                    reader.Close();
                }

                var mrStick = DiscordEmoji.FromName(Client, ":mrstick:");
                if (assholeMode && e.Message.Content.Equals(mrStick))
                {
                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Update config SET boolValue = ?boolValue where paramName = ?paramName";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?boolValue", MySqlDbType.Int16).Value = 0;
                        command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeMode";
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    var currentResponse = "";
                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Select stringResponse from config where paramName = ?paramName";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeResponse";
                        connection.Open();
                        var reader = command.ExecuteReader();
                        if (reader.HasRows)
                            while (reader.Read())
                                currentResponse = reader.GetString("stringResponse");
                        reader.Close();
                    }

                    if (!currentResponse.Equals("empty"))
                        Task.Run(async () =>
                        {
                            _ = await e.Channel.SendMessageAsync($"{currentResponse}").ConfigureAwait(false);
                        });
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        private async Task SpawnGoat(MessageCreateEventArgs e)
        {
            try
            {
                var channelList = await e.Guild.GetChannelsAsync();
                var result = channelList.FirstOrDefault(x => x.Id == goatSpawnChannelId);
                if (result != null)
                {
                    var rnd = new Random();
                    var breed = rnd.Next(0, 3);
                    var baseColour = rnd.Next(0, 5);
                    var randomGoat = new Goat();
                    randomGoat.baseColour = (BaseColour) Enum.Parse(typeof(BaseColour),
                        Enum.GetName(typeof(BaseColour), baseColour));
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

                    var interactivtiy = Client.GetInteractivity();

                    var goatMsg = await e.Guild.GetChannel(goatSpawnChannelId).SendFileAsync(
                            $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Kids/{goatImageUrl}",
                            embed: embed)
                        .ConfigureAwait(false);
                    var msg = await interactivtiy.WaitForMessageAsync(x =>
                        x.Channel == e.Guild.GetChannel(goatSpawnChannelId)
                        && x.Content.ToLower().Trim() == "purchase", TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                    await goatMsg.DeleteAsync();
                    var goatService = new GoatService();
                    if (msg.TimedOut)
                    {
                        await e.Guild.GetChannel(goatSpawnChannelId)
                            .SendMessageAsync($"No one decided to purchase {randomGoat.name}").ConfigureAwait(false);
                    }
                    else if (!goatService.CanGoatFitInBarn(msg.Result.Author.Id))
                    {
                        var member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                        await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync(
                                $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                            .ConfigureAwait(false);
                    }
                    else if (!goatService.CanFarmerAffordGoat(randomGoat.level - 1, msg.Result.Author.Id))
                    {
                        var member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                        await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync(
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

                            await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync("Congrats " +
                                    $"{e.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
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

        private Task Client_GuildAvailable(DiscordClient Client, GuildCreateEventArgs e)
        {
            Client.Logger.Log(LogLevel.Information, "BumbleBot", $"Guild available: {e.Guild.Name}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_ClientError(DiscordClient Client, ClientErrorEventArgs e)
        {
            Client.Logger.Log(LogLevel.Error, "BumbleBot",
                $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            e.Context.Client.Logger.Log(LogLevel.Information, "BumbleBot",
                $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            e.Context.Client.Logger.Log(LogLevel.Error, "BumbleBot",
                $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}",
                DateTime.Now);

            if (e.Exception is ChecksFailedException ex)
            {
                var failed = ex.FailedChecks;
                var test = failed.Any(x => x is HasEnoughCredits);
                if (test)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Lack of funds",
                        Description = "You do not have enough credits to perform this action",
                        Color = DiscordColor.Aquamarine
                    };
                    await e.Context.RespondAsync("", embed: embed);
                }
                else
                {
                    var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Access denied or run criteria not met",
                        Description = $"{emoji} You do not have the permissions required to execute this command, " +
                                      "or the pre-checks for this command have failed.",
                        Color = new DiscordColor(0xFF0000) // red
                    };
                    await e.Context.RespondAsync("", embed: embed);
                }
            }
            else if (e.Exception is CommandNotFoundException Cnfex)
            {
                if (e.Context.Message.Content.Contains("??") || e.Context.Message.Content.Contains("?!"))
                    return; // for when people do ?????!?....
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Command not found",
                    Description = "I do not know this command. See g?help for a list of commands I know.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync("", embed: embed);
            }
            else
            {
                Console.Out.WriteLine(e.Exception.Message);
                Console.Out.WriteLine(e.Exception.StackTrace);
            }
        }

        public async Task SpawnChristmasGoat(MessageCreateEventArgs e)
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

                var interactivtiy = Client.GetInteractivity();

                var goatMsg = await e.Guild.GetChannel(goatSpawnChannelId).SendFileAsync(
                        $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/{randomGoat.filePath}",
                        embed: embed)
                    .ConfigureAwait(false);
                var msg = await interactivtiy.WaitForMessageAsync(x =>
                    x.Channel == e.Guild.GetChannel(goatSpawnChannelId)
                    && x.Content.ToLower().Trim() == "purchase", TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                await goatMsg.DeleteAsync();
                var goatService = new GoatService();
                if (msg.TimedOut)
                {
                    await e.Guild.GetChannel(goatSpawnChannelId)
                        .SendMessageAsync($"No one decided to purchase {randomGoat.name}").ConfigureAwait(false);
                }
                else if (!goatService.CanGoatFitInBarn(msg.Result.Author.Id))
                {
                    var member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync(
                            $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanFarmerAffordGoat(randomGoat.level - 1, msg.Result.Author.Id))
                {
                    var member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync(
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

                        await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync("Congrats " +
                            $"{e.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
                            $"{randomGoat.name} for {(randomGoat.level - 1).ToString()} credits").ConfigureAwait(false);
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

        public async Task SpawnSpecialGoat(MessageCreateEventArgs e)
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

                var interactivtiy = Client.GetInteractivity();
                var goatMsg = await e.Guild.GetChannel(goatSpawnChannelId).SendFileAsync(
                    $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Special Variations" +
                    $"/{specialGoat.filePath}", embed: embed).ConfigureAwait(false);

                var msg = await interactivtiy.WaitForMessageAsync(x =>
                    x.Channel == e.Guild.GetChannel(goatSpawnChannelId)
                    && x.Content.ToLower().Trim() == "purchase", TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                await goatMsg.DeleteAsync();
                var goatService = new GoatService();
                if (msg.TimedOut)
                {
                    await e.Guild.GetChannel(goatSpawnChannelId)
                        .SendMessageAsync($"No one decided to purchase {specialGoat.name}").ConfigureAwait(false);
                }
                else if (!goatService.CanGoatFitInBarn(msg.Result.Author.Id))
                {
                    var member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync(
                            $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanFarmerAffordGoat(specialGoat.level - 1, msg.Result.Author.Id))
                {
                    var member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync(
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

                        await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync("Congrats " +
                                $"{e.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
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
    }
}