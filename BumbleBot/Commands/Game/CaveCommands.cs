using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BumbleBot.Commands.Game
{
    [Group("cave")]
    [IsUserAvailable]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class CaveCommands : BaseCommandModule
    {
        private DairyService DairyService { get; }
        private FarmerService FarmerService { get; }

        public CaveCommands(FarmerService farmerService, DairyService dairyService)
        {
            DairyService = dairyService;
            FarmerService = farmerService;
        }

        [GroupCommand]
        public async Task CaveGeneralCommand(CommandContext ctx)
        {
            if (!DairyService.DoesDairyHaveACave(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync("First you need to purchase a cave by upgrading your dairy")
                    .ConfigureAwait(false);
            }
            else
            {
                var cave = DairyService.GetUsersCave(ctx.User.Id);
                var slotUpgradePrice = (3250 * cave.Slots);
                var embed = new DiscordEmbedBuilder()
                {
                    Title = "Cave upgrade options",
                    Color =  DiscordColor.Aquamarine
                };
                embed.AddField("Capacity",
                    $"{slotUpgradePrice} credits. Upgrades soft cheese cave capacity to {(cave.Slots + 1) * 500} " +
                    $"lbs by using command {Formatter.InlineCode($"cave upgrade capacity {slotUpgradePrice}")}");
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
        }

        [Command("upgrade")]
        [Description("Upgrade your cave")]
        [HasEnoughCredits(1)]
        public async Task UpgradeCave(CommandContext ctx, string item, int price)
        {
            if (!DairyService.DoesDairyHaveACave(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync("You do not own a cave").ConfigureAwait(false);
            }
            else if (item.ToLower().Equals("capacity"))
            {
                var cave = DairyService.GetUsersCave(ctx.User.Id);
                var slotUpgradePrice = (3250 * cave.Slots);
                if (price == slotUpgradePrice)
                {
                    DairyService.IncreaseCaveSlots(ctx.User.Id, cave.Slots + 1);
                    FarmerService.DeductCreditsFromFarmer(ctx.User.Id, slotUpgradePrice);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"You have upgraded your caves soft cheese capacity to {(cave.Slots + 1) * 500}lbs")
                        .ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel
                        .SendMessageAsync(
                            $"The cost to upgrade the capacity of your cave is {slotUpgradePrice} not {price}")
                        .ConfigureAwait(false);
                }
            }
            else
            {
                await ctx.Channel.SendMessageAsync($"There is no upgrade option for {item}").ConfigureAwait(false);
            }
        }
        
        [Command("sell")]
        [Description("Sell the contents of your cave")]
        public async Task SellHardCheeseInCave(CommandContext ctx)
        {
            if (!DairyService.DoesDairyHaveACave(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync("First you need to purchase a cave by upgrading your dairy")
                    .ConfigureAwait(false);
            }
            else
            {
                var dairy = DairyService.GetUsersDairy(ctx.User.Id);
                var saleAmount = (int)Math.Ceiling(dairy.HardCheese * 475);
                FarmerService.AddCreditsToFarmer(ctx.User.Id, saleAmount);
                DairyService.DeductAllHardCheeseFromDairy(ctx.User.Id);
                await ctx.Channel
                    .SendMessageAsync($"You have sold {dairy.HardCheese} lbs of hard cheese for {saleAmount} credits")
                    .ConfigureAwait(false);
            }
        } 

        [Command("add")]
        [Description("Add soft cheese to your cave")]
        public async Task AddSoftCheeseToCave(CommandContext ctx, int amount)
        {
            if (!DairyService.DoesDairyHaveACave(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync("You need to buy the cave upgrade for your dairy first")
                    .ConfigureAwait(false);
            }
            else
            {
                var dairy = DairyService.GetUsersDairy(ctx.User.Id);
                var cave = DairyService.GetUsersCave(ctx.User.Id);
                if (amount % 10 != 0)
                {
                    await ctx.Channel.SendMessageAsync("Soft Cheese has to be added to the cave in a ratio of 10:1, " +
                                                       "therefore the amount must be divisible by 10")
                        .ConfigureAwait(false);
                }
                else if (dairy.SoftCheese < amount)
                {
                    await ctx.Channel
                        .SendMessageAsync(
                            $"You only have {dairy.SoftCheese} lbs of soft cheese which is less than the {amount} lbs you tried to add")
                        .ConfigureAwait(false);
                }
                else if ((cave.SoftCheese + amount) > (cave.Slots * 500))
                {
                    await ctx.Channel
                        .SendMessageAsync(
                            $"There is not enough room in your cave to add soft cheese at the moment. Please upgrade your cave capacity.")
                        .ConfigureAwait(false);
                }
                else
                {
                    var random = new Random();
                    if (random.Next(0, 70) == 50)
                    {
                        DairyService.RemoveSoftCheeseFromPlayer(ctx.User.Id, amount);
                        await ctx.Channel.SendMessageAsync(
                            "Unfortunately something has gone wrong in the cheese making process");
                    }
                    else
                    {
                        _ = Task.Run(async () =>
                            await SendAndPostResponse(ctx,
                                $"http://localhost:8080/dairy/cave/{ctx.User.Id}/add/softcheese/{amount}")
                        );
                    }
                }
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
                using (var reader = new StreamReader(stream))
                {
                    var stringResponse = await reader.ReadToEndAsync();

                    await ctx.Channel.SendMessageAsync(stringResponse).ConfigureAwait(false);
                }
            }
        }
    }
}