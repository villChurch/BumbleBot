﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace BumbleBot.Commands.Game
{
    [Group("shelter")]
    [Description("Shelter commands")]
    public class KiddingPen : BaseCommandModule
    {
        public KiddingPen(FarmerService farmerService, GoatService goatService)
        {
            this.FarmerService = farmerService;
            this.GoatService = goatService;
        }

        private FarmerService FarmerService { get; }
        private GoatService GoatService { get; }

        [GroupCommand]
        public async Task ShowUpgradeOptions(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Shelter upgrade options",
                Color = DiscordColor.Aquamarine,
                Description =
                    "Here are the available upgrade options for your shelter. To upgrade run `shelter upgrade {item} {price}`"
            };
            var capacity = FarmerService.GetKiddingPenCapacity(ctx.User.Id);
            var price = (int) Math.Ceiling(5000 * capacity / 2.0);
            embed.AddField("Capacity", $"{price} credits will increase capacity by 1");
            await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
        }

        [Command("info")]
        [Description("Show information about your shelter")]
        public async Task ShowShelterInfo(CommandContext ctx)
        {
            if (FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.User.Username}'s shelter",
                    Color = DiscordColor.Aquamarine
                };
                embed.AddField("Capacity", FarmerService.GetKiddingPenCapacity(ctx.User.Id).ToString(), true);
                embed.AddField("In use",
                    FarmerService.DoesFarmerHaveAdultsInKiddingPen(GoatService.ReturnUsersGoats(ctx.User.Id))
                        ? "Yes"
                        : "False", true);
                embed.AddField("Kids in shelter",
                    FarmerService.DoesFarmerHaveKidsInKiddingPen(ctx.User.Id) ? "Yes" : "False", true);
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
            else
            {
                await ctx.Channel.SendMessageAsync("You do not own a shelter yet.").ConfigureAwait(false);
            }
        }

        [Command("upgrade")]
        [HasEnoughCredits(1)]
        [Description("show upgrade options for shelter and upgrade it")]
        public async Task UpgradeShelter(CommandContext ctx, string option, int price)
        {
            var options = new HashSet<string>
            {
                "capacity"
            };

            if (!FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync("You do not own a shelter yet.").ConfigureAwait(false);
            }
            else if (options.Contains(option))
            {
                if (option.ToLower().Equals("capacity"))
                {
                    var capacity = FarmerService.GetKiddingPenCapacity(ctx.User.Id);
                    var upgradePrice = (int) Math.Ceiling(5000 * capacity / 2.0);
                    if (price == upgradePrice)
                    {
                        FarmerService.IncreaseKiddingPenCapacity(ctx.User.Id, capacity, 1);
                        FarmerService.DeductCreditsFromFarmer(ctx.User.Id, upgradePrice);
                        await ctx.Channel.SendMessageAsync($"Your shelter can now hold {capacity + 1} does")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await ctx.Channel.SendMessageAsync($"Upgrade price for {option} is {upgradePrice} not {price}")
                            .ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await ctx.Channel.SendMessageAsync($"There is no upgrade option for the shelter called {option}")
                    .ConfigureAwait(false);
            }
        }

        [Command("move")]
        [Aliases("transfer")]
        [Description("move a kid into your goat pen")]
        public async Task MoveKidIntoKiddingPen(CommandContext ctx, [Description("id of kid to move")] string idString)
        {
            try
            {
                if (!FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have a kidding pen").ConfigureAwait(false);
                }
                else if (!FarmerService.DoesFarmerHaveKidsInKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have any kids in your kidding pen")
                        .ConfigureAwait(false);
                }
                else if (idString.ToLower().Equals("all"))
                {
                    var kids = GoatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                    if (GoatService.CanGoatsFitInBarn(ctx.User.Id, kids.Count))
                    {
                        kids.ForEach(kid => GoatService.MoveKidIntoGoatPen(kid, ctx.User.Id));
                        await ctx.Channel.SendMessageAsync($"{kids.Count} kids have now been moved to your barn")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await ctx.Channel
                            .SendMessageAsync($"There is not enough room in your barn for all {kids.Count} kids")
                            .ConfigureAwait(false);
                    }
                }
                else
                {
                    if (!int.TryParse(idString, out var id))
                    {
                        await ctx.Channel.SendMessageAsync($"You entered {idString} which is not a number")
                            .ConfigureAwait(false);
                        throw new Exception("not a number");
                    }

                    var kids = GoatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                    var kidToMove = kids.Find(x => x.Id == id);
                    if (null == kidToMove)
                    {
                        await ctx.Channel.SendMessageAsync("You don't have a kid in your shelter with this id")
                            .ConfigureAwait(false);
                    }
                    else if (GoatService.CanGoatsFitInBarn(ctx.User.Id, 1))
                    {
                        GoatService.MoveKidIntoGoatPen(kidToMove, ctx.User.Id);
                        await ctx.Channel.SendMessageAsync($"Kid with id {id} has been moved into your barn")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await ctx.Channel.SendMessageAsync("There is no room in your barn for this kid at the moment")
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("sell")]
        [Description("Sell a kid in your shelter")]
        public async Task SellKidInKiddingPen(CommandContext ctx, [Description("id of kid to sell")] string idString)
        {
            try
            {
                if (!FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have a kidding pen").ConfigureAwait(false);
                }
                else if (!FarmerService.DoesFarmerHaveKidsInKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have any kids in your kidding pen")
                        .ConfigureAwait(false);
                }
                else if (idString.ToLower().Equals("all"))
                {
                    var kids = GoatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                    var total = 0;
                    kids.ForEach(kid =>
                    {
                        GoatService.DeleteKidFromKiddingPen(kid.Id);
                        FarmerService.AddCreditsToFarmer(ctx.User.Id, kid.Level * 2);
                        total += kid.Level * 2;
                    });
                    var kidOrKids = kids.Count == 1 ? "kid" : "kids";
                    await ctx.Channel
                        .SendMessageAsync($"You have sold {kids.Count} {kidOrKids} for {total.ToString()} credits.")
                        .ConfigureAwait(false);
                }
                else
                {
                    if (!int.TryParse(idString, out var id))
                    {
                        await ctx.Channel.SendMessageAsync($"You entered {idString} which is not a number")
                            .ConfigureAwait(false);
                        throw new Exception("not a number");
                    }

                    var kids = GoatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                    var kidToSell = kids.Find(x => x.Id == id);
                    if (null == kidToSell)
                    {
                        await ctx.Channel.SendMessageAsync("You don't have a kid in your shelter with this id")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        GoatService.DeleteKidFromKiddingPen(id);
                        FarmerService.AddCreditsToFarmer(ctx.User.Id, kidToSell.Level * 2);
                        await ctx.Channel
                            .SendMessageAsync($"You have sold kid with id {id} for {kidToSell.Level * 2} credits")
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("show")]
        [Description("Shows the kids in your shelter")]
        public async Task ShowKidsInKiddingPen(CommandContext ctx)
        {
            try
            {
                if (!FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have a kidding pen").ConfigureAwait(false);
                }
                else if (!FarmerService.DoesFarmerHaveKidsInKiddingPen(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have any kids in your kidding pen")
                        .ConfigureAwait(false);
                }
                else
                {
                    var kids = GoatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                    var url = "http://williamspires.com/";
                    var pages = new List<Page>();
                    var interactivity = ctx.Client.GetInteractivity();
                    foreach (var goat in kids)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{goat.Id}",
                            ImageUrl = url + goat.FilePath.Replace(" ", "%20")
                        };
                        embed.AddField("Name", goat.Name);
                        embed.AddField("Level", goat.Level.ToString(), true);
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
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}