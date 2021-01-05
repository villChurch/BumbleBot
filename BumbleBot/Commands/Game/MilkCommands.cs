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
        private readonly DBUtils dbUtils = new DBUtils();

        public MilkCommands(GoatService goatService)
        {
            this.goatService = goatService;
        }

        private GoatService goatService { get; }

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

                    await ctx.Channel.SendMessageAsync(milkingResponse.message).ConfigureAwait(false);
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
                        sb.AppendLine($"{obj.milk} lbs of milk will expire at {obj.expiryDate}");
                    });
                    var interactivity = ctx.Client.GetInteractivity();
                    var expiryPages =
                        interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, expiryPages)
                        .ConfigureAwait(false);
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
                            farmer.credits = reader.GetInt32("credits");
                            farmer.milk = reader.GetDecimal("milk");
                            farmer.discordID = reader.GetUInt64("DiscordID");
                        }

                    reader.Close();
                }

                if (farmer == null)
                {
                    await ctx.Channel.SendMessageAsync("You do not have a character setup yet.").ConfigureAwait(false);
                }
                else if (farmer.milk <= 0)
                {
                    await ctx.Channel.SendMessageAsync("You do not have any milk you can sell.").ConfigureAwait(false);
                }
                else
                {
                    farmer.credits += (int) Math.Ceiling(farmer.milk * 3);
                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Update farmers set milk = ?milk, credits = ?credits where DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?milk", MySqlDbType.Decimal).Value = 0;
                        command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.credits;
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
                        $"Congratulations {ctx.User.Mention} you have sold {farmer.milk} lbs of milk for " +
                        $"{Math.Ceiling(farmer.milk * 3)} credits.").ConfigureAwait(false);
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