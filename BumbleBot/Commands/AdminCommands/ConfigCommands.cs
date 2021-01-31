using System;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.AdminCommands
{
    [Group("config")]
    [Hidden]
    public class ConfigCommands : BaseCommandModule
    {
        private readonly DbUtils dbUtils = new DbUtils();

        [Command("tailless")]
        [Aliases("ts")]
        [Description("Disable or enable tailless spawns")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetTaillessSpawnVariable(CommandContext ctx, bool enabled)
        {
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "update config SET boolValue = ?value where paramName = ?param";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?value", MySqlDbType.Int16).Value = enabled;
                command.Parameters.Add("?param", MySqlDbType.VarChar).Value = "taillessEnabled";
                connection.Open();
                command.ExecuteNonQuery();
            }

            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Tailless spawns have been {enabledOrDisabled}.").ConfigureAwait(false);
        }
        
        [Command("goatspawns")]
        [Aliases("gs", "gsc")]
        [Description("Sets the channel for goats to spawn in")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetGoatSpawnChannel(CommandContext ctx, DiscordChannel discordChannel)
        {
            try
            {
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Update config SET stringResponse = ?spawnChannel where paramName = ?paramName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?spawnChannel", MySqlDbType.VarChar).Value = discordChannel.Id;
                    command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "spawnChannel";
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync($"Goats will spawn in {discordChannel.Mention}")
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("asshole")]
        [Hidden]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task EnableAssholeMode(CommandContext ctx)
        {
            try
            {
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Update config SET boolValue = ?boolValue where paramName = ?paramName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?boolValue", MySqlDbType.Int16).Value = 1;
                    command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeMode";
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync("Parameter set").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("assholeresponse")]
        [Aliases("ar")]
        [Hidden]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task SetAssholeResponse(CommandContext ctx)
        {
            try
            {
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

                var interactivity = ctx.Client.GetInteractivity();
                await ctx.Channel.SendMessageAsync($"Current reponse is: {currentResponse} Please enter the new one")
                    .ConfigureAwait(false);
                var responseMsg = await interactivity.WaitForMessageAsync(
                    x => x.Author == ctx.Message.Author && x.Channel == ctx.Channel,
                    TimeSpan.FromMinutes(5));
                if (responseMsg.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("This command has timed out").ConfigureAwait(false);
                }
                else
                {
                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Update config set stringResponse = ?stringResponse where paramName = ?paramName";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?stringResponse", MySqlDbType.VarChar).Value =
                            responseMsg.Result.Content;
                        command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeResponse";
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    await ctx.Channel.SendMessageAsync("Response now updated").ConfigureAwait(false);
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