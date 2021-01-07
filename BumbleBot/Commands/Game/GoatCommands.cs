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
        private readonly DBUtils dBUtils = new DBUtils();

        public GoatCommands(GoatService goatService, FarmerService farmerService)
        {
            this.goatService = goatService;
            this.farmerService = farmerService;
        }

        private GoatService goatService { get; }
        private FarmerService farmerService { get; }

        [Command("stats")]
        [Aliases("statistics")]
        [Description("show statistics relating to goats you own")]
        public async Task ShowGoatStatistics(CommandContext ctx)
        {
            var goats = goatService.ReturnUsersGoats(ctx.User.Id);
            var deadGoats = goatService.ReturnUsersDeadGoats(ctx.User.Id);
            var kidsInPen = goatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);

            var embed = new DiscordEmbedBuilder()
            {
                Title = $"{((DiscordMember) ctx.User).Nickname}'s Goat statistics",
                Color = DiscordColor.Aquamarine
            };
            embed.AddField("Number of goats owned", goats.Count.ToString(), true);
            embed.AddField("Number of adults", goats.FindAll(goat => goat.type == Type.Adult).Count.ToString(), true);
            embed.AddField("Number of kids", goats.FindAll(goat => goat.type == Type.Kid).Count.ToString(), true);
            embed.AddField("Number of Nubian goats", goats.FindAll(goat => goat.breed == Breed.Nubian).Count.ToString(),
                true);
            embed.AddField("Number of La Mancha goats",
                goats.FindAll(goat => goat.breed == Breed.La_Mancha).Count.ToString(), true);
            embed.AddField("Number of Nigerian Dwarf goats",
                goats.FindAll(goat => goat.breed == Breed.Nigerian_Dwarf).Count.ToString(), true);
            embed.AddField("Number of Special goats",
                goats.FindAll(goat => goat.baseColour == BaseColour.Special).Count.ToString(), true);
            embed.AddField("Number of Chocolate goats",
                goats.FindAll(goat => goat.baseColour == BaseColour.Chocolate).Count.ToString(), true);
            embed.AddField("Number of Black goats",
                goats.FindAll(goat => goat.baseColour == BaseColour.Black).Count.ToString(), true);
            embed.AddField("Number of White goats",
                goats.FindAll(goat => goat.baseColour == BaseColour.White).Count.ToString(), true);
            embed.AddField("Number of Gold goats",
                goats.FindAll(goat => goat.baseColour == BaseColour.Gold).Count.ToString(), true);
            embed.AddField("Number of Red goats",
                goats.FindAll(goat => goat.baseColour == BaseColour.Red).Count.ToString(), true);
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
                        var goats = goatService.ReturnUsersGoats(ctx.User.Id).OrderByDescending(x => x.level);
                        var url = "http://williamspires.com/";
                        var pages = new List<Page>();
                        var interactivity = ctx.Client.GetInteractivity();
                        foreach (var goat in goats)
                        {
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = $"{goat.id}",
                                ImageUrl = url + Uri.EscapeUriString(goat.filePath) //.Replace(" ", "%20")
                            };
                            embed.AddField("Name", goat.name);
                            embed.AddField("Level", goat.level.ToString(), true);
                            embed.AddField("Experience", goat.experience.ToString(), true);
                            embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed)?.Replace("_", " "), true);
                            embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
                            var page = new Page
                            {
                                Embed = embed
                            };
                            pages.Add(page);
                        }

                        await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false);
                    }
                    else if (parameter.ToLower().Equals("breed"))
                    {
                        var goats = goatService.ReturnUsersGoats(ctx.User.Id).OrderBy(x => x.breed);
                        var url = "http://williamspires.com/";
                        var pages = new List<Page>();
                        var interactivity = ctx.Client.GetInteractivity();
                        foreach (var goat in goats)
                        {
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = $"{goat.id}",
                                ImageUrl = url + Uri.EscapeUriString(goat.filePath) //.Replace(" ", "%20")
                            };
                            embed.AddField("Name", goat.name);
                            embed.AddField("Level", goat.level.ToString(), true);
                            embed.AddField("Experience", goat.experience.ToString(), true);
                            embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed)?.Replace("_", " "), true);
                            embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
                            var page = new Page
                            {
                                Embed = embed
                            };
                            pages.Add(page);
                        }

                        await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false);
                    }
                    else if (parameter.ToLower().Equals("colour"))
                    {
                        var goats = goatService.ReturnUsersGoats(ctx.User.Id).OrderBy(x => x.baseColour);
                        var url = "http://williamspires.com/";
                        var pages = new List<Page>();
                        var interactivity = ctx.Client.GetInteractivity();
                        foreach (var goat in goats)
                        {
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = $"{goat.id}",
                                ImageUrl = url + Uri.EscapeUriString(goat.filePath) //.Replace(" ", "%20")
                            };
                            embed.AddField("Name", goat.name);
                            embed.AddField("Level", goat.level.ToString(), true);
                            embed.AddField("Experience", goat.experience.ToString(), true);
                            embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed)?.Replace("_", " "), true);
                            embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
                            var page = new Page
                            {
                                Embed = embed
                            };
                            pages.Add(page);
                        }

                        await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false);
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
                var goats = goatService.ReturnUsersGoats(ctx.User.Id);

                var url = "http://williamspires.com/";
                var pages = new List<Page>();
                var interactivity = ctx.Client.GetInteractivity();
                foreach (var goat in goats)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"{goat.id}",
                        ImageUrl = url + Uri.EscapeUriString(goat.filePath) //.Replace(" ", "%20")
                    };
                    embed.AddField("Name", goat.name);
                    embed.AddField("Level", goat.level.ToString(), true);
                    embed.AddField("Experience", goat.experience.ToString(), true);
                    embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed).Replace("_", " "), true);
                    embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
                    var page = new Page
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
            [RemainingText] [Description("New name for your goat")]
            string newName)
        {
            try
            {
                var goats = goatService.ReturnUsersGoats(ctx.User.Id);
                if (goats.Any(x => x.id == goatId))
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

                    var changedGoat = goats.Where(x => x.id == goatId);
                    await ctx.Channel.SendMessageAsync($"{changedGoat.First().name} has been renamed to {newName}")
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
                var goats = goatService.ReturnUsersGoats(ctx.User.Id);
                if (goats.Any(goat => goat.id == goatId))
                {
                    goatService.DeleteGoat(goatId);
                    var goat = goats.First(g => g.id == goatId);
                    farmerService.AddCreditsToFarmer(ctx.User.Id, (int) Math.Ceiling(goat.level * 0.75));
                    await ctx.Channel.SendMessageAsync(
                        $"You have sold {goat.name} to market for {(int) Math.Ceiling(goat.level * 0.75)} " +
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
                var goats = goatService.ReturnUsersDeadGoats(ctx.User.Id);

                var url = "http://williamspires.com/";
                var pages = new List<Page>();
                var interactivity = ctx.Client.GetInteractivity();
                foreach (var goat in goats)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"{goat.id}",
                        ImageUrl = url + goat.filePath.Replace(" ", "%20")
                    };
                    embed.AddField("Name", goat.name);
                    embed.AddField("Level", goat.level.ToString(), true);
                    embed.AddField("Experience", goat.experience.ToString(), true);
                    embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed).Replace("_", " "), true);
                    embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
                    var page = new Page
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