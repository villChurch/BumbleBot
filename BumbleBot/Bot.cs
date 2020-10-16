using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using BumbleBot.Models;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using BumbleBot.Services;

namespace BumbleBot
{
    public class Bot
    {
        private Timer timer;
        private int messageCount;
        private int gpm = 0; // goats per minute
        private ulong guildId = 761698823726039120;
        private ulong goatSpawnChannelId = 762230405784272916;
        private DiscordClient Client { get; set; }
        private CommandsNextExtension Commands { get; set; }
        public InteractivityConfiguration Interactivity { get; private set; }
        DBUtils dbUtils = new DBUtils();
        private IServiceProvider Services { get; set; }

        public Bot()
        {
        }

        private void StartTimer()
        {
            timer = new Timer();
            timer.Interval = 60000; // One Minute
            timer.Elapsed += ResetMPM;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void ResetMPM(Object source, ElapsedEventArgs e) // calculate messages per minute
        {
            messageCount = 0;
            gpm = 0;
        }

        public async Task RunAsync()
        {

            var json = string.Empty;

            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            using (var fs =
                File.OpenRead(path + "/config.json"))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync().ConfigureAwait(false);

            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);


            var config = new DiscordConfiguration
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
            };

            // var shardClient = new DiscordShardedClient(config);
            Client = new DiscordClient(config);

            Client.Ready += OnClientReady;
            Client.GuildAvailable += this.Client_GuildAvailable;
            Client.ClientErrored += this.Client_ClientError;
            Client.MessageCreated += this.Client_MessageCreated;

#pragma warning disable CS1702 // Assuming assembly reference matches identity
            Services = new ServiceCollection()
                .AddSingleton<AssholeService>()
                .BuildServiceProvider(true);
#pragma warning restore CS1702 // Assuming assembly reference matches identity

            // let's enable interactivity, and set default options
#pragma warning disable IDE0058 // Expression value is never used
            Client.UseInteractivity(new InteractivityConfiguration
            {
                // default pagination behaviour to just ignore the reactions
                PaginationBehaviour = PaginationBehaviour.WrapAround,

                // default timeout for other actions to 5 minutes
                Timeout = TimeSpan.FromMinutes(5)
            });
#pragma warning restore IDE0058 // Expression value is never used

            CommandsNextConfiguration commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { configJson.Prefix },
                EnableMentionPrefix = true,
                EnableDms = false,
                DmHelp = false,
                Services = Services
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            Commands.CommandExecuted += this.Commands_CommandExecuted;
            Commands.CommandErrored += this.Commands_CommandErrored;

            Commands.RegisterCommands(Assembly.GetExecutingAssembly());

            await Client.ConnectAsync();

            await Task.Delay(-1);
        }

