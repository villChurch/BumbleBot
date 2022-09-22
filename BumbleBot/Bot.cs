using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using BumbleBot.Attributes;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.CommandsNext.Exceptions;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.EventHandling;
using DisCatSharp.Interactivity.Extensions;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.EventArgs;
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
        private int goatsSinceSpecial = 0; // goats since special
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
                .AddTransient<GoatSpawningService>()
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
                ServiceProvider = Services
            };
            var applicationCommandsConfiguration = new ApplicationCommandsConfiguration
            {
                EnableDefaultHelp = true,
                DebugStartup = true,
                ServiceProvider = Services
            };
            var applicationCommands = Client.UseApplicationCommands(applicationCommandsConfiguration);
            applicationCommands.ApplicationCommandsModuleStartupFinished += Bot_ApplicationCommandsModuleStartupFinished;
            applicationCommands.GlobalApplicationCommandsRegistered += (sender, args) =>
            {
                sender.Client.Logger.Log(LogLevel.Debug, "Number of global commands registered {command}",
                    sender.GlobalCommands.Count);
                return Task.CompletedTask;
            };
            applicationCommands.GuildApplicationCommandsRegistered += (sender, args) =>
            {
                sender.Client.Logger.Log(LogLevel.Debug, "Number of guild commands registered {command} in guild {guild}",
                    sender.GuildCommands.Count,
                    args.GuildId);
                return Task.CompletedTask;
            };
            applicationCommands.SlashCommandErrored += SlashOnSlashCommandErrored;
            Client.GuildDownloadCompleted += (client, gdcEventArgs) => ClientOnGuildDownloadCompleted(client, gdcEventArgs, applicationCommands);
            Client.GuildMemberAdded += ClientGuildMemberAdded;
            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.CommandExecuted += Commands_CommandExecuted;
            Commands.CommandErrored += Commands_CommandErrored;
            
            Commands.RegisterCommands(Assembly.GetExecutingAssembly());

            await Client.ConnectAsync();

            await Task.Delay(-1);
        }

        private Task ClientGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                var guild = e.Guild;
                var welcomeUtilities = new WelcomeUtilities();
                if (welcomeUtilities.HasWelcomeMessage(guild))
                {
                    var channelId = welcomeUtilities.ReturnChannelId(guild);
                    var channel = await sender.GetChannelAsync(Convert.ToUInt64(channelId));
                    await channel.SendMessageAsync(welcomeUtilities.ReturnCompletedWelcomeMessage(guild, e.Member));
                }
            });
            return Task.CompletedTask;
        }

        private Task ClientOnGuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e, ApplicationCommandsExtension applicationCommands)
        {
            _ = Task.Run(async () =>
            {
                var appCommandModule = typeof(ApplicationCommandsModule);
                var slashCommands = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(t => appCommandModule.IsAssignableFrom(t) && !t.IsNested).ToList();
                sender.Logger.Log(LogLevel.Information, "Bot is in {NumberOfGuilds} guilds",
                    e.Guilds.Count);
                foreach (var command in slashCommands)
                {
                    foreach (var guildId in e.Guilds.Keys)
                    {
                        applicationCommands.RegisterGuildCommands(command, guildId);
                    }
                }

                await applicationCommands.RefreshCommandsAsync();
            });
            return Task.CompletedTask;
        }

        private Task Bot_ApplicationCommandsModuleStartupFinished(ApplicationCommandsExtension sender, ApplicationCommandsModuleStartupFinishedEventArgs e)
        {
            Console.WriteLine($"Application commands module has finished the startup.");
            var guild_cmd_count = 0;
            foreach (var cmd in e.RegisteredGuildCommands)
            {
                guild_cmd_count += cmd.Value.Select(x => x.Name).Distinct().Count();
            }
            Console.WriteLine($"Stats: \n" +
                              $" - Found {e.GuildsWithoutScope.Count} guilds without the applications.commands scope\n" +
                              $" - Registered {e.RegisteredGlobalCommands.Count} global commands\n" +
                              $" - Registered {guild_cmd_count} commands on {e.RegisteredGuildCommands.Count} guilds."
            );
            return Task.CompletedTask;
        }

        private async Task SlashOnSlashCommandErrored(ApplicationCommandsExtension sender, SlashCommandErrorEventArgs e)
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
            client.Logger.Log(LogLevel.Information, "Client is ready to process events");
            StartTimer(); // start timer
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
                        if (messageCount > 10)
                        {
                            var goatSpawningService = new GoatSpawningService();
                            var random = new Random();
                            var number = goatsSinceSpecial >= 5 ? 1 : random.Next(0, 5);
                            ulong goatSpawnChannelId = 774294357057732608;
                            await e.Guild.GetChannelsAsync();
                            var spawnChannel = e.Guild.GetChannel(goatSpawnChannelId);
                            switch (number)
                            {
                                case 1:
                                    var goat = goatSpawningService.GenerateSpecialGoatToSpawnNew();
                                    goatsSinceSpecial = 0;
                                    _ = Task.Run(() =>
                                        goatSpawningService.SpawnGoatFromGoatObject(spawnChannel, e.Guild, goat.Item1,
                                            goat.Item2, client));
                                    break;
                                default:
                                {
                                    goatsSinceSpecial++;
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