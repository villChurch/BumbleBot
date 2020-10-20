using System;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace BumbleBot.Commands.Game
{
    [Group("shop")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class FarmShop : BaseCommandModule
    {

        FarmerService farmerService;

        public FarmShop(FarmerService farmerService)
        {
            this.farmerService = farmerService;
        }
        [GroupCommand]
        public async Task ShopWelcomePage(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Welcome to the shop",
                Description = "Here you will find a list of purchaseable items. To purchase them run `purchase {itemName}`.",
                Color = DiscordColor.Aquamarine
            };

            Farmer currentFarmer = farmerService.ReturnFarmerInfo(ctx.User.Id);

            if (String.IsNullOrEmpty(currentFarmer.barnspace.ToString()) || currentFarmer.barnspace == 0)
            {
                await ctx.Channel.SendMessageAsync("Looks like you don't have a character yet, use the `create` command to make one")
                    .ConfigureAwait(false);
            }
            else
            {
                int barnCost = (currentFarmer.barnspace + 10) * 100;
                int grazeCost = (currentFarmer.grazingspace + 10) * 100;

                embed.AddField("Barn", $"Cost {barnCost} - Will give 10 extra space");
                embed.AddField("Graze", $"Cost {grazeCost} - Will give 10 extra grazing space");
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
        }
    }
}