        private Task OnClientReady(ReadyEventArgs e)
        {
            // let's log the fact that this event occured
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "BumbleBot", "Client is ready to process events.", DateTime.Now);
            StartTimer(); // start timer
            return Task.CompletedTask;
        }

        private Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (e.Guild.Id == guildId && !e.Author.IsBot)
            {
                messageCount++;
                if (messageCount > 10 && gpm <= 5)
                {
                    SpawnGoat(e);
                    messageCount = 0;
                    gpm++;
                }
            }
            try
            {
                bool assholeMode = false;
                using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select boolValue from config where paramName = ?paramName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?paramName", MySqlDbType.VarChar, 2550).Value = "assholeMode";
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            assholeMode = reader.GetBoolean("boolValue");
                        }
                    }
                    reader.Close();
                }
                DiscordEmoji mrStick = DiscordEmoji.FromName(e.Client, ":mrstick:");
                if (assholeMode && e.Message.Content.Equals(mrStick))
                {
                    using(MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "Update config SET boolValue = ?boolValue where paramName = ?paramName";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?boolValue", MySqlDbType.Int16).Value = 0;
                        command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeMode";
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    string currentResponse = "";
                    using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "Select stringResponse from config where paramName = ?paramName";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeResponse";
                        connection.Open();
                        var reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                currentResponse = reader.GetString("stringResponse");
                            }
                        }
                        reader.Close();
                    }
                    if (!currentResponse.Equals("empty"))
                    {
                        Task.Run(async () =>
                        {
                            _ = await e.Channel.SendMessageAsync($"{currentResponse}").ConfigureAwait(false);
                        });
                    }
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

        private async void SpawnGoat(MessageCreateEventArgs e)
        {
            Random rnd = new Random();
            int breed = rnd.Next(0, 2);
            int baseColour = rnd.Next(0, 4);
            var randomGoat = new Goat();
            randomGoat.baseColour = (BaseColour)Enum.Parse(typeof(BaseColour), Enum.GetName(typeof(BaseColour), baseColour));
            randomGoat.breed = (Breed)Enum.Parse(typeof(Breed), Enum.GetName(typeof(Breed), breed));
            randomGoat.type = Models.Type.Kid;
            randomGoat.level = RandomLevel.GetRandomLevel();
            randomGoat.levelMulitplier = 1;
            randomGoat.name = "Goaty McGoatFace";
            randomGoat.special = false;

            var embed = new DiscordEmbedBuilder
            {
                Title = $"{randomGoat.name} has spawned, type capture to capture her",
                Color = DiscordColor.Aquamarine
            };
            embed.AddField("Colour", Enum.GetName(typeof(BaseColour), randomGoat.baseColour), false);
            embed.AddField("Breed", Enum.GetName(typeof(Breed), randomGoat.breed).Replace("_", " "), true);
            embed.AddField("Level", randomGoat.level.ToString(), true);

            var interactivtiy = e.Client.GetInteractivity();

            var goatMsg = await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync(embed: embed).ConfigureAwait(false);
            var msg = await interactivtiy.WaitForMessageAsync(x => x.Channel == e.Guild.GetChannel(goatSpawnChannelId)
            && x.Content.ToLower().Trim() == "capture", TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            await goatMsg.DeleteAsync();
            if (msg.TimedOut)
            {
                await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync($"No one managed to catch {randomGoat.name}").ConfigureAwait(false);
                return;
            }
            else
            {
                if (!DoesUserHaveCharacter(msg.Result.Author.Id))
                {
                    using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "INSERT INTO farmers (DiscordID) VALUES (?discordID)";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = msg.Result.Author.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }
                await msg.Result.DeleteAsync();
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "INSERT INTO goats (level, name, type, breed, baseColour, ownerID) " +
                            "VALUES (?level, ?name, ?type, ?breed, ?baseColour, ?ownerID)";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?level", MySqlDbType.Int32).Value = randomGoat.level;
                        command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = randomGoat.name;
                        command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                        command.Parameters.Add("?breed", MySqlDbType.VarChar).Value = Enum.GetName(typeof(Breed), randomGoat.breed);
                        command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value = Enum.GetName(typeof(BaseColour), randomGoat.baseColour);
                        command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync($"Congrats " +
                        $"{e.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you caught " +
                        $"{randomGoat.name}").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.Message);
                    Console.Out.WriteLine(ex.StackTrace);
                }
            }
        }

        private bool DoesUserHaveCharacter(ulong discordID)
        {
            bool hasCharacter = false;
            using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select * from farmers where DiscordID = ?discordID";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = discordID;
                connection.Open();
                var reader = command.ExecuteReader();
                hasCharacter = reader.HasRows;
                reader.Close();
            }
            return hasCharacter;
        }

        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "BumbleBot", $"Guild available: {e.Guild.Name}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_ClientError(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "BumbleBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "BumbleBot", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "BumbleBot", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

            if (e.Exception is ChecksFailedException ex)
            {
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Access denied",
                    Description = $"{emoji} You do not have the permissions required to execute this command.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync("", embed: embed);
            }
            else if (e.Exception is CommandNotFoundException Cnfex)
            {
                if (e.Context.Message.Content.Contains("??") || e.Context.Message.Content.Contains("?!"))
                {
                    return; // for when people do ?????!?....
                }
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Command not found",
                    Description = $"I do not know this command. See g?help for a list of commands I know.",
                    Color = new DiscordColor(0xFF0000) // red
                };
                await e.Context.RespondAsync("", embed: embed);
            }
        }
    }
}
