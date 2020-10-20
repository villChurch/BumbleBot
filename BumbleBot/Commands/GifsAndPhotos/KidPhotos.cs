using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;
using System.Linq;
using BumbleBot.Attributes;
using System.Text;
using DSharpPlus.Entities;
using BumbleBot.Services;
using DSharpPlus.Interactivity.Enums;

namespace BumbleBot.Commands.GifsAndPhotos
{
    [Group("kid")]
    [Aliases("baby", "babygoat")]
    public class KidPhotos : BaseCommandModule
    {

        private readonly DBUtils dBUtils = new DBUtils();
        private AssholeService assholeService { get; }

        public KidPhotos(AssholeService assholeService)
        {
            this.assholeService = assholeService;
        }

        [GroupCommand]
        public async Task RandomKid(CommandContext ctx)
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
                List<string> gifLinks = new List<string>();
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select kidLink from goatKids";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        gifLinks.Add(reader.GetString("kidLink"));
                    }
                    reader.Close();
                }

                if (gifLinks.Count < 0)
                {
                    await ctx.Channel.SendMessageAsync($"Currently there are no kid pictures! Run {Formatter.InlineCode("!kid add")}" +
                        $" to add one").ConfigureAwait(false);
                }
                else
                {
                    Random rnd = new Random();
                    int gifToShow = rnd.Next(0, gifLinks.Count);

                    await ctx.Channel.SendMessageAsync(gifLinks.ElementAt(gifToShow)).ConfigureAwait(false);
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
        [Description("starts a dialogue to add a kid picture")]
        public async Task AddGoatGiff(CommandContext ctx)
        {
            try
            {
                string gifName = "";
                string gifLink = "";
                var interactivity = ctx.Client.GetInteractivity();

                await ctx.Channel.SendMessageAsync("Please enter the name for this kid picture").ConfigureAwait(false);
                var messageRespone = await interactivity.WaitForMessageAsync(x => x.Channel == ctx.Channel && x.Author == ctx.Message.Author,
                    TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                if (messageRespone.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync($"{ctx.Command.Name} command has timed out please try again").ConfigureAwait(false);
                    return;
                }

                gifName = messageRespone.Result.Content.Trim();

                await ctx.Channel.SendMessageAsync("Please enter the link for this gif").ConfigureAwait(false);
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
                    string query = "Insert into goatKids (kidName, kidLink, addedBy) Values (?kidName, ?kidLink, ?addedBy)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?kidName", MySqlDbType.VarChar).Value = gifName;
                    command.Parameters.Add("?kidLink", MySqlDbType.VarChar).Value = gifLink;
                    command.Parameters.Add("?addedBy", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync($"Added gif called {gifName}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("list")]
        [OwnerOrPermission(Permissions.Administrator)]
        [Description("show all kid pictures by name")]
        public async Task ShowGifs (CommandContext ctx)
        {
            try
            {
                Dictionary<string, string> goatKids = new Dictionary<string, string>();
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select kidName, kidLink from goatKids";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        goatKids.Add(reader.GetString("kidName"), reader.GetString("kidLink"));
                    }
                    reader.Close();
                }

                if (goatKids.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("There currently are no kid pictures stored").ConfigureAwait(false);
                }
                else
                {
                    var interactivity = ctx.Client.GetInteractivity();
                    StringBuilder sb = new StringBuilder();
                    foreach (var gifKey in goatKids.Keys)
                    {
                        sb.AppendLine(gifKey + " - " + goatKids[gifKey]);
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
        [Description("deletes kid picture")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task DeleteGif(CommandContext ctx, [RemainingText, Description("name of kid picture to delete")] string gifName)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    MySqlCommand command = new MySqlCommand("RemoveKid", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    command.Parameters.Add("kidName", MySqlDbType.VarChar).Value = gifName;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                await ctx.Channel.SendMessageAsync($"Deleted kid picture with name {gifName}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("show")]
        [Description("shows specific kid picture")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task ShowGif(CommandContext ctx, [RemainingText, Description("name of kid picture to show")] string kidName)
        {
            try
            {
                string kidLink = "";
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select kidLink from goatKids where kidName = ?kidName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?kidName", MySqlDbType.VarChar).Value = kidName;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        kidLink = reader.GetString("kidLink");
                    }
                }

                if (string.IsNullOrEmpty(kidLink))
                {
                    await ctx.Channel.SendMessageAsync($"Could not find a kid photo with name {kidName}").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync(kidLink).ConfigureAwait(false);
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
