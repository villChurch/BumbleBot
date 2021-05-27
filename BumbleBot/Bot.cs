using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using BumbleBot.Attributes;
using BumbleBot.Commands;
using BumbleBot.Converter;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Type = BumbleBot.Models.Type;

namespace BumbleBot
{
    public class Bot
    {
        private readonly DbUtils dbUtils = new DbUtils();
        private ulong goatSpawnChannelId = 762230405784272916;
        private int gpm; // goats per minute
        private int messageCount;
        private Timer timer;

        private DiscordClient Client { get; set; }
        private CommandsNextExtension Commands { get; set; }
        public InteractivityConfiguration Interactivity { get; private set; }
        private IServiceProvider Services { get; set; }

        private readonly GoatSpawningService goatSpawningService = new();

        private void StartTimer()
        {
            timer = new Timer();
            timer.Interval = 60000 * 5; // One Minute
            timer.Elapsed += ResetMpm;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void ResetMpm(object source, ElapsedEventArgs e) // calculate messages per minute
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
                MinimumLogLevel = LogLevel.Debug,
                Intents = DiscordIntents.All
            };
            
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
                .AddSingleton<ReminderService>()
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

            var slash = Client.UseSlashCommands();
            slash.RegisterCommands<SlashHandle>();
            slash.SlashCommandErrored += SlashOnSlashCommandErrored;
            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.CommandExecuted += Commands_CommandExecuted;
            Commands.CommandErrored += Commands_CommandErrored;

            Commands.RegisterConverter(new ReminderTimeConverter());
            Commands.RegisterCommands(Assembly.GetExecutingAssembly());

            await Client.ConnectAsync();

