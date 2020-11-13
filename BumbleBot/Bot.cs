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
using Microsoft.Extensions.Logging;
using DSharpPlus.Interactivity.Extensions;
using System.Linq;

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
            timer.Interval = 60000 * 5; // One Minute
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
                MinimumLogLevel = LogLevel.Debug
            };

            // var shardClient = new DiscordShardedClient(config);
            Client = new DiscordClient(config);
            Client.Ready += OnClientReady;
            Client.GuildAvailable += this.Client_GuildAvailable;
            Client.ClientErrored += this.Client_ClientError;
            Client.MessageCreated += this.Client_MessageCreated;

            Services = new ServiceCollection()
                .AddSingleton<AssholeService>()
                .AddTransient<GoatService>()
                .AddTransient<FarmerService>()
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

        private Task OnClientReady(DiscordClient Client, ReadyEventArgs e)
        {
            Client.Logger.Log(LogLevel.Information, "BumbleBot", "Client is ready to process events.", DateTime.Now);
            StartTimer(); // start timer
            return Task.CompletedTask;
        }

        private Task Client_MessageCreated(DiscordClient Client, MessageCreateEventArgs e)
        {
            using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select stringResponse from config where paramName = ?paramName";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "spawnChannel";
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while(reader.Read())
                    {
                        goatSpawnChannelId = reader.GetUInt64("stringResponse");
                    }
                }
            }
            if (!e.Author.IsBot) //e.Guild.Id == guildId && 
            {
                messageCount++;
                if (messageCount > 10 && gpm <= 0)
                {
                    SpawnGoat(e);
                    messageCount = 0;
                    gpm++;
                }
            }
            try
            {
                GoatService goatService = new GoatService();
                bool hasGoatLevelled = goatService.CheckExpAgainstNextLevel(e.Message.Author.Id, (decimal)0.5);
                if (hasGoatLevelled)
                {
                    Task.Run(async () =>
                    {
                        var channelList = await e.Guild.GetChannelsAsync();
                        if (channelList.Any(x => x.Id == 774294465942257715))
                        {
                            var channel = e.Guild.GetChannel(774294465942257715);
                            _ = await channel.SendMessageAsync($"Congrats {e.Author.Mention} your current goat has just gained a level").ConfigureAwait(false);
                        }
                    }
                    );
                }

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
                DiscordEmoji mrStick = DiscordEmoji.FromName(Client, ":mrstick:");
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
            var channelList = await e.Guild.GetChannelsAsync();
            var result = channelList.FirstOrDefault(x => x.Id == goatSpawnChannelId);
            if (result != null)
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
                randomGoat.name = "Unregistered Goat";
                randomGoat.special = false;

                string goatImageUrl = GetKidImage(randomGoat.breed, randomGoat.baseColour);
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{randomGoat.name} has become available, type purchase to add her to your herd.",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = $"attachment://{goatImageUrl}"
                };
                embed.AddField("Colour", Enum.GetName(typeof(BaseColour), randomGoat.baseColour), false);
                embed.AddField("Breed", Enum.GetName(typeof(Breed), randomGoat.breed).Replace("_", " "), true);
                embed.AddField("Level", randomGoat.level.ToString(), true);

                var interactivtiy = Client.GetInteractivity();

                var goatMsg = await e.Guild.GetChannel(goatSpawnChannelId).SendFileAsync(
                    $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/Goat_Images/Kids/{goatImageUrl}", embed: embed)
                    .ConfigureAwait(false);
                var msg = await interactivtiy.WaitForMessageAsync(x => x.Channel == e.Guild.GetChannel(goatSpawnChannelId)
                && x.Content.ToLower().Trim() == "capture", TimeSpan.FromSeconds(45)).ConfigureAwait(false);
                await goatMsg.DeleteAsync();
                GoatService goatService = new GoatService();
                if (msg.TimedOut)
                {
                    await e.Guild.GetChannel(goatSpawnChannelId).SendMessageAsync($"No one decided to purchase {randomGoat.name}").ConfigureAwait(false);
                    return;
                }
                else if (!goatService.CanGoatFitInBarn(msg.Result.Author.Id))
                {
                    DiscordMember member = await e.Guild.GetMemberAsync(msg.Result.Author.Id);
                    await e.Channel.SendMessageAsync($"Unfortunately {member.DisplayName} your barn is full and the goat has gone back to market!")
                        .ConfigureAwait(false);
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
                            string query = "INSERT INTO goats (level, name, type, breed, baseColour, ownerID, experience, imageLink) " +
                                "VALUES (?level, ?name, ?type, ?breed, ?baseColour, ?ownerID, ?exp, ?imageLink)";
                            var command = new MySqlCommand(query, connection);
                            command.Parameters.Add("?level", MySqlDbType.Int32).Value = randomGoat.level;
                            command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = randomGoat.name;
                            command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                            command.Parameters.Add("?breed", MySqlDbType.VarChar).Value = Enum.GetName(typeof(Breed), randomGoat.breed);
                            command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value = Enum.GetName(typeof(BaseColour), randomGoat.baseColour);
                            command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                            command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                                (int)Math.Ceiling(10 * Math.Pow(1.05, (randomGoat.level - 1)));
                            command.Parameters.Add("?imageLink", MySqlDbType.VarChar).Value = $"Goat_Images/Kids/{goatImageUrl}";
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
        }

        private String GetKidImage(Breed breed, BaseColour baseColour)
        {
            string goat = "";
            string colour = "";
            if (breed.Equals(Breed.Nubian))
            {
                goat = "NBkid";
            }
            else if (breed.Equals(Breed.Nigerian_Dwarf))
            {
                goat = "NDkid";
            }
            else
            {
                goat = "LMkid";
            }

            if (baseColour.Equals(BaseColour.Black))
            {
                colour = "black";
            }
            else if (baseColour.Equals(BaseColour.Chocolate))
            {
                colour = "chocolate";
            }
            else if (baseColour.Equals(BaseColour.Gold))
            {
                colour = "gold";
            }
            else if (baseColour.Equals(BaseColour.Red))
            {
                colour = "red";
            }
            else
            {
                colour = "white";
            }

            return $"{goat}{colour}.png";
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

        private Task Client_GuildAvailable(DiscordClient Client, GuildCreateEventArgs e)
        {
            Client.Logger.Log(LogLevel.Information, "BumbleBot", $"Guild available: {e.Guild.Name}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_ClientError(DiscordClient Client, ClientErrorEventArgs e)
        {
            Client.Logger.Log(LogLevel.Error, "BumbleBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

            return Task.CompletedTask;
        }

        private Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            e.Context.Client.Logger.Log(LogLevel.Information, "BumbleBot", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);

            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            e.Context.Client.Logger.Log(LogLevel.Error, "BumbleBot", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);

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
