using System;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BumbleBot.Commands.Game
{
    [Group("shop")]
    [Aliases("market")]
    [IsUserAvailable]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class FarmShop : BaseCommandModule
    {
        private readonly FarmerService farmerService;
        private readonly PerkService perkService;

        public FarmShop(FarmerService farmerService, PerkService perkService)
        {
            this.farmerService = farmerService;
            this.perkService = perkService;
        }

        [GroupCommand]
        public async Task ShopWelcomePage(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Welcome to the Shop",
                Description =
                    "Here you will find a list of purchaseable items. To obtain them use `purchase {itemName} {itemCost}`.",
                Color = DiscordColor.Aquamarine
            };

            var currentFarmer = farmerService.ReturnFarmerInfo(ctx.User.Id);

            if (string.IsNullOrEmpty(currentFarmer.Barnspace.ToString()) || currentFarmer.Barnspace == 0)
            {
                await ctx.Channel
                    .SendMessageAsync("Looks like you don't have a character yet, use the `create` command to start.")
                    .ConfigureAwait(false);
            }
            else
            {
                var userPerks = await perkService.GetUsersPerks(ctx.User.Id);
                var barnCost = (currentFarmer.Barnspace + 10) * 100;
                var grazeCost = (currentFarmer.Grazingspace + 10) * 100;
                var dairyCost = 10000;
                var shelterCost = 5000;
                var oatsCost = 250;
                var alfalfaCost = 500;
                var dustCost = 1000;
                if (userPerks.Any(perk => perk.id == 9))
                {
                    barnCost = (int) Math.Ceiling(barnCost * 0.75);
                }
                else if (userPerks.Any(perk => perk.id == 4))
                {
                    barnCost = (int) Math.Ceiling(barnCost * 0.9);
                }

                if (userPerks.Any(perk => perk.id == 11))
                {
                    grazeCost = (int) Math.Ceiling(grazeCost * 0.75);
                }
                else if (userPerks.Any(perk => perk.id == 5))
                {
                    grazeCost = (int) Math.Ceiling(grazeCost * 0.9);
                }

                if (userPerks.Any(perk => perk.id == 14))
                {
                    barnCost = (int) Math.Ceiling(barnCost * 0.9);
                    grazeCost = (int) Math.Ceiling(grazeCost * 0.9);
                    shelterCost = (int) Math.Ceiling(shelterCost * 0.9);
                    dairyCost = (int) Math.Ceiling(dairyCost * 0.9);
                    oatsCost = (int) Math.Ceiling(oatsCost * 0.9);
                    alfalfaCost = (int) Math.Ceiling(alfalfaCost * 0.9);
                    dustCost = (int) Math.Ceiling(dustCost * 0.9);
                }

                embed.AddField("Barn", $"Cost {barnCost} - Will provide 10 extra stalls");
                embed.AddField("Pasture", $"Cost {grazeCost} - Will provide 10 extra pasture space");
                if (!farmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                    embed.AddField("Shelter",
                        $"Cost {shelterCost} - Purchases a Kidding Pen which adds the ability to breed goats");
                if (!farmerService.DoesFarmerHaveDairy(ctx.User.Id))
                    embed.AddField("Dairy",
                        $"Cost {dairyCost} - Purchases a Dairy which can be used to make products from milk");
                embed.AddField("Oats",
                    $"Cost {oatsCost} - Will provide a boost to your goats milk output next time they're milked");
                embed.AddField("Alfalfa",
                    $"Cost {alfalfaCost} - Will give goats an exp boost when daily is used");
                embed.AddField("Dust",
                    $"Cost {dustCost} - Combined feed that offers both a boost to milk output and daily XP");
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
        }
    }
}
