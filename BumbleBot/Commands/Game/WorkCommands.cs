using System.IO;
using System.Net;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Newtonsoft.Json;

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
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "update farmers set working = ?working where DiscordID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?working", true);
                command.Parameters.AddWithValue("?userId", ctx.User.Id);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }

            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
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
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
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
            
            using (var response = (HttpWebResponse) await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                var dailyResponse = await reader.ReadToEndAsync();
                await new DiscordMessageBuilder()
                    .WithReply(ctx.Message.Id, true)
                    .WithContent(dailyResponse)
                    .SendAsync(ctx.Channel);
            }
        }
    }
}