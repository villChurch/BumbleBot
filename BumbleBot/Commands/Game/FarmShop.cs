﻿using System;
using System.Threading.Tasks;
using BumbleBot.Models;
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
                Title = "Welcome to the Shop",
                Description = "Here you will find a list of purchaseable items. To obtain them use `purchase {itemName} {itemCost}`.",
                Color = DiscordColor.Aquamarine
            };

            Farmer currentFarmer = farmerService.ReturnFarmerInfo(ctx.User.Id);

            if (String.IsNullOrEmpty(currentFarmer.barnspace.ToString()) || currentFarmer.barnspace == 0)
            {
                await ctx.Channel.SendMessageAsync("Looks like you don't have a character yet, use the `create` command to start.")
                    .ConfigureAwait(false);
            }
            else
            {
                int barnCost = (currentFarmer.barnspace + 10) * 100;
                int grazeCost = (currentFarmer.grazingspace + 10) * 100;

                embed.AddField("Barn", $"Cost {barnCost} - Will provide 10 extra stalls");
                embed.AddField("Pasture", $"Cost {grazeCost} - Will provide 10 extra pasture space");
                if (!farmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    embed.AddField("Shelter", "Cost 5000 - Purchases a kidding pen which adds the ability to breed goats");
                }
                embed.AddField("Oats", "Cost 250 - Will provide a boost to your goats milk output next time they're milked");
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
        }
    }
}
