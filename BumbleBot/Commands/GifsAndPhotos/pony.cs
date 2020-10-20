using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.GifsAndPhotos
{
    [Group("pony")]
    public class pony : BaseCommandModule
    {
        private readonly DBUtils dBUtils = new DBUtils();
        private AssholeService assholeService { get; }

        public pony(AssholeService assholeService)
        {
            this.assholeService = assholeService;
        }

        [GroupCommand]
        public async Task ShowRandomPonyPhoto(CommandContext ctx)
        {
            try
            {
                this.assholeService.SetAhConfig();
                bool isAssholeMode = false;
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
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
                            isAssholeMode = reader.GetBoolean("boolValue");
                        }
                    }
                    reader.Close();
                }
                if (isAssholeMode)
                {
                    await ctx.Channel.SendMessageAsync("Don't feel like it right now").ConfigureAwait(false);
                    return;
                }
                List<string> ponyLinks = new List<string>();
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select ponyLink from pony";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        ponyLinks.Add(reader.GetString("ponyLink"));
                    }
                    reader.Close();
                }

                if (ponyLinks.Count < 0)
                {
                    await ctx.Channel.SendMessageAsync($"Currently there are no kid pictures! Run {Formatter.InlineCode("!pony add")}" +
                        $" to add one").ConfigureAwait(false);
                }
                else
                {
                    Random rnd = new Random();
                    int gifToShow = rnd.Next(0, ponyLinks.Count);

                    await ctx.Channel.SendMessageAsync(ponyLinks.ElementAt(gifToShow)).ConfigureAwait(false);
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
        [Description("starts a dialogue to add a pony picture or gif")]
        public async Task AddPonyGifOrPhoto(CommandContext ctx)
        {
            try
            {
                string gifLink = "";
                var interactivity = ctx.Client.GetInteractivity();

                await ctx.Channel.SendMessageAsync("Please enter the link for this pony picture/gif").ConfigureAwait(false);
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
                    string query = "Insert into pony (ponyLink, addedBy) Values (?ponyLink, ?addedBy)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?ponyLink", MySqlDbType.VarChar).Value = gifLink;
                    command.Parameters.Add("?addedBy", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync($"Added pony photo/gif {gifLink}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("list")]
        [OwnerOrPermission(Permissions.Administrator)]
        [Description("show all pony pictures by id")]
        public async Task ShowPonyPicturesOrGifs(CommandContext ctx)
        {
            try
            {
                Dictionary<string, string> ponyPics = new Dictionary<string, string>();
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select id, ponyLink from pony";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        ponyPics.Add(reader.GetString("id"), reader.GetString("ponyLink"));
                    }
                    reader.Close();
                }

                if (ponyPics.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("There currently are no pony pictures stored").ConfigureAwait(false);
                }
                else
                {
                    var interactivity = ctx.Client.GetInteractivity();
                    StringBuilder sb = new StringBuilder();
                    foreach (var gifKey in ponyPics.Keys)
                    {
                        sb.AppendLine(gifKey + " - " + ponyPics[gifKey]);
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
        [Description("deletes pony picture or gif")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task DeletePonyPictureOrGif(CommandContext ctx, [RemainingText, Description("id of pony picture to delete")] int ponyId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    MySqlCommand command = new MySqlCommand("RemovePony", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    command.Parameters.Add("ponyId", MySqlDbType.Int32).Value = ponyId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                await ctx.Channel.SendMessageAsync($"Deleted minx picture with id of {ponyId}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("show")]
        [Description("shows specific pony picture or gif")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task ShowPony(CommandContext ctx, [RemainingText, Description("id of pony picture to show")] int ponyId)
        {
            try
            {
                string ponyLink = "";
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select ponyLink from pony where id = ?id";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?id", MySqlDbType.VarChar).Value = ponyId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        ponyLink = reader.GetString("ponyLink");
                    }
                }

                if (string.IsNullOrEmpty(ponyLink))
                {
                    await ctx.Channel.SendMessageAsync($"Could not find a minx photo with id of {ponyId}").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync(ponyLink).ConfigureAwait(false);
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
