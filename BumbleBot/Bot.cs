using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using BumbleBot.Attributes;
using BumbleBot.Commands;
using BumbleBot.Converter;
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
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace BumbleBot
{
    public class Bot
    {
        private readonly DbUtils dbUtils = new DbUtils();
        private int gpm; // goats per minute
        private int messageCount;
        private Timer timer;

        private DiscordClient Client { get; set; }
        private CommandsNextExtension Commands { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public InteractivityConfiguration Interactivity { get; private set; }
        private IServiceProvider Services { get; set; }

        private void StartTimer()
        {
            timer = new Timer {Interval = 60000 * 1};
            // One Minute
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
            string json;

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
                MinimumLogLevel = LogLevel.Information,
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
                .AddTransient<GoatSpawningService>()
                .AddSingleton<ReminderService>()
                .AddSingleton<MaintenanceService>()
                .AddSingleton<PerkService>()
                .BuildServiceProvider(true);

#pragma warning disable IDE0058 // Expression value is never used
            Client.UseInteractivity(new InteractivityConfiguration
            {
                // default pagination behaviour to just ignore the reactions
                PaginationBehaviour = PaginationBehaviour.WrapAround,

                // default timeout for other actions to 5 minutes
                Timeout = TimeSpan.FromMinutes(5),
                ResponseBehavior = InteractionResponseBehavior.Ack,
                ButtonBehavior = ButtonPaginationBehavior.Disable,
                AckPaginationButtons = true,
                PaginationButtons = new PaginationButtons(),
                PaginationEmojis = new PaginationEmojis(),
                PaginationDeletion = PaginationDeletion.DeleteEmojis
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
            if (e.Exception is not SlashExecutionChecksFailedException)
            {
                e.Context.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{CommandName}' but it errored: {Type}: {Message}",
                    e.Context.User.Username, e.Context.CommandName ?? "<unknown command>",
                    e.Exception.GetType(), e.Exception.Message);
            }

            if (e.Exception is SlashExecutionChecksFailedException ex)
            {
                var failed = ex.FailedChecks;
                var workingTest = failed.Any(x => x is IsUserAvailableSlash);
                if (workingTest)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "You are Currently working",
                        Description =
                            $"Slash Command {e.Context.CommandName} cannot be executed at the moment as you are currently working",
                        Color = DiscordColor.Red
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
            _ = Task.Run(() =>
            {
                client.Logger.Log(LogLevel.Information, "Client is ready to process events");
                StartTimer(); // start timer
                ReminderService.StartReminderTimer(client);
            }
            );
            return Task.CompletedTask;
        }

        private Task Client_MessageCreated(DiscordClient client, MessageCreateEventArgs e)
        {
            _ = Task.Run(async () =>
                {
                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                    {
                        var query = "select stringResponse from config where paramName = ?paramName";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "spawnChannel";
                        connection.Open();
                        var reader = command.ExecuteReader();
                        if (reader.HasRows)
                            while (reader.Read())
                                reader.GetUInt64("stringResponse");
                    }

                    if (!e.Author.IsBot && e.Guild.Id == 565016829131751425 &&
                        e.Channel.ParentId != 725504404995964948) //798239862989127691)
                    {
                        messageCount++;
                        if (messageCount > 10 && gpm <= 0)
                        {
                            var goatSpawningService = new GoatSpawningService();
                            var random = new Random();
                            var number = random.Next(0, 5);
                            ulong goatSpawnChannelId = 774294357057732608;
                            var guildList = await e.Guild.GetChannelsAsync().ConfigureAwait(false);
                            var spawnChannel = e.Guild.GetChannel(goatSpawnChannelId);
                            switch (number)
                            {
                                case 0 or 1 when goatSpawningService.IsSpecialSpawnEnabled("springSpecials"):
                                    var springGoatToSpawn = goatSpawningService.GenerateSpecialSpringGoatToSpawn();
                                    _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(spawnChannel,
                                        e.Guild, springGoatToSpawn.Item1, springGoatToSpawn.Item2, client));
                                    break;
                                case 0 or 1 when goatSpawningService.IsSpecialSpawnEnabled("bestestGoat"):
                                    var bestGoat = goatSpawningService.GenerateBestestGoatToSpawn();
                                    _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(spawnChannel,
                                        e.Guild, bestGoat.Item1, bestGoat.Item2, client));
                                    break;
                                case 0 or 1 when goatSpawningService.IsSpecialSpawnEnabled("memberSpecials"):
                                    var memberGoatToSpawn = goatSpawningService.GenerateMemberSpecialGoatToSpawn();
                                    _ = Task.Run(() =>
                                        goatSpawningService.SpawnGoatFromGoatObject(spawnChannel, e.Guild,
                                            memberGoatToSpawn.Item1, memberGoatToSpawn.Item2, client));
                                    break;
                                case 1 when goatSpawningService.IsSpecialSpawnEnabled("dairySpecials"):
                                    var dairySpecial = goatSpawningService.GenerateSpecialDairyGoatToSpawn();
                                    _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(spawnChannel,
                                        e.Guild,
                                        dairySpecial.Item1, dairySpecial.Item2, client));
                                    break;
                                case 0 or 1 when goatSpawningService.IsSpecialSpawnEnabled("botBirthdayEnabled"):
                                    var goatToSpawn = goatSpawningService.GenerateBotBirthdaySpecialToSpawn();
                                    _ = Task.Run(() =>
                                        goatSpawningService.SpawnGoatFromGoatObject(spawnChannel, e.Guild,
                                            goatToSpawn.Item1, goatToSpawn.Item2, client));
                                    break;
                                case 0 or 1 when goatSpawningService.IsSpecialSpawnEnabled("novemberSpecials"):
                                    var novSpecialToSpawn = goatSpawningService.GenerateNovemberGoatToSpawn();
                                    _ = Task.Run(() =>
                                        goatSpawningService.SpawnGoatFromGoatObject(spawnChannel, e.Guild,
                                            novSpecialToSpawn.Item1, novSpecialToSpawn.Item2, client));
                                    break;
                                case 1 when goatSpawningService.IsSpecialSpawnEnabled("christmasSpecials"):
                                    var christmasGoat = goatSpawningService.GenerateChristmasSpecialToSpawn();
                                    _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(spawnChannel,
                                        e.Guild, christmasGoat.Item1, christmasGoat.Item2, client));
                                    break;
                                case 1 when goatSpawningService.IsSpecialSpawnEnabled("summerEnabled"):
                                    var summerGoat = goatSpawningService.GenerateSummerGoatToSpawn();
                                    _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(spawnChannel,
                                        e.Guild, summerGoat.Item1, summerGoat.Item2, client));
                                    break;
                                case 2 when goatSpawningService.IsSpecialSpawnEnabled("taillessEnabled"):
                                    var taillessGoat = goatSpawningService.GenerateTaillessSpecialGoatToSpawn();
                                    _ = Task.Run(() =>
                                        goatSpawningService.SpawnGoatFromGoatObject(spawnChannel, e.Guild,
                                            taillessGoat.Item1, taillessGoat.Item2, client));
                                    break;
                                case 2 when goatSpawningService.IsSpecialSpawnEnabled("buckSpecials"):
                                    var buckGoat = goatSpawningService.GenerateBuckSpecialToSpawn();
                                    _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(spawnChannel,
                                        e.Guild, buckGoat.Item1, buckGoat.Item2, client));
                                    break;
                                case 3 when goatSpawningService.IsSpecialSpawnEnabled("valentinesSpecials"):
                                    var valentinesGoat = goatSpawningService.GenerateValentinesGoatToSpawn();
                                    _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(spawnChannel,
                                        e.Guild, valentinesGoat.Item1, valentinesGoat.Item2, client));
                                    break;
                                case 2 when goatSpawningService.IsSpecialSpawnEnabled("halloweenEnabled"):
                                    var halloweenGoat = goatSpawningService.GenerateHalloweenSpecialToSpawn();
                                    _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(spawnChannel,
                                        e.Guild,
                                        halloweenGoat.Item1, halloweenGoat.Item2, client));
                                    break;
                                case 4 when goatSpawningService.IsSpecialSpawnEnabled("paddysSpecials"):
                                    var goat = goatSpawningService.GenerateSpecialPaddyGoatToSpawn();
                                    _ = Task.Run(() =>
                                        goatSpawningService.SpawnGoatFromGoatObject(spawnChannel, e.Guild, goat.Item1,
                                            goat.Item2, client));
                                    break;
                                default:
                                {
                                    _ = random.Next(0, 100) == 69
                                        ? Task.Run(() =>
                                            goatSpawningService.SpawnGoatFromGoatObject(spawnChannel, e.Guild,
                                                goatSpawningService.GenerateSpecialGoatToSpawn(), client))
                                        : Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(spawnChannel,
                                            e.Guild, goatSpawningService.GenerateNormalGoatToSpawn(), client));
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
                        var hasGoatLevelled = await goatService.CheckExpAgainstNextLevel(e.Message.Author.Id, (decimal) 0.5);
                        if (hasGoatLevelled.Item1)
                            await Task.Run(async () =>
                                {
                                    var channelList = await e.Guild.GetChannelsAsync();
                                    if (channelList.Any(x => x.Id == 774294465942257715))
                                    {
                                        var channel = e.Guild.GetChannel(774294465942257715);
                                        _ = await channel
                                            .SendMessageAsync($"{e.Author.Mention} {hasGoatLevelled.Item2}")
                                            .ConfigureAwait(false);
                                    }
                                }
                            );

                        var assholeMode = false;
                        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
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
                            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                            {
                                var query = "Update config SET boolValue = ?boolValue where paramName = ?paramName";
                                var command = new MySqlCommand(query, connection);
                                command.Parameters.Add("?boolValue", MySqlDbType.Int16).Value = 0;
                                command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeMode";
                                connection.Open();
                                command.ExecuteNonQuery();
                            }

                            var currentResponse = "";
                            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
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
                                await Task.Run(async () =>
                                {
                                    _ = await e.Channel.SendMessageAsync($"{currentResponse}")
                                        .ConfigureAwait(false);
                                });
                        }
                    }
                    catch (Exception ex)
                    {
                        await Console.Out.WriteLineAsync(ex.Message);
                        await Console.Out.WriteLineAsync(ex.StackTrace);
                    }
                });
            return Task.CompletedTask;
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
            if (e.Exception is not ChecksFailedException)
            {
                e.Context.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}, Stacktrace {StackTrace}",
                    e.Context.User.Username, e.Command?.QualifiedName ?? "<unknown command>",
                    e.Exception.GetType(), e.Exception.Message, e.Exception.StackTrace);
            }

            if (e.Exception is ChecksFailedException ex)
            {
                var failed = ex.FailedChecks;
                var test = failed.Any(x => x is HasEnoughCredits);
                var test2 = failed.Any(x => x is CooldownAttribute)
                            && ex.Command.Parent.Name.ToLower().Trim().Equals("stick");
                var workingTest = failed.Any(x => x is IsUserAvailable);
                if (test)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Lack of funds",
                        Description = "You do not have enough credits to perform this action",
                        Color = DiscordColor.Aquamarine
                    };
                    await e.Context.RespondAsync(embed).ConfigureAwait(false);
                }
                else if (test2)
                {
                    await e.Context.RespondAsync($"Don't abuse Mr Stick").ConfigureAwait(false);
                }
                else if (workingTest)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "You are Currently working",
                        Description =
                            $"Command {e.Context.Command.QualifiedName} cannot be executed at the moment as you are currently working",
                        Color = DiscordColor.Red
                    };
                    await e.Context.RespondAsync(embed).ConfigureAwait(false);
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
        }
    }
}