using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace BumbleBot.Commands.Game
{
    [Group("Milk")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class MilkCommands : BaseCommandModule
    {
        private readonly DbUtils dbUtils = new DbUtils();

        public MilkCommands(GoatService goatService)
        {
            this.GoatService = goatService;
        }

        private GoatService GoatService { get; }

        [GroupCommand]
        public async Task MilkGoats(CommandContext ctx)
        {
            try
            {
                var uri = $"http://localhost:8080/milk/{ctx.User.Id}";
                var request = (HttpWebRequest) WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (var response = (HttpWebResponse) await request.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var jsonReader = new JsonTextReader(reader);
                    var serializer = new JsonSerializer();
                    var milkingResponse = serializer.Deserialize<MilkingResponse>(jsonReader);
                    await new DiscordMessageBuilder()
                        .WithReply(ctx.Message.Id, true)
                        .WithContent(milkingResponse.Message)
                        .SendAsync(ctx.Channel);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("expiry")]
        public async Task CheckExpiry(CommandContext ctx)
        {
            try
            {
                var uri = $"http://localhost:8080/milk/expiry/{ctx.User.Id}";
                var request = (HttpWebRequest) WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                var expiryDates = new List<ExpiryResponse>();
                using (var response = (HttpWebResponse) await request.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var jsonReader = new JsonTextReader(reader);
                    var serializer = new JsonSerializer();
                    expiryDates = serializer.Deserialize<List<ExpiryResponse>>(jsonReader);
                }

                if (expiryDates.Count > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Expiry dates are shown in the following format YYYY-MM-dd");
                    expiryDates.ForEach(obj =>
                    {
                        sb.AppendLine($"{obj.Milk} lbs of milk will expire at {obj.ExpiryDate}");
                    });
                    var interactivity = ctx.Client.GetInteractivity();
                    var expiryPages =
                        interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                    _ = Task.Run(async () => await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, expiryPages)
                        .ConfigureAwait(false));
                }
                else
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have any milk").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("sell")]
        public async Task SellMilk(CommandContext ctx)
        {
            try
            {
                var farmer = new Farmer();
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Select * from farmers where DiscordID = ?discordID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.User.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                        {
                            farmer.Credits = reader.GetInt32("credits");
                            farmer.Milk = reader.GetDecimal("milk");
                            farmer.DiscordId = reader.GetUInt64("DiscordID");
                        }

                    reader.Close();
                }

                if (farmer == null)
                {
                    await ctx.Channel.SendMessageAsync("You do not have a character setup yet.").ConfigureAwait(false);
                }
                else if (farmer.Milk <= 0)
                {
                    await ctx.Channel.SendMessageAsync("You do not have any milk you can sell.").ConfigureAwait(false);
                }
                else
                {
                    farmer.Credits += (int) Math.Ceiling(farmer.Milk * 3);
                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Update farmers set milk = ?milk, credits = ?credits where DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?milk", MySqlDbType.Decimal).Value = 0;
                        command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.Credits;
                        command.Parameters.Add("?discordId", MySqlDbType.VarChar, 40).Value = ctx.User.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Delete from milkexpiry where DiscordID = ?discordID";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?discordID", MySqlDbType.VarChar).Value = ctx.User.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    await ctx.Channel.SendMessageAsync(
                        $"Congratulations {ctx.User.Mention} you have sold {farmer.Milk} lbs of milk for " +
                        $"{Math.Ceiling(farmer.Milk * 3)} credits.").ConfigureAwait(false);
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