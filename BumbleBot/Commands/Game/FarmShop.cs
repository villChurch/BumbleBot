using System.Threading.Tasks;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BumbleBot.Commands.Game
{
    [Group("shop")]
    [Aliases("market")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class FarmShop : BaseCommandModule
    {
        private readonly FarmerService farmerService;

        public FarmShop(FarmerService farmerService)
        {
            this.farmerService = farmerService;
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
                var barnCost = (currentFarmer.Barnspace + 10) * 100;
                var grazeCost = (currentFarmer.Grazingspace + 10) * 100;

                embed.AddField("Barn", $"Cost {barnCost} - Will provide 10 extra stalls");
                embed.AddField("Pasture", $"Cost {grazeCost} - Will provide 10 extra pasture space");
                if (!farmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                    embed.AddField("Shelter",
                        "Cost 5,000 - Purchases a Kidding Pen which adds the ability to breed goats");
                if (!farmerService.DoesFarmerHaveDairy(ctx.User.Id))
                    embed.AddField("Dairy",
                        "Cost 10,000 - Purchases a Dairy which can be used to make products from milk");
                embed.AddField("Oats",
                    "Cost 250 - Will provide a boost to your goats milk output next time they're milked");
                embed.AddField("Alfalfa",
                    "Cost 500 - Will give goats an exp boost when daily is used");
                embed.AddField("Dust",
                    "Cost 1,000 - Combined feed that offers both a boost to milk output and daily XP");
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
        }
    }
}
