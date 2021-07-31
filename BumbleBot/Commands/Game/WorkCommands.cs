using System.Net;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace BumbleBot.Commands.Game
{
    [Group("work")]
    public class WorkCommands : BaseCommandModule
    {
        private DbUtils dbUtils = new();

        [Command("start")]
        [IsUserAvailable]
        [Description("Sends your character to work")]
        public async Task StartWorking(CommandContext ctx)
        {
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "update farmers set working = ?working where DiscordID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?working", true);
                command.Parameters.AddWithValue("?userId", ctx.User.Id);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }

            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "insert into working (farmerid) values (?farmerid)";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?farmerid", ctx.User.Id);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }

            await ctx.RespondAsync(
                    $"You have now started work. To stop working run {Formatter.InlineCode("g?work stop")}.")
                .ConfigureAwait(false);
        }

        [Command("stop")]
        [Description("Returns your character from work")]
        public async Task StopWorking(CommandContext ctx)
        {
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "update farmers set working = ?working where DiscordID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?working", false);
                command.Parameters.AddWithValue("?userId", ctx.User.Id);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
            var uri = $"http://localhost:8080/work/stop/{ctx.User.Id}";
            var request = (HttpWebRequest) WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            
            var response = (HttpWebResponse) await request.GetResponseAsync();
            ctx.Client.Logger.Log(LogLevel.Information,
                "{Username} stopped working and got the following status code {Response}",
                ctx.User.Username, response.StatusCode);
        }
    }
}