using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.GifsAndPhotos
{
    [Group("zteam")]
    public class Zteam : BaseCommandModule
    {
        private readonly DbUtils dBUtils = new DbUtils();

        public Zteam(AssholeService assholeService)
        {
            this.AssholeService = assholeService;
        }

        private AssholeService AssholeService { get; }

        [GroupCommand]
        public async Task ShowRandomZteamPhoto(CommandContext ctx)
        {
            try
            {
                AssholeService.SetAhConfig();
                var isAssholeMode = false;
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Select boolValue from config where paramName = ?paramName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?paramName", MySqlDbType.VarChar, 2550).Value = "assholeMode";
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                            isAssholeMode = reader.GetBoolean("boolValue");
                    reader.Close();
                }

                if (isAssholeMode)
                {
                    await ctx.Channel.SendMessageAsync("Don't feel like it right now").ConfigureAwait(false);
                    return;
                }

                var zteamLinks = new List<string>();
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Select zteamLink from zteam";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read()) zteamLinks.Add(reader.GetString("zteamLink"));
                    reader.Close();
                }

                if (zteamLinks.Count < 0)
                {
                    await ctx.Channel.SendMessageAsync(
                        $"Currently there are no zteam pictures! Run {Formatter.InlineCode("!zteam add")}" +
                        " to add one").ConfigureAwait(false);
                }
                else
                {
                    var rnd = new Random();
                    var gifToShow = rnd.Next(0, zteamLinks.Count);

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
                var gifLink = "";
                var interactivity = ctx.Client.GetInteractivity();

                await ctx.Channel.SendMessageAsync("Please enter the link for this zteam picture/gif")
                    .ConfigureAwait(false);
                var linkResponse = await interactivity.WaitForMessageAsync(
                    x => x.Channel == ctx.Channel && x.Author == ctx.Message.Author,
                    TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                if (linkResponse.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync($"{ctx.Command.Name} command has timed out please try again")
                        .ConfigureAwait(false);
                    return;
                }

                gifLink = linkResponse.Result.Content.Trim();

                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Insert into zteam (zteamLink, addedBy) Values (?zteamLink, ?addedBy)";
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
                var zteamPics = new Dictionary<string, string>();
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Select id, zteamLink from zteam";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read()) zteamPics.Add(reader.GetString("id"), reader.GetString("zteamLink"));
                    reader.Close();
                }

                if (zteamPics.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("There currently are no zteam pictures stored")
                        .ConfigureAwait(false);
                }
                else
                {
                    var interactivity = ctx.Client.GetInteractivity();
                    var sb = new StringBuilder();
                    foreach (var gifKey in zteamPics.Keys) sb.AppendLine(gifKey + " - " + zteamPics[gifKey]);
                    var gifPages =
                        interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                    _ = Task.Run(async () => await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, gifPages, null,
                            PaginationBehaviour.WrapAround,ButtonPaginationBehavior.Disable,CancellationToken.None)
                        .ConfigureAwait(false));
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
        public async Task DeleteZteamPictureOrGif(CommandContext ctx,
            [RemainingText] [Description("id of zteam picture to delete")]
            int zteamId)
        {
            try
            {
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var command = new MySqlCommand("RemoveZteam", connection)
                    {
                        CommandType = CommandType.StoredProcedure
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
        public async Task ShowZteam(CommandContext ctx, [RemainingText] [Description("id of zteam picture to show")]
            int zteamId)
        {
            try
            {
                var zteamLink = "";
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Select zteamLink from zteam where id = ?id";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?id", MySqlDbType.VarChar).Value = zteamId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read()) zteamLink = reader.GetString("zteamLink");
                }

                if (string.IsNullOrEmpty(zteamLink))
                    await ctx.Channel.SendMessageAsync($"Could not find a minx photo with id of {zteamId}")
                        .ConfigureAwait(false);
                else
                    await ctx.Channel.SendMessageAsync(zteamLink).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}