using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;
using Type = BumbleBot.Models.Type;

namespace BumbleBot.Commands.Game
{
    [Group("goat")]
    [Description("General goat commands")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class GoatCommands : BaseCommandModule
    {
        private readonly DbUtils dBUtils = new DbUtils();

        public GoatCommands(GoatService goatService, FarmerService farmerService)
        {
            this.GoatService = goatService;
            this.FarmerService = farmerService;
        }

        private GoatService GoatService { get; }
        private FarmerService FarmerService { get; }

        [Command("stats")]
        [Aliases("statistics")]
        [Description("show statistics relating to goats you own")]
        public async Task ShowGoatStatistics(CommandContext ctx)
        {
            var goats = GoatService.ReturnUsersGoats(ctx.User.Id);
            var deadGoats = GoatService.ReturnUsersDeadGoats(ctx.User.Id);
            var kidsInPen = GoatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);

            var embed = new DiscordEmbedBuilder()
            {
                Title = $"{((DiscordMember) ctx.User).DisplayName}'s Goat statistics",
                Color = DiscordColor.Aquamarine
            };
            embed.AddField("Number of goats owned", goats.Count.ToString(), true);
            embed.AddField("Number of adults", goats.FindAll(goat => goat.Type == Type.Adult).Count.ToString(), true);
            embed.AddField("Number of kids", goats.FindAll(goat => goat.Type == Type.Kid).Count.ToString(), true);
            embed.AddField("Number of Nubian goats", goats.FindAll(goat => goat.Breed == Breed.Nubian).Count.ToString(),
                true);
            embed.AddField("Number of La Mancha goats",
                goats.FindAll(goat => goat.Breed == Breed.La_Mancha).Count.ToString(), true);
            embed.AddField("Number of Nigerian Dwarf goats",
                goats.FindAll(goat => goat.Breed == Breed.Nigerian_Dwarf).Count.ToString(), true);
            embed.AddField("Number of Special goats",
                goats.FindAll(goat => goat.BaseColour == BaseColour.Special).Count.ToString(), true);
            embed.AddField("Number of Chocolate goats",
                goats.FindAll(goat => goat.BaseColour == BaseColour.Chocolate).Count.ToString(), true);
            embed.AddField("Number of Black goats",
                goats.FindAll(goat => goat.BaseColour == BaseColour.Black).Count.ToString(), true);
            embed.AddField("Number of White goats",
                goats.FindAll(goat => goat.BaseColour == BaseColour.White).Count.ToString(), true);
            embed.AddField("Number of Gold goats",
                goats.FindAll(goat => goat.BaseColour == BaseColour.Gold).Count.ToString(), true);
            embed.AddField("Number of Red goats",
                goats.FindAll(goat => goat.BaseColour == BaseColour.Red).Count.ToString(), true);
            embed.AddField("Number of kids in shelter", kidsInPen.Count.ToString(), true);
            embed.AddField("Number of goats in memorial", deadGoats.Count.ToString(), true);
            await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }

        [Command("ordered")]
        [Description("Shows your goats ordered by a parameter")]
        public async Task ShowGoatsOrdered(CommandContext ctx, string parameter)
        {
            try
            {
                var orderParams = new HashSet<string>()
                {
                    "level",
                    "breed",
                    "colour"
                };
                if (parameter.ToLower().Equals("color"))
                {
                    parameter = "colour";
                    await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} you must of meant colour!")
                        .ConfigureAwait(false);
                }
                if (!orderParams.Contains(parameter.ToLower()))
                {
                    await ctx.Channel.SendMessageAsync($"There is no order option of {parameter}")
                        .ConfigureAwait(false);
                }
                else
                {
                    if (parameter.ToLower().Equals("level"))
                    {
                        var goats = GoatService.ReturnUsersGoats(ctx.User.Id).OrderByDescending(x => x.Level);
                        var url = "http://williamspires.com/";
                        var pages = new List<Page>();
                        var interactivity = ctx.Client.GetInteractivity();
                        foreach (var goat in goats)
                        {
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = $"{goat.Id}",
                                ImageUrl = url + Uri.EscapeUriString(goat.FilePath) //.Replace(" ", "%20")
                            };
                            embed.AddField("Name", goat.Name);
                            embed.AddField("Level", goat.Level.ToString(), true);
                            embed.AddField("Experience", goat.Experience.ToString(), true);
                            embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true);
                            embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                            var page = new Page
                            {
                                Embed = embed
                            };
                            pages.Add(page);
                        }

                        _ = Task.Run(async () =>await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false));
                    }
                    else if (parameter.ToLower().Equals("breed"))
                    {
                        var goats = GoatService.ReturnUsersGoats(ctx.User.Id).OrderBy(x => x.Breed);
                        var url = "http://williamspires.com/";
                        var pages = new List<Page>();
                        var interactivity = ctx.Client.GetInteractivity();
                        foreach (var goat in goats)
                        {
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = $"{goat.Id}",
                                ImageUrl = url + Uri.EscapeUriString(goat.FilePath) //.Replace(" ", "%20")
                            };
                            embed.AddField("Name", goat.Name);
                            embed.AddField("Level", goat.Level.ToString(), true);
                            embed.AddField("Experience", goat.Experience.ToString(), true);
                            embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true);
                            embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                            var page = new Page
                            {
                                Embed = embed
                            };
                            pages.Add(page);
                        }

                        _ = Task.Run(async () =>await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false));
                    }
                    else if (parameter.ToLower().Equals("colour"))
                    {
                        var goats = GoatService.ReturnUsersGoats(ctx.User.Id).OrderBy(x => x.BaseColour);
                        var url = "http://williamspires.com/";
                        var pages = new List<Page>();
                        var interactivity = ctx.Client.GetInteractivity();
                        foreach (var goat in goats)
                        {
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = $"{goat.Id}",
                                ImageUrl = url + Uri.EscapeUriString(goat.FilePath) //.Replace(" ", "%20")
                            };
                            embed.AddField("Name", goat.Name);
                            embed.AddField("Level", goat.Level.ToString(), true);
                            embed.AddField("Experience", goat.Experience.ToString(), true);
                            embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true);
                            embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                            var page = new Page
                            {
                                Embed = embed
                            };
                            pages.Add(page);
                        }

                        _ = Task.Run(async () =>await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        [Command("show")]
        [Description("Shows your goats")]
        public async Task ShowGoats(CommandContext ctx)
        {
            try
            {
                var goats = GoatService.ReturnUsersGoats(ctx.User.Id);

                var url = "http://williamspires.com/";
                var pages = new List<Page>();
                var interactivity = ctx.Client.GetInteractivity();
                foreach (var goat in goats)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"{goat.Id}",
                        ImageUrl = url + Uri.EscapeUriString(goat.FilePath) //.Replace(" ", "%20")
                    };
                    embed.AddField("Name", goat.Name);
                    embed.AddField("Level", goat.Level.ToString(), true);
                    embed.AddField("Experience", goat.Experience.ToString(), true);
                    embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed).Replace("_", " "), true);
                    embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                    var page = new Page
                    {
                        Embed = embed
                    };
                    pages.Add(page);
                }

                _ = Task.Run(async () => await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false));
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
            [RemainingText] [Description("New name for your goat")]
            string newName)
        {
            try
            {
                var goats = GoatService.ReturnUsersGoats(ctx.User.Id);
                if (goats.Any(x => x.Id == goatId))
                {
                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Update goats Set name = ?newName where id = ?goatId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?newName", MySqlDbType.VarChar).Value = newName;
                        command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goatId;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    var changedGoat = goats.Where(x => x.Id == goatId);
                    await ctx.Channel.SendMessageAsync($"{changedGoat.First().Name} has been renamed to {newName}")
                        .ConfigureAwait(false);
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
                var goats = GoatService.ReturnUsersGoats(ctx.User.Id);
                if (goats.Any(goat => goat.Id == goatId))
                {
                    GoatService.DeleteGoat(goatId);
                    var goat = goats.First(g => g.Id == goatId);
                    var creditsToAdd = goat.Type == Type.Adult ? (int)Math.Ceiling(goat.Level * 1.35) : (int)Math.Ceiling(goat.Level * 0.75);
                    FarmerService.AddCreditsToFarmer(ctx.User.Id, creditsToAdd);
                    await ctx.Channel.SendMessageAsync(
                        $"You have sold {goat.Name} to market for {creditsToAdd} " +
                        "credits").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync($"You do not own a goat with id {goatId}.")
                        .ConfigureAwait(false);
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
                var goats = GoatService.ReturnUsersDeadGoats(ctx.User.Id);

                var url = "http://williamspires.com/";
                var pages = new List<Page>();
                var interactivity = ctx.Client.GetInteractivity();
                foreach (var goat in goats)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"{goat.Id}",
                        ImageUrl = url + goat.FilePath.Replace(" ", "%20")
                    };
                    embed.AddField("Name", goat.Name);
                    embed.AddField("Level", goat.Level.ToString(), true);
                    embed.AddField("Experience", goat.Experience.ToString(), true);
                    embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed).Replace("_", " "), true);
                    embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                    var page = new Page
                    {
                        Embed = embed
                    };
                    pages.Add(page);
                }

                _ = Task.Run(async () =>await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}