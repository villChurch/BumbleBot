using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.GifsAndPhotos
{
    [Group("ana")]
    public class Ana : BaseCommandModule
    {
        private readonly DBUtils dBUtils = new DBUtils();

        [GroupCommand]
        public async Task ShowRandomanaPhoto(CommandContext ctx)
        {
            try
            {
                List<string> anaLinks = new List<string>();
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select anaLink from ana";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        anaLinks.Add(reader.GetString("anaLink"));
                    }
                    reader.Close();
                }

                if (anaLinks.Count < 0)
                {
                    await ctx.Channel.SendMessageAsync($"Currently there are no kid pictures! Run {Formatter.InlineCode("!ana add")}" +
                        $" to add one").ConfigureAwait(false);
                }
                else
                {
                    Random rnd = new Random();
                    int gifToShow = rnd.Next(0, anaLinks.Count);

                    await ctx.Channel.SendMessageAsync(anaLinks.ElementAt(gifToShow)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("add")]
        [OwnerOrPermission(Permissions.Administrator)]
        [Description("starts a dialogue to add a ana picture or gif")]
        public async Task AddanaGifOrPhoto(CommandContext ctx)
        {
            try
            {
                string gifLink = "";
                var interactivity = ctx.Client.GetInteractivity();

                await ctx.Channel.SendMessageAsync("Please enter the link for this ana picture/gif").ConfigureAwait(false);
                var linkResponse = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.Message.Author,
                    TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                if (linkResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync($"{ctx.Command.Name} command has timed out please try again").ConfigureAwait(false);
                    return;
                }

                gifLink = linkResponse.Result.Content.Trim();

                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Insert into ana (anaLink, addedBy) Values (?anaLink, ?addedBy)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?anaLink", MySqlDbType.VarChar).Value = gifLink;
                    command.Parameters.Add("?addedBy", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync($"Added ana photo/gif {gifLink}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("list")]
        [OwnerOrPermission(Permissions.Administrator)]
        [Description("show all ana pictures by id")]
        public async Task ShowanaPicturesOrGifs(CommandContext ctx)
        {
            try
            {
                Dictionary<string, string> anaPics = new Dictionary<string, string>();
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select id, anaLink from ana";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        anaPics.Add(reader.GetString("id"), reader.GetString("anaLink"));
                    }
                    reader.Close();
                }

                if (anaPics.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("There currently are no ana pictures stored").ConfigureAwait(false);
                }
                else
                {
                    var interactivity = ctx.Client.GetInteractivity();
                    StringBuilder sb = new StringBuilder();
                    foreach (var gifKey in anaPics.Keys)
                    {
                        sb.AppendLine(gifKey + " - " + anaPics[gifKey]);
                    }
                    var gifPages = interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, gifPages)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("delete")]
        [Description("deletes ana picture or gif")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task DeleteanaPictureOrGif(CommandContext ctx, [RemainingText, Description("id of ana picture to delete")] int anaId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    MySqlCommand command = new MySqlCommand("Removeana", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    command.Parameters.Add("anaId", MySqlDbType.Int32).Value = anaId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                await ctx.Channel.SendMessageAsync($"Deleted minx picture with id of {anaId}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("show")]
        [Description("shows specific ana picture or gif")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task Showana(CommandContext ctx, [RemainingText, Description("id of ana picture to show")] int anaId)
        {
            try
            {
                string anaLink = "";
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select anaLink from ana where id = ?id";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?id", MySqlDbType.VarChar).Value = anaId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        anaLink = reader.GetString("anaLink");
                    }
                }

                if (string.IsNullOrEmpty(anaLink))
                {
                    await ctx.Channel.SendMessageAsync($"Could not find a minx photo with id of {anaId}").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync(anaLink).ConfigureAwait(false);
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
