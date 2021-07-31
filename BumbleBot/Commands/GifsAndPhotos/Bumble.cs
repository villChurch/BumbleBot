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
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.GifsAndPhotos
{
    [Group("bumble")]
    public class Bumble : BaseCommandModule
    {
        private readonly DbUtils dBUtils = new DbUtils();

        public Bumble(AssholeService assholeService)
        {
            this.AssholeService = assholeService;
        }

        private AssholeService AssholeService { get; }

        [GroupCommand]
        public async Task ShowRandombumblePhoto(CommandContext ctx)
        {
            try
            {
                AssholeService.SetAhConfig();
                var isAssholeMode = false;
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
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

                var bumbleLinks = new List<string>();
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Select bumbleLink from bumble";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read()) bumbleLinks.Add(reader.GetString("bumbleLink"));
                    reader.Close();
                }

                if (bumbleLinks.Count < 0)
                {
                    await ctx.Channel.SendMessageAsync(
                        $"Currently there are no bumble pictures! Run {Formatter.InlineCode("!bumble add")}" +
                        " to add one").ConfigureAwait(false);
                }
                else
                {
                    var rnd = new Random();
                    var gifToShow = rnd.Next(0, bumbleLinks.Count);

                    await ctx.Channel.SendMessageAsync(bumbleLinks.ElementAt(gifToShow)).ConfigureAwait(false);
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
        [Description("starts a dialogue to add a bumble picture or gif")]
        public async Task AddbumbleGifOrPhoto(CommandContext ctx)
        {
            try
            {
                var gifLink = "";
                var interactivity = ctx.Client.GetInteractivity();

                await ctx.Channel.SendMessageAsync("Please enter the link for this bumble picture/gif")
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

                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Insert into bumble (bumbleLink, addedBy) Values (?bumbleLink, ?addedBy)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?bumbleLink", MySqlDbType.VarChar).Value = gifLink;
                    command.Parameters.Add("?addedBy", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync($"Added bumble photo/gif {gifLink}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("list")]
        [OwnerOrPermission(Permissions.Administrator)]
        [Description("show all bumble pictures by id")]
        public async Task ShowbumblePicturesOrGifs(CommandContext ctx)
        {
            try
            {
                var bumblePics = new Dictionary<string, string>();
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Select id, bumbleLink from bumble";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read()) bumblePics.Add(reader.GetString("id"), reader.GetString("bumbleLink"));
                    reader.Close();
                }

                if (bumblePics.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("There currently are no bumble pictures stored")
                        .ConfigureAwait(false);
                }
                else
                {
                    var interactivity = ctx.Client.GetInteractivity();
                    var sb = new StringBuilder();
                    foreach (var gifKey in bumblePics.Keys) sb.AppendLine(gifKey + " - " + bumblePics[gifKey]);
                    var gifPages =
                        interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder());
                    _ = Task.Run(async () =>await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, gifPages, null,
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
        [Description("deletes bumble picture or gif")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task DeletebumblePictureOrGif(CommandContext ctx,
            [RemainingText] [Description("id of bumble picture to delete")]
            int bumbleId)
        {
            try
            {
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var command = new MySqlCommand("Removebumble", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    command.Parameters.Add("bumbleId", MySqlDbType.Int32).Value = bumbleId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync($"Deleted minx picture with id of {bumbleId}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("show")]
        [Description("shows specific bumble picture or gif")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task Showbumble(CommandContext ctx, [RemainingText] [Description("id of bumble picture to show")]
            int bumbleId)
        {
            try
            {
                var bumbleLink = "";
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "Select bumbleLink from bumble where id = ?id";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?id", MySqlDbType.VarChar).Value = bumbleId;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read()) bumbleLink = reader.GetString("bumbleLink");
                }

                if (string.IsNullOrEmpty(bumbleLink))
                    await ctx.Channel.SendMessageAsync($"Could not find a minx photo with id of {bumbleId}")
                        .ConfigureAwait(false);
                else
                    await ctx.Channel.SendMessageAsync(bumbleLink).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}