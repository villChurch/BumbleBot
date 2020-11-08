using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Linq;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Entities;

namespace BumbleBot.Commands.Game
{
    [Group("Milk")]
    [Hidden]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class MilkCommands : BaseCommandModule
    {
        private DBUtils dbUtils = new DBUtils();
        GoatService goatService { get; }

        public MilkCommands(GoatService goatService)
        {
            this.goatService = goatService;
        }

        [GroupCommand]
        public async Task MilkGoats(CommandContext ctx)
        {
            try
            {
                String uri = $"http://localhost:8080/milk/{ctx.User.Id}";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using(HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using(Stream stream = response.GetResponseStream())
                using(StreamReader reader = new StreamReader(stream))
                {
                    var jsonReader = new JsonTextReader(reader);
                    JsonSerializer serializer = new JsonSerializer();
                    MilkingResponse milkingResponse = serializer.Deserialize<MilkingResponse>(jsonReader);

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
                string uri = $"http://localhost:8080/milk/expiry/{ctx.User.Id}";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                List<ExpiryResponse> expiryDates = new List<ExpiryResponse>();
                using (HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                {
                    var jsonReader = new JsonTextReader(reader);
                    JsonSerializer serializer = new JsonSerializer();
                    expiryDates = serializer.Deserialize<List<ExpiryResponse>>(jsonReader);
                }

                if (expiryDates.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Expiry dates are shown in the following format YYYY-MM-dd");
                    expiryDates.ForEach((ExpiryResponse obj) => {
                        sb.AppendLine($"{obj.milk} lbs of milk will expire at {obj.expiryDate}");
                    });
                    var interactivity = ctx.Client.GetInteractivity();
                    var expiryPages = interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, expiryPages)
                        .ConfigureAwait(false);
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
                Farmer farmer = new Farmer();
                using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select * from farmers where DiscordID = ?discordID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.User.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            farmer.credits = reader.GetInt32("credits");
                            farmer.milk = reader.GetDecimal("milk");
                            farmer.discordID = reader.GetUInt64("DiscordID");
                        }
                    }
                    reader.Close();
                }
                if (farmer == null)
                {
                    await ctx.Channel.SendMessageAsync("You do not have a character setup yet").ConfigureAwait(false);
                }
                else if (farmer.milk <=0)
                {
                    await ctx.Channel.SendMessageAsync("You do not have any milk you can sell").ConfigureAwait(false);
                }
                else
                {
                    farmer.credits +=  (int)Math.Ceiling(farmer.milk * 5);
                    using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "Update farmers set milk = ?milk, credits = ?credits where DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?milk", MySqlDbType.Decimal).Value = 0;
                        command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.credits;
                        command.Parameters.Add("?discordId", MySqlDbType.VarChar, 40).Value = ctx.User.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    await ctx.Channel.SendMessageAsync($"Congratulations {ctx.User.Mention} you have sold {farmer.milk} for " +
                        $"{Math.Ceiling(farmer.milk * 5)} credits.").ConfigureAwait(false);
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