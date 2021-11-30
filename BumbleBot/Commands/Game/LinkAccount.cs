using System.Data;
using System.Threading.Tasks;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.Game
{
    public class LinkAccount:BaseCommandModule
    {

        private DbUtils dbUtils = new();
        
        [Hidden]
        [Command("link")]
        public async Task LinkToWebAccount(CommandContext commandContext)
        {
            await commandContext.RespondAsync("Enter the 36 character code from the website.").ConfigureAwait(false);
            var dataTable = new DataTable();
            var validCode = false;
            var result = await commandContext.Message.GetNextMessageAsync();
            if (result.TimedOut)
            {
                await commandContext.Channel.SendMessageAsync("Interaction has timed out please re run to try again.")
                    .ConfigureAwait(false);
                return;
            }
            var response = result.Result.Content.ToLower();
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                var query = "select * from userslink where LOWER(code) = ?code";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?code", response);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    dataTable.Load(reader);
                    validCode = true;
                }
            }

            if (validCode == false)
            {
                await commandContext.Channel.SendMessageAsync("The code you entered could not be found.")
                    .ConfigureAwait(false);
                return;
            }

            if (dataTable.Rows.Count > 1)
            {
                commandContext.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but there was more than one result for code: {Code}",
                    commandContext.User.Username, commandContext.Command?.QualifiedName ?? "<unknown command>",
                    response);
                await commandContext.Channel
                    .SendMessageAsync(
                        "Something has gone wrong with the code, please regenerate a new one from the website and try again.")
                    .ConfigureAwait(false);
            }
            else if (dataTable.Rows.Count == 1)
            {
                var dr = dataTable.Rows[0];
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "UPDATE users SET DiscordID = ?discordId where id = ?userId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("?discordId", commandContext.User.Id);
                    command.Parameters.AddWithValue("?userId", dr["UserID"]);
                    connection.Open();
                    command.ExecuteNonQuery();
                    await connection.CloseAsync();
                }

                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "delete from userslink where LOWER(code) = ?code";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("?code", response);
                    connection.Open();
                    command.ExecuteNonQuery();
                    await connection.CloseAsync();
                }

                await commandContext.Channel.SendMessageAsync("Your discord account has now been linked.")
                    .ConfigureAwait(false);
            }
            else
            {
                commandContext.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but there was rows returned but no data in the data table",
                    commandContext.User.Username, commandContext.Command?.QualifiedName ?? "<unknown command>");
                await commandContext.Channel.SendMessageAsync(
                    "Something has gone wrong with the linking process, please contact an admin for assistance.");
            }
        }
    }
}