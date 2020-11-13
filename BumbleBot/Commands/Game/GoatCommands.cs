using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System.Linq;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.Game
{
    [Group("goat")]
    [Description("General goat commands")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class GoatCommands : BaseCommandModule
    {
        private GoatService goatService { get; }
        private DBUtils dBUtils = new DBUtils();
        private Timer nameTimer;
        private bool nameTimerRunning = false;

        public GoatCommands(GoatService goatService)
        {
            this.goatService = goatService;
        }

        private void SetNameTimer()
        {
            nameTimer = new Timer(240000);
            nameTimer.Elapsed += FinishTimer;
            nameTimer.Enabled = true;
            nameTimerRunning = true;
        }

        private void FinishTimer(Object source, ElapsedEventArgs e)
        {
            nameTimerRunning = false;
            nameTimer.Stop();
            nameTimer.Dispose();
        }

        [Command("show")]
        [Description("Shows your goats")]
        public async Task ShowGoats(CommandContext ctx)
        {
            try
            {
                List<Goat> goats = goatService.ReturnUsersGoats(ctx.User.Id);

                var url = "http://williamspires.com/";
                List<Page> pages = new List<Page>();
                var interactivity = ctx.Client.GetInteractivity();
                foreach (var goat in goats)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"{goat.id}",
                        ImageUrl = url + goat.filePath.Replace(" ", "%20")
                    };
                    embed.AddField("Name", goat.name, true);
                    embed.AddField("Level", goat.level.ToString(), true);
                    embed.AddField("Experience", goat.experience.ToString(), true);
                    Page page = new Page
                    {
                        Embed = embed
                    };
                    pages.Add(page);
                }
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("rename")]
        [Description("Rename a goat")]
        public async Task RenameGoat(CommandContext ctx, [Description("id of goat to rename")] int goatId,
            [RemainingText, Description("New name for your goat")] string newName) {
            try
            {
                List<Goat> goats = goatService.ReturnUsersGoats(ctx.User.Id);
                if (goats.Any(x => x.id == goatId))
                {
                    using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "Update goats Set name = ?newName where id = ?goatId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?newName", MySqlDbType.VarChar).Value = newName;
                        command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goatId;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    var changedGoat = goats.Where(x => x.id == goatId);
                    await ctx.Channel.SendMessageAsync($"{changedGoat.First().name} has been renamed to {newName}").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync("You do not own a goat with this ID").ConfigureAwait(false);
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
