using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using System.Linq;
using BumbleBot.Attributes;
using System.Text;
using DSharpPlus.Entities;

namespace BumbleBot.Commands.GifsAndPhotos
{
    [Group("gif")]
    public class KidPhotos : BaseCommandModule
    {

        private readonly DBUtils dBUtils = new DBUtils();

        [GroupCommand]
        public async Task RandomGif(CommandContext ctx)
        {
            try
            {
                List<string> gifLinks = new List<string>();
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select gifLink from goatGifs";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        gifLinks.Add(reader.GetString("gifLink"));
                    }
                    reader.Close();
                }

                if (gifLinks.Count < 0)
                {
                    await ctx.Channel.SendMessageAsync($"Currently there are no gifs! Run {Formatter.InlineCode("!giff add")}" +
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
        [Description("starts a dialogue to add a gif")]
        public async Task AddGoatGiff(CommandContext ctx)
        {
            try
            {
                string gifName = "";
                string gifLink = "";
                var interactivity = ctx.Client.GetInteractivity();

                await ctx.Channel.SendMessageAsync("Please enter the name for this gif").ConfigureAwait(false);
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
                    string query = "Insert into goatGifs (gifName, gifLink, addedBy) Values (?gifName, ?gifLink, ?addedBy)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?gifName", MySqlDbType.VarChar).Value = gifName;
                    command.Parameters.Add("?gifLink", MySqlDbType.VarChar).Value = gifLink;
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
        [Description("show all gifs by name")]
        public async Task ShowGifs (CommandContext ctx)
        {
            try
            {
                Dictionary<string, string> goatGifs = new Dictionary<string, string>();
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select gifName, gifLink from goatGifs";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        goatGifs.Add(reader.GetString("gifName"), reader.GetString("gifLink"));
                    }
                    reader.Close();
                }

                if (goatGifs.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("There currently are no gifs stored").ConfigureAwait(false);
                }
                else
                {
                    var interactivity = ctx.Client.GetInteractivity();
                    StringBuilder sb = new StringBuilder();
                    foreach (var gifKey in goatGifs.Keys)
                    {
                        sb.AppendLine(gifKey + " - " + goatGifs[gifKey]);
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
        [Description("deletes gif")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task DeleteGif(CommandContext ctx, [RemainingText, Description("name of gif to delete")] string gifName)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    MySqlCommand command = new MySqlCommand("RemoveGif", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    command.Parameters.Add("gifName", MySqlDbType.VarChar).Value = gifName;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                await ctx.Channel.SendMessageAsync($"Deleted gif with name {gifName}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("show")]
        [Description("shows specific gif")]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task ShowGif(CommandContext ctx, [RemainingText, Description("name of gif to show")] string gifName)
        {
            try
            {
                string gifLink = "";
                using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select gifLink from goatGifs where gifName = ?gifName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?gifName", MySqlDbType.VarChar).Value = gifName;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        gifLink = reader.GetString("gifLink");
                    }
                }

                if (string.IsNullOrEmpty(gifLink))
                {
                    await ctx.Channel.SendMessageAsync($"Could not find a gif with name {gifName}").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync(gifLink).ConfigureAwait(false);
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
