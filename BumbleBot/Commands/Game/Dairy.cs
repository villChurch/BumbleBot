using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BumbleBot.Commands.Game
{
    [Group("dairy")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class Dairy : BaseCommandModule
    {
        public Dairy(DairyService dairyService, FarmerService farmerService)
        {
            this.dairyService = dairyService;
            this.farmerService = farmerService;
        }

        private DairyService dairyService { get; }
        private FarmerService farmerService { get; }

        [GroupCommand]
        public async Task ShowDairyUpgrades(CommandContext ctx)
        {
            if (!dairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} you do not have a dairy").ConfigureAwait(false);
            }
            else
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Dairy Upgrade Options",
                    Color = DiscordColor.Aquamarine,
                    Description =
                        "Here are the available upgrade options for your dairy. To upgrade run `dairy upgrade {item} {price}`"
                };
                var dairy = dairyService.GetUsersDairy(ctx.User.Id);
                var price = dairy.slots * 5000;
                embed.AddField("Capacity", $"{price} credits will increase milk capacity by 1,000 lbs");
                if (!dairyService.DoesDairyHaveACave(ctx.User.Id))
                    embed.AddField("Cave", "12,000 credits will add a cave to your dairy to produce hard cheese");
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
        }

        [Command("upgrade")]
        [HasEnoughCredits(1)]
        [Description("Purchase upgrades for your dairy")]
        public async Task UpgradeDairy(CommandContext ctx, string option, int price)
        {
            var options = new HashSet<string>
            {
                "capacity",
                "cave"
            };
            if (!dairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} you do not have a dairy").ConfigureAwait(false);
            }
            else if (options.Contains(option.ToLower()))
            {
                var dairy = dairyService.GetUsersDairy(ctx.User.Id);
                switch (option.ToLower())
                {
                    case "capacity":
                    {
                        var upgradePrice = dairy.slots * 5000;
                        if (price == upgradePrice)
                        {
                            farmerService.DeductCreditsFromFarmer(ctx.User.Id, price);
                            dairyService.IncreaseCapcityOfDairy(ctx.User.Id, dairy.slots, 1);
                            await ctx.Channel
                                .SendMessageAsync($"Your dairy can now hold {(dairy.slots + 1) * 1000} lbs of milk")
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await ctx.Channel
                                .SendMessageAsync($"Upgrade price for {option} is {upgradePrice} not {price}")
                                .ConfigureAwait(false);
                        }

                        break;
                    }
                    case "cave":
                        if (dairyService.DoesDairyHaveACave(ctx.User.Id))
                        {
                            await ctx.Channel.SendMessageAsync("Your dairy already has a cave").ConfigureAwait(false);
                        }
                        else if (price == 12000)
                        {
                            dairyService.CreateCaveInDairy(ctx.User.Id);
                            farmerService.DeductCreditsFromFarmer(ctx.User.Id, 12000);
                            await ctx.Channel.SendMessageAsync("Your dairy now has a cave to age soft cheese in")
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await ctx.Channel.SendMessageAsync($"Upgrade price for {option} is 12,000 not {price}")
                                .ConfigureAwait(false);
                        }

                        break;
                }
            }
            else
            {
                await ctx.Channel.SendMessageAsync($"There is no upgrade option for the dairy called {option}")
                    .ConfigureAwait(false);
            }
        }

        [Command("show")]
        [Aliases("info")]
        [Description("Show the contents of your dairy")]
        public async Task ShowContentsOfDairy(CommandContext ctx)
        {
            if (!dairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} you do not have a dairy").ConfigureAwait(false);
            }
            else
            {
                var dairy = dairyService.GetUsersDairy(ctx.User.Id);
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.User.Username}'s Dairy",
                    Color = DiscordColor.Aquamarine
                };
                embed.AddField("Milk", $"{dairy.milk} lbs", true);
                embed.AddField("Soft Cheese", $"{dairy.softCheese} lbs", true);
                embed.AddField("Hard Cheese", $"{dairy.hardCheese} lbs", true);
                embed.AddField("Milk capacity", $"{dairy.slots * 1000} lbs", true);
                if (dairyService.DoesDairyHaveACave(ctx.User.Id))
                {
                    var cave = dairyService.GetUsersCave(ctx.User.Id);
                    embed.AddField("Cave", "Dairy Cave information below");
                    embed.AddField("Soft Cheese", $"{cave.softCheese} lbs", true);
                    embed.AddField("Soft Cheese capacity", $"{cave.slots * 500} lbs", true);
                }

                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
        }

        [Command("sell")]
        [Description("Sell the contents of your dairy")]
        public async Task SellContentsOfDairy(CommandContext ctx)
        {
            if (!dairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} you do not have a dairy").ConfigureAwait(false);
            }
            else
            {
                var dairy = dairyService.GetUsersDairy(ctx.User.Id);
                if (dairy.softCheese <= 0 && dairy.hardCheese <= 0)
                {
                    await ctx.Channel.SendMessageAsync("You don't have anything in your dairy that can be sold.")
                        .ConfigureAwait(false);
                }
                else
                {
                    var sellAmount = 0;
                    sellAmount += (int) Math.Ceiling(dairy.softCheese * 45);
                    farmerService.AddCreditsToFarmer(ctx.User.Id, sellAmount);
                    dairyService.RemoveSoftCheeseFromPlayer(ctx.User.Id, null);
                    dairyService.DeleteSoftCheeseFromExpiryTable(ctx.User.Id);
                    await ctx.Channel.SendMessageAsync($"Sold contents of your dairy for {sellAmount}")
                        .ConfigureAwait(false);
                }
            }
        }

        [Command("add")]
        [Description("add milk to the dairy to produce cheese")]
        public async Task AddMilkToDairy(CommandContext ctx, int milk)
        {
            if (milk % 10 != 0)
            {
                await ctx.Channel.SendMessageAsync("Milk has to be added to the dairy in a ratio of 10:1 " +
                                                   "therefore the amount must be divisible by 10.")
                    .ConfigureAwait(false);
            }
            else if (!dairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync("You need to purchase a dairy first.").ConfigureAwait(false);
            }
            else if (!dairyService.CanMilkFitInDairy(ctx.User.Id, milk))
            {
                await ctx.Channel.SendMessageAsync($"There is not enough room in your dairy for {milk} lbs of milk")
                    .ConfigureAwait(false);
            }
            else
            {
                var random = new Random();
                if (random.Next(0, 70) == 50)
                    await ctx.Channel.SendMessageAsync(
                        "Unfortunately something has gone wrong in the cheese making process");
                else
                    _ = SendAndPostResponse(ctx, $"http://localhost:8080/dairy/{ctx.User.Id}/add/milk/{milk}");
            }
        }

        private static async Task SendAndPostResponse(CommandContext ctx, string url)
        {
            // send and post response from api here
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (var response = (HttpWebResponse) await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            {
                if (stream != null)
                    using (var reader = new StreamReader(stream))
                    {
                        var stringResponse = await reader.ReadToEndAsync();

                        await ctx.Channel.SendMessageAsync(stringResponse).ConfigureAwait(false);
                    }
            }
        }
    }
}