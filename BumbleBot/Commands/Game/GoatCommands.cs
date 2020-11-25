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
        private FarmerService farmerService { get; }
        private DBUtils dBUtils = new DBUtils();

        public GoatCommands(GoatService goatService, FarmerService farmerService)
        {
            this.goatService = goatService;
            this.farmerService = farmerService;
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
                    embed.AddField("Name", goat.name, false);
                    embed.AddField("Level", goat.level.ToString(), true);
                    embed.AddField("Experience", goat.experience.ToString(), true);
                    embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed).Replace("_", " "), true);
                    embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
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
                    using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
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

        [Command("sell")]
        [Description("Sell a goat")]
        public async Task SellGoat(CommandContext ctx, [Description("id of goat to sell")] int goatId)
        {
            try
            {
                List<Goat> goats = goatService.ReturnUsersGoats(ctx.User.Id);
                if (goats.Any(goat => goat.id == goatId))
                {
                    goatService.DeleteGoat(goatId);
                    Goat goat = goats.First(g => g.id == goatId);
                    farmerService.AddCreditsToFarmer(ctx.User.Id, (int)Math.Ceiling(goat.level * 0.75));
                    await ctx.Channel.SendMessageAsync($"You have sold {goat.name} to market for {(int)Math.Ceiling(goat.level * 0.75)} " +
                        $"credits").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync($"You do not own a goat with id {goatId}.").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("memorial")]
        [Description("See all your past goats")]
        public async Task SeeDeadGoats(CommandContext ctx)
        {
            try
            {
                List<Goat> goats = goatService.ReturnUsersDeadGoats(ctx.User.Id);

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
                    embed.AddField("Name", goat.name, false);
                    embed.AddField("Level", goat.level.ToString(), true);
                    embed.AddField("Experience", goat.experience.ToString(), true);
                    embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed).Replace("_", " "), true);
                    embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
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
    }
}
