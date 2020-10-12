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
    [Group("zteam")]
    public class Zteam : BaseCommandModule
    {
        private readonly DBUtils dBUtils = new DBUtils();

        [GroupCommand]
        public async Task ShowRandomZteamPhoto(CommandContext ctx)
        {
            try
            {
                List<string> zteamLinks = new List<string>();
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select zteamLink from zteam";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        zteamLinks.Add(reader.GetString("zteamLink"));
                    }
                    reader.Close();
                }

                if (zteamLinks.Count < 0)
                {
                    await ctx.Channel.SendMessageAsync($"Currently there are no zteam pictures! Run {Formatter.InlineCode("!zteam add")}" +
                        $" to add one").ConfigureAwait(false);
                }
                else
                {
                    Random rnd = new Random();
                    int gifToShow = rnd.Next(0, zteamLinks.Count);

                    await ctx.Channel.SendMessageAsync(zteamLinks.ElementAt(gifToShow)).ConfigureAwait(false);
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
        [Description("starts a dialogue to add a zteam picture or gif")]
        public async Task AddZteamGifOrPhoto(CommandContext ctx)
        {
            try
            {
                string gifLink = "";
                var interactivity = ctx.Client.GetInteractivity();

                await ctx.Channel.SendMessageAsync("Please enter the link for this zteam picture/gif").ConfigureAwait(false);
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
                    string query = "Insert into zteam (zteamLink, addedBy) Values (?zteamLink, ?addedBy)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?zteamLink", MySqlDbType.VarChar).Value = gifLink;
                    command.Parameters.Add("?addedBy", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync($"Added zteam photo/gif {gifLink}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("list")]
        [OwnerOrPermission(Permissions.Administrator)]
        [Description("show all zteam pictures by id")]
        public async Task ShowZteamPicturesOrGifs(CommandContext ctx)
        {
            try
            {
                Dictionary<string, string> zteamPics = new Dictionary<string, string>();
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select id, zteamLink from zteam";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        zteamPics.Add(reader.GetString("id"), reader.GetString("zteamLink"));
                    }
                    reader.Close();
                }

                if (zteamPics.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("There currently are no zteam pictures stored").ConfigureAwait(false);
                }
                else
                {
                    var interactivity = ctx.Client.GetInteractivity();
                    StringBuilder sb = new StringBuilder();
                    foreach (var gifKey in zteamPics.Keys)
                    {
                        sb.AppendLine(gifKey + " - " + zteamPics[gifKey]);
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
        [Description("deletes zteam picture or gif")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task DeleteZteamPictureOrGif(CommandContext ctx, [RemainingText, Description("id of zteam picture to delete")] int zteamId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    MySqlCommand command = new MySqlCommand("RemoveZteam", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    command.Parameters.Add("zteamId", MySqlDbType.Int32).Value = zteamId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                await ctx.Channel.SendMessageAsync($"Deleted minx picture with id of {zteamId}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("show")]
        [Description("shows specific zteam picture or gif")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task ShowZteam(CommandContext ctx, [RemainingText, Description("id of zteam picture to show")] int zteamId)
        {
            try
            {
                string zteamLink = "";
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select zteamLink from zteam where id = ?id";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?id", MySqlDbType.VarChar).Value = zteamId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        zteamLink = reader.GetString("zteamLink");
                    }
                }

                if (string.IsNullOrEmpty(zteamLink))
                {
                    await ctx.Channel.SendMessageAsync($"Could not find a minx photo with id of {zteamId}").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync(zteamLink).ConfigureAwait(false);
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