            await Task.Delay(-1);
        }

        
        private async Task SlashOnSlashCommandErrored(SlashCommandsExtension sender, SlashCommandErrorEventArgs e)
        {
            e.Context.Client.Logger.Log(LogLevel.Error,
                "{Username} tried executing '{CommandName}' but it errored: {Type}: {Message}",
                e.Context.User.Username, e.Context.CommandName ?? "<unknown command>",
                e.Exception.GetType(), e.Exception.Message);

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
                    await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AddEmbed(embed)).ConfigureAwait(false);
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
                    await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AddEmbed(embed)).ConfigureAwait(false);
                }
            }
            else if (e.Exception is CommandNotFoundException)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Command not found",
                    Description = "I do not know this command. See g?help for a list of commands I know.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AddEmbed(embed)).ConfigureAwait(false);
            }
        }
        
        private Task OnClientReady(DiscordClient client, ReadyEventArgs e)
        {
            client.Logger.Log(LogLevel.Information, "Client is ready to process events");
            StartTimer(); // start timer
            ReminderService.StartReminderTimer(client);
            return Task.CompletedTask;
        }

        private Task Client_MessageCreated(DiscordClient client, MessageCreateEventArgs e)
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

            if (!e.Author.IsBot && e.Channel.ParentId != 725504404995964948)
            {
                messageCount++;
                if (messageCount > 10 && gpm <= 0)
                {
                    var random = new Random();
                    var number = random.Next(0, 5);
                    switch (number)
                    {
                        case 0 or 1 when goatSpawningService.AreSpringSpawnsEnabled():
                            var springGoatToSpawn = goatSpawningService.GenerateSpecialSpringGoatToSpawn();
                            _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(e, springGoatToSpawn.Item1, springGoatToSpawn.Item2, client));
                            break;
                        case 0 or 1 when goatSpawningService.AreDazzleSpawnsEnabled():
                            var bestGoat = goatSpawningService.GenerateBestestGoatToSpawn();
                            _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(e, bestGoat.Item1, bestGoat.Item2, client));
                            break;
                        case 0 or 1 when goatSpawningService.AreMemberSpawnsEnabled():
                            var memberGoatToSpawn = goatSpawningService.GenerateMemberSpecialGoatToSpawn();
                            _ = Task.Run(() =>
                                goatSpawningService.SpawnGoatFromGoatObject(e, memberGoatToSpawn.Item1, memberGoatToSpawn.Item2, client));
                            break;
                        case 1 when goatSpawningService.AreChristmasSpawnsEnabled():
                            _ = Task.Run(() =>SpawnChristmasGoat(e));
                            break;
                        case 2 when goatSpawningService.AreTaillessSpawnsEnabled():
                            _ = Task.Run(() => SpawnSpecialTaillessGoat(e));
                            break;
                        case 3 when goatSpawningService.AreValentinesSpawnsEnabled():
                            _ = Task.Run(() => SpawnValentinesGoat(e));
                            break;
                        case 4 when goatSpawningService.ArePaddysSpawnsEnabled():
                            var goat = goatSpawningService.GenerateSpecialPaddyGoatToSpawn();
                            _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(e, goat.Item1, goat.Item2, client));
                            break;
                        default:
                        {
                            _ = random.Next(0, 100) == 69 ? Task.Run(() => SpawnSpecialGoat(e)) : Task.Run(() => SpawnGoat(e));
                            break;
                        }
                    }
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

                var mrStick = DiscordEmoji.FromName(client, ":mrstick:");
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
                    randomGoat.BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour),
                        Enum.GetName(typeof(BaseColour), baseColour) ?? throw new InvalidOperationException());
                    randomGoat.Breed = (Breed) Enum.Parse(typeof(Breed), Enum.GetName(typeof(Breed), breed) ?? throw new InvalidOperationException());
                    randomGoat.Type = Type.Kid;
                    randomGoat.Level = RandomLevel.GetRandomLevel();
                    randomGoat.LevelMulitplier = 1;
                    randomGoat.Name = "Unregistered Goat";
                    randomGoat.Special = false;

                    var goatImageUrl = GetKidImage(randomGoat.Breed, randomGoat.BaseColour);
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"{randomGoat.Name} has become available, type purchase to add her to your herd.",
                        Color = DiscordColor.Aquamarine,
                        ImageUrl = $"attachment://{goatImageUrl}"
                    };
                    embed.AddField("Cost", randomGoat.Level - 1 + " credits");
                    embed.AddField("Colour", Enum.GetName(typeof(BaseColour), randomGoat.BaseColour));
                    embed.AddField("Breed", Enum.GetName(typeof(Breed), randomGoat.Breed)?.Replace("_", " "), true);
                    embed.AddField("Level", (randomGoat.Level - 1).ToString(), true);

                    var interactivtiy = Client.GetInteractivity();

                    var fileStream =
                        File.OpenRead(
                            $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Kids/{goatImageUrl}");
                    var goatMsg = await new DiscordMessageBuilder()
                        .WithEmbed(embed)
                        .WithFile(goatImageUrl, fileStream)
                        .SendAsync(e.Guild.GetChannel(goatSpawnChannelId));
                    var msg = await interactivtiy.WaitForMessageAsync(x =>
                        x.Channel == e.Guild.GetChannel(goatSpawnChannelId)
                        && x.Content.ToLower().Trim() == "purchase", TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                    await goatMsg.DeleteAsync();
                    var goatService = new GoatService();
                    if (msg.TimedOut)
                    {
                        await e.Guild.GetChannel(goatSpawnChannelId)
                            .SendMessageAsync($"No one decided to purchase {randomGoat.Name}").ConfigureAwait(false);
                    }
                    else if (!goatService.CanGoatsFitInBarn(msg.Result.Author.Id, 1))
                    {
                        var member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                        await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync(
                                $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                            .ConfigureAwait(false);
                    }
                    else if (!goatService.CanFarmerAffordGoat(randomGoat.Level - 1, msg.Result.Author.Id))
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
                                command.Parameters.Add("?level", MySqlDbType.Int32).Value = randomGoat.Level - 1;
                                command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = randomGoat.Name;
                                command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                                command.Parameters.Add("?breed", MySqlDbType.VarChar).Value =
                                    Enum.GetName(typeof(Breed), randomGoat.Breed);
                                command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                                    Enum.GetName(typeof(BaseColour), randomGoat.BaseColour);
                                command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                                command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                                    (int) Math.Ceiling(10 * Math.Pow(1.05, randomGoat.Level - 1));
                                command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value =
                                    $"Goat_Images/Kids/{goatImageUrl}";
                                connection.Open();
                                command.ExecuteNonQuery();
                            }

                            var fs = new FarmerService();
                            fs.DeductCreditsFromFarmer(msg.Result.Author.Id, randomGoat.Level - 1);

                            await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync("Congrats " +
                                    $"{e.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
                                    $"{randomGoat.Name} for {(randomGoat.Level - 1).ToString()} credits")
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

        private Task Client_GuildAvailable(DiscordClient client, GuildCreateEventArgs e)
        {
            client.Logger.Log(LogLevel.Information, "Guild available: {Name}", e.Guild.Name);
            return Task.CompletedTask;
        }

        private Task Client_ClientError(DiscordClient client, ClientErrorEventArgs e)
        {
            client.Logger.Log(LogLevel.Error,
                "Exception occured: {Type}: {Message}", e.Exception.GetType(), e.Exception.Message);

            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            e.Context.Client.Logger.Log(LogLevel.Information,
                "{Username} successfully executed '{Command}'", e.Context.User.Username, e.Command.QualifiedName);

            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            e.Context.Client.Logger.Log(LogLevel.Error,
                "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                e.Context.User.Username, e.Command?.QualifiedName ?? "<unknown command>",
                e.Exception.GetType(), e.Exception.Message);

            if (e.Exception is ChecksFailedException ex)
            {
                var failed = ex.FailedChecks;
                var test = failed.Any(x => x is HasEnoughCredits);
                var test2 = failed.Any(x => x is CooldownAttribute)
                            && ex.Command.Parent.Name.ToLower().Trim().Equals("stick");
                if (test)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Lack of funds",
                        Description = "You do not have enough credits to perform this action",
                        Color = DiscordColor.Aquamarine
                    };
                    await e.Context.RespondAsync("", embed: embed).ConfigureAwait(false);
                }
                else if (test2)
                {
                    await e.Context.RespondAsync($"Don't abuse Mr Stick").ConfigureAwait(false);
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
            else if (e.Exception is CommandNotFoundException)
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
                randomGoat.BaseColour = BaseColour.Special;
                randomGoat.Breed = Breed.Christmas;
                randomGoat.Type = Type.Kid;
                randomGoat.LevelMulitplier = 1;
                randomGoat.Level = RandomLevel.GetRandomLevel();
                randomGoat.Name = "Christmas Special";

                var goatImage = GetChristmasKidImage();
                randomGoat.FilePath = $"Goat_Images/Special Variations/{goatImage}";
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{randomGoat.Name} has become available, type purchase to add her to your herd",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = $"attachment://{goatImage}"
                };
                embed.AddField("Cost", (randomGoat.Level - 1).ToString());
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), randomGoat.BaseColour));
                embed.AddField("Breed", Enum.GetName(typeof(Breed), randomGoat.Breed)?.Replace("_", " "), true);
                embed.AddField("Level", (randomGoat.Level - 1).ToString(), true);

                var interactivtiy = Client.GetInteractivity();

                var fileStream =
                    File.OpenRead(
                        $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/{randomGoat.FilePath}");
                var goatMsg = await new DiscordMessageBuilder()
                    .WithEmbed(embed)
                    .WithFile(randomGoat.FilePath, fileStream)
                    .SendAsync(e.Guild.GetChannel(goatSpawnChannelId));
                var msg = await interactivtiy.WaitForMessageAsync(x =>
                    x.Channel == e.Guild.GetChannel(goatSpawnChannelId)
                    && x.Content.ToLower().Trim() == "purchase", TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                await goatMsg.DeleteAsync();
                var goatService = new GoatService();
                if (msg.TimedOut)
                {
                    await e.Guild.GetChannel(goatSpawnChannelId)
                        .SendMessageAsync($"No one decided to purchase {randomGoat.Name}").ConfigureAwait(false);
                }
                else if (!goatService.CanGoatsFitInBarn(msg.Result.Author.Id, 1))
                {
                    var member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync(
                            $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanFarmerAffordGoat(randomGoat.Level - 1, msg.Result.Author.Id))
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
                            command.Parameters.Add("?level", MySqlDbType.Int32).Value = randomGoat.Level - 1;
                            command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = randomGoat.Name;
                            command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                            command.Parameters.Add("?breed", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(Breed), randomGoat.Breed);
                            command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(BaseColour), randomGoat.BaseColour);
                            command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                            command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                                (int) Math.Ceiling(10 * Math.Pow(1.05, randomGoat.Level - 1));
                            command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = $"{randomGoat.FilePath}";
                            connection.Open();
                            command.ExecuteNonQuery();
                        }

                        var fs = new FarmerService();
                        fs.DeductCreditsFromFarmer(msg.Result.Author.Id, randomGoat.Level - 1);

                        await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync("Congrats " +
                            $"{e.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
                            $"{randomGoat.Name} for {(randomGoat.Level - 1).ToString()} credits").ConfigureAwait(false);
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

        public async Task SpawnSpecialGoat(MessageCreateEventArgs e)
        {
            try
            {
                var specialGoat = GenerateSpecialGoat();
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{specialGoat.Name} has become available, type purchase to add her to your herd",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = $"attachment://{specialGoat.FilePath}"
                };
                embed.AddField("Cost", (specialGoat.Level - 1).ToString());
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), specialGoat.BaseColour));
                embed.AddField("Breed", Enum.GetName(typeof(Breed), specialGoat.Breed)?.Replace("_", " "), true);
                embed.AddField("Level", (specialGoat.Level - 1).ToString(), true);

                var interactivtiy = Client.GetInteractivity();
                var fileStream = File.OpenRead(
                    $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Special Variations" +
                    $"/{specialGoat.FilePath}");
                var goatMsg = await new DiscordMessageBuilder()
                    .WithEmbed(embed)
                    .WithFile(specialGoat.FilePath, fileStream)
                    .SendAsync(e.Guild.GetChannel(goatSpawnChannelId));

                var msg = await interactivtiy.WaitForMessageAsync(x =>
                    x.Channel == e.Guild.GetChannel(goatSpawnChannelId)
                    && x.Content.ToLower().Trim() == "purchase", TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                await goatMsg.DeleteAsync();
                var goatService = new GoatService();
                if (msg.TimedOut)
                {
                    await e.Guild.GetChannel(goatSpawnChannelId)
                        .SendMessageAsync($"No one decided to purchase {specialGoat.Name}").ConfigureAwait(false);
                }
                else if (!goatService.CanGoatsFitInBarn(msg.Result.Author.Id, 1))
                {
                    var member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync(
                            $"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                        .ConfigureAwait(false);
                }
                else if (!goatService.CanFarmerAffordGoat(specialGoat.Level - 1, msg.Result.Author.Id))
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
                            command.Parameters.Add("?level", MySqlDbType.Int32).Value = specialGoat.Level - 1;
                            command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = specialGoat.Name;
                            command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                            command.Parameters.Add("?breed", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(Breed), specialGoat.Breed);
                            command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(BaseColour), specialGoat.BaseColour);
                            command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                            command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                                (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
                            command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value =
                                $"Goat_Images/Special Variations/{specialGoat.FilePath}";
                            connection.Open();
                            command.ExecuteNonQuery();
                        }

                        var fs = new FarmerService();
                        fs.DeductCreditsFromFarmer(msg.Result.Author.Id, specialGoat.Level - 1);

                        await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync("Congrats " +
                                $"{e.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
                                $"{specialGoat.Name} for {(specialGoat.Level - 1).ToString()} credits")
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
                goat.Breed = Breed.Bumble;
                goat.FilePath = "BumbleKid.png";
            }
            else if (number == 1)
            {
                goat.Breed = Breed.Minx;
                goat.FilePath = "MinxKid.png";
            }
            else
            {
                goat.Breed = Breed.Zenyatta;
                goat.FilePath = "ZenyattaKid.png";
            }

            goat.BaseColour = BaseColour.Special;
            goat.Level = RandomLevel.GetRandomLevel();
            goat.LevelMulitplier = 1;
            goat.Type = Type.Kid;
            goat.Name = "Special Goat";

            return goat;
        }
        
        private async Task SpawnValentinesGoat(MessageCreateEventArgs e)
        {
          try
          {
              var specialGoat = new Goat();
              specialGoat.Breed = Breed.Valentines;
              specialGoat.BaseColour = BaseColour.Special;
              specialGoat.Level = new Random().Next(76, 100);
              specialGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
              specialGoat.Name = "Valentines Goat";
              var valentineGoats = new List<String>()
              {
                  "CupidKid.png",
                  "HeartKid.png",
                  "RosesKid.png"  
              };
              var rnd = new Random();
              specialGoat.FilePath = valentineGoats[rnd.Next(0,3)];
              var embed = new DiscordEmbedBuilder
              {
                  Title = $"{specialGoat.Name} has become available, type purchase to add her to your herd",
                  Color = DiscordColor.Aquamarine,
                  ImageUrl = $"attachment://{specialGoat.FilePath}"
              };
              embed.AddField("Cost", (specialGoat.Level - 1).ToString());
              embed.AddField("Colour", Enum.GetName(typeof(BaseColour), specialGoat.BaseColour));
              embed.AddField("Breed", Enum.GetName(typeof(Breed), specialGoat.Breed)?.Replace("_", " "), true);
              embed.AddField("Level", (specialGoat.Level - 1).ToString(), true);

              var fileStream = File.OpenRead(
                  $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Valentine_Special_Variations/" +
                  $"/{specialGoat.FilePath}");
              var interactivity = Client.GetInteractivity();
              var goatMsg = await new DiscordMessageBuilder()
                  .WithEmbed(embed)
                  .WithFile(specialGoat.FilePath, fileStream)
                  .SendAsync(e.Guild.GetChannel(goatSpawnChannelId));

              var msg = await interactivity.WaitForMessageAsync(x => x.Channel == e.Guild.GetChannel(goatSpawnChannelId)
                                                                     && x.Content.ToLower().Trim() == "purchase",
                  TimeSpan.FromSeconds(45)).ConfigureAwait(false);
              await goatMsg.DeleteAsync();
              var goatService = new GoatService();
              if (msg.TimedOut)
              {
                  await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync($"No one decided to purchase {specialGoat.Name}")
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
              else if (!goatService.CanFarmerAffordGoat(specialGoat.Level - 1, msg.Result.Author.Id))
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
                  try
                  {
                      using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                      {
                          var query =
                              "INSERT INTO goats (level, name, type, breed, baseColour, ownerID, experience, imageLink) " +
                              "VALUES (?level, ?name, ?type, ?breed, ?baseColour, ?ownerID, ?exp, ?imageLink)";
                          var command = new MySqlCommand(query, connection);
                          command.Parameters.Add("?level", MySqlDbType.Int32).Value = specialGoat.Level - 1;
                          command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = specialGoat.Name;
                          command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                          command.Parameters.Add("?breed", MySqlDbType.VarChar).Value =
                              Enum.GetName(typeof(Breed), specialGoat.Breed);
                          command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                              Enum.GetName(typeof(BaseColour), specialGoat.BaseColour);
                          command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                          command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                              (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
                          command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value =
                              $"Goat_Images/Special Variations/{specialGoat.FilePath}";
                          connection.Open();
                          command.ExecuteNonQuery();
                      }

                      var fs = new FarmerService();
                      fs.DeductCreditsFromFarmer(msg.Result.Author.Id, specialGoat.Level - 1);

                      await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync("Congrats " +
                              $"{e.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
                              $"{specialGoat.Name} for {(specialGoat.Level - 1).ToString()} credits")
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

 
        private async Task SpawnSpecialTaillessGoat(MessageCreateEventArgs e)
        {
            try
            {
                var specialGoat = new Goat();
                specialGoat.Breed = Breed.Tailless;
                specialGoat.BaseColour = BaseColour.Special;
                specialGoat.Level = new Random().Next(76, 100);
                specialGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
                specialGoat.Name = "Tailless Goat";
                specialGoat.FilePath = "taillesskid.png";
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{specialGoat.Name} has become available, type purchase to add her to your herd",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = $"attachment://{specialGoat.FilePath}"
                };
                embed.AddField("Cost", (specialGoat.Level - 1).ToString());
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), specialGoat.BaseColour));
                embed.AddField("Breed", Enum.GetName(typeof(Breed), specialGoat.Breed)?.Replace("_", " "), true);
                embed.AddField("Level", (specialGoat.Level - 1).ToString(), true);

                var fileStream = File.OpenRead(
                    $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Special Variations" +
                    $"/{specialGoat.FilePath}");
                var interactivity = Client.GetInteractivity();
                var goatMsg = await new DiscordMessageBuilder()
                    .WithEmbed(embed)
                    .WithFile(specialGoat.FilePath, fileStream)
                    .SendAsync(e.Guild.GetChannel(goatSpawnChannelId));

                var msg = await interactivity.WaitForMessageAsync(x => x.Channel == e.Guild.GetChannel(goatSpawnChannelId)
                                                                       && x.Content.ToLower().Trim() == "purchase",
                    TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                await goatMsg.DeleteAsync();
                var goatService = new GoatService();
                if (msg.TimedOut)
                {
                    await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync($"No one decided to purchase {specialGoat.Name}")
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
                else if (!goatService.CanFarmerAffordGoat(specialGoat.Level - 1, msg.Result.Author.Id))
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
                    try
                    {
                        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                        {
                            var query =
                                "INSERT INTO goats (level, name, type, breed, baseColour, ownerID, experience, imageLink) " +
                                "VALUES (?level, ?name, ?type, ?breed, ?baseColour, ?ownerID, ?exp, ?imageLink)";
                            var command = new MySqlCommand(query, connection);
                            command.Parameters.Add("?level", MySqlDbType.Int32).Value = specialGoat.Level - 1;
                            command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = specialGoat.Name;
                            command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                            command.Parameters.Add("?breed", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(Breed), specialGoat.Breed);
                            command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value =
                                Enum.GetName(typeof(BaseColour), specialGoat.BaseColour);
                            command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                            command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                                (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
                            command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value =
                                $"Goat_Images/Special Variations/{specialGoat.FilePath}";
                            connection.Open();
                            command.ExecuteNonQuery();
                        }

                        var fs = new FarmerService();
                        fs.DeductCreditsFromFarmer(msg.Result.Author.Id, specialGoat.Level - 1);

                        await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync("Congrats " +
                                                           $"{e.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you purchased " +
                                                           $"{specialGoat.Name} for {(specialGoat.Level - 1).ToString()} credits")
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
    }
}