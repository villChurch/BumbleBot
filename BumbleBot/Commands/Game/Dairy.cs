using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

namespace BumbleBot.Commands.Game
{
    [Group("dairy")]
    [IsUserAvailable]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class Dairy : BaseCommandModule
    {
        public Dairy(DairyService dairyService, FarmerService farmerService, PerkService perkService)
        {
            this.DairyService = dairyService;
            this.FarmerService = farmerService;
            this.perkService = perkService;
        }

        private readonly PerkService perkService;
        private DairyService DairyService { get; }
        private FarmerService FarmerService { get; }

        [GroupCommand]
        public async Task ShowDairyUpgrades(CommandContext ctx)
        {
            if (!DairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} you do not have a dairy").ConfigureAwait(false);
            }
            else
            {
                var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
                int caveCost = 12000;
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Dairy Upgrade Options",
                    Color = DiscordColor.Aquamarine,
                    Description =
                        "Here are the available upgrade options for your dairy. To upgrade run `dairy upgrade {item} {price}`"
                };
                var dairy = DairyService.GetUsersDairy(ctx.User.Id);
                var price = dairy.Slots * 5000;
                if (usersPerks.Any(perk => perk.id == 14))
                {
                    caveCost = (int) Math.Ceiling(caveCost * 0.9);
                    price = (int) Math.Ceiling(price * 0.9);
                }
                embed.AddField("Capacity", $"{price} credits will increase milk capacity by 1,000 lbs");
                if (!DairyService.DoesDairyHaveACave(ctx.User.Id))
                    embed.AddField("Cave", $"{caveCost} credits will add a cave to your dairy to produce hard cheese");
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
            if (!DairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} you do not have a dairy").ConfigureAwait(false);
            }
            else if (options.Contains(option.ToLower()))
            {
                var dairy = DairyService.GetUsersDairy(ctx.User.Id);
                var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
                switch (option.ToLower())
                {
                    case "capacity":
                    {
                        var upgradePrice = dairy.Slots * 5000;
                        if (usersPerks.Any(perk => perk.id == 14))
                        {
                            upgradePrice = (int) Math.Ceiling(upgradePrice * 0.9);
                        }
                        if (price == upgradePrice)
                        {
                            FarmerService.DeductCreditsFromFarmer(ctx.User.Id, price);
                            DairyService.IncreaseCapcityOfDairy(ctx.User.Id, dairy.Slots, 1);
                            await ctx.Channel
                                .SendMessageAsync($"Your dairy can now hold {(dairy.Slots + 1) * 1000} lbs of milk")
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
                        var cavePrice = 12000;
                        if (usersPerks.Any(perk => perk.id == 14))
                        {
                            cavePrice = (int) Math.Ceiling(cavePrice * 0.9);
                        }
                        if (DairyService.DoesDairyHaveACave(ctx.User.Id))
                        {
                            await ctx.Channel.SendMessageAsync("Your dairy already has a cave").ConfigureAwait(false);
                        }
                        else if (price == cavePrice)
                        {
                            DairyService.CreateCaveInDairy(ctx.User.Id);
                            FarmerService.DeductCreditsFromFarmer(ctx.User.Id, cavePrice);
                            await ctx.Channel.SendMessageAsync("Your dairy now has a cave to age soft cheese in")
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await ctx.Channel.SendMessageAsync($"Upgrade price for {option} is {cavePrice} not {price}")
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
            if (!DairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} you do not have a dairy").ConfigureAwait(false);
            }
            else
            {
                var dairy = DairyService.GetUsersDairy(ctx.User.Id);
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.User.Username}'s Dairy",
                    Color = DiscordColor.Aquamarine
                };
                embed.AddField("Milk", $"{dairy.Milk} lbs", true);
                embed.AddField("Soft Cheese", $"{dairy.SoftCheese} lbs", true);
                embed.AddField("Hard Cheese", $"{dairy.HardCheese} lbs", true);
                embed.AddField("Milk capacity", $"{dairy.Slots * 1000} lbs", true);
                if (DairyService.DoesDairyHaveACave(ctx.User.Id))
                {
                    var cave = DairyService.GetUsersCave(ctx.User.Id);
                    embed.AddField("Cave", "Dairy Cave information below");
                    embed.AddField("Soft Cheese", $"{cave.SoftCheese} lbs", true);
                    embed.AddField("Soft Cheese capacity", $"{cave.Slots * 500} lbs", true);
                }

                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
        }

        [Command("sell")]
        [Description("Sell the contents of your dairy")]
        public async Task SellContentsOfDairy(CommandContext ctx)
        {
            if (!DairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Mention} you do not have a dairy").ConfigureAwait(false);
            }
            else
            {
                var dairy = DairyService.GetUsersDairy(ctx.User.Id);
                if (dairy.SoftCheese <= 0 && dairy.HardCheese <= 0)
                {
                    await ctx.Channel.SendMessageAsync("You don't have anything in your dairy that can be sold.")
                        .ConfigureAwait(false);
                }
                else
                {
                    var sellAmount = 0;
                    sellAmount += (int) Math.Ceiling(dairy.SoftCheese * 45);
                    var loanString = "";
                    if (FarmerService.DoesFarmerHaveALoan(ctx.User.Id))
                    {
                        var (repaymentAmount, loanAmount) = FarmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, sellAmount);
                        loanString =
                            $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                            $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";
                        FarmerService.AddCreditsToFarmer(ctx.User.Id, (sellAmount - repaymentAmount));
                    }
                    else
                    {
                        FarmerService.AddCreditsToFarmer(ctx.User.Id, sellAmount);
                    }
                    DairyService.RemoveSoftCheeseFromPlayer(ctx.User.Id, null);
                    DairyService.DeleteSoftCheeseFromExpiryTable(ctx.User.Id);
                    await ctx.Channel.SendMessageAsync($"Sold contents of your dairy for {sellAmount:n0}. {loanString}")
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
            else if (!DairyService.HasDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync("You need to purchase a dairy first.").ConfigureAwait(false);
            }
            else if (!DairyService.CanMilkFitInDairy(ctx.User.Id, milk))
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