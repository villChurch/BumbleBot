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
    [Group("minx")]
    public class Minx : BaseCommandModule
    {
        private readonly DbUtils dBUtils = new DbUtils();

        public Minx(AssholeService assholeService)
        {
            this.AssholeService = assholeService;
        }

        private AssholeService AssholeService { get; }

        [GroupCommand]
        public async Task ShowRandomMinxPhoto(CommandContext ctx)
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

                var minxLinks = new List<string>();
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Select minxLink from minx";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read()) minxLinks.Add(reader.GetString("minxLink"));
                    reader.Close();
                }

                if (minxLinks.Count < 0)
                {
                    await ctx.Channel.SendMessageAsync(
                        $"Currently there are no minx pictures! Run {Formatter.InlineCode("!minx add")}" +
                        " to add one").ConfigureAwait(false);
                }
                else
                {
                    var rnd = new Random();
                    var gifToShow = rnd.Next(0, minxLinks.Count);

                    await ctx.Channel.SendMessageAsync(minxLinks.ElementAt(gifToShow)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                await Console.Out.WriteLineAsync(ex.StackTrace);
            }
        }

        [Command("add")]
        [OwnerOrPermission(Permissions.Administrator)]
        [Description("starts a dialogue to add a minx picture")]
        public async Task AddMinxGifOrPic(CommandContext ctx)
        {
            try
            {
                var gifLink = "";
                var interactivity = ctx.Client.GetInteractivity();

                await ctx.Channel.SendMessageAsync("Please enter the link for this minx picture/gif")
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
                    var query = "Insert into minx (minxLink, addedBy) Values (?minxLink, ?addedBy)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?minxLink", MySqlDbType.VarChar).Value = gifLink;
                    command.Parameters.Add("?addedBy", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync($"Added minx photo/gif {gifLink}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                await Console.Out.WriteLineAsync(ex.StackTrace);
            }
        }

        [Command("list")]
        [OwnerOrPermission(Permissions.Administrator)]
        [Description("show all minx pictures by id")]
        public async Task ShowMinxs(CommandContext ctx)
        {
            try
            {
                var minxPics = new Dictionary<string, string>();
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Select id, minxLink from minx";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read()) minxPics.Add(reader.GetString("id"), reader.GetString("minxLink"));
                    reader.Close();
                }

                if (minxPics.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("There currently are no kid pictures stored")
                        .ConfigureAwait(false);
                }
                else
                {
                    var interactivity = ctx.Client.GetInteractivity();
                    var sb = new StringBuilder();
                    foreach (var gifKey in minxPics.Keys) sb.AppendLine(gifKey + " - " + minxPics[gifKey]);
                    var gifPages =
                        interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                    _ = Task.Run(async () => await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, gifPages, null,
                            PaginationBehaviour.WrapAround,ButtonPaginationBehavior.Disable, CancellationToken.None)
                        .ConfigureAwait(false));
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                await Console.Out.WriteLineAsync(ex.StackTrace);
            }
        }

        [Command("delete")]
        [Description("deletes minx picture")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task DeleteMinx(CommandContext ctx, [RemainingText] [Description("id of minx picture to delete")]
            int minxId)
        {
            try
            {
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var command = new MySqlCommand("RemoveMinx", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.Add("minxId", MySqlDbType.Int32).Value = minxId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync($"Deleted minx picture with id of {minxId}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                await Console.Out.WriteLineAsync(ex.StackTrace);
            }
        }

        [Command("show")]
        [Description("shows specific kid picture")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task ShowMinx(CommandContext ctx, [RemainingText] [Description("id of minx picture to show")]
            int minxId)
        {
            try
            {
                var minxLink = "";
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Select minxLink from minx where id = ?id";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?id", MySqlDbType.VarChar).Value = minxId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read()) minxLink = reader.GetString("minxLink");
                }

                if (string.IsNullOrEmpty(minxLink))
                    await ctx.Channel.SendMessageAsync($"Could not find a minx photo with id of {minxId}")
                        .ConfigureAwait(false);
                else
                    await ctx.Channel.SendMessageAsync(minxLink).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.Message);
                await Console.Out.WriteLineAsync(ex.StackTrace);
            }
        }
    }
}