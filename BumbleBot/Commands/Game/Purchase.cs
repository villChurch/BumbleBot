﻿using System;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.Game
{
    [Group("purchase")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class Purchase : BaseCommandModule
    {
        private readonly DbUtils dBUtils = new DbUtils();
        private readonly FarmerService farmerService;

        public Purchase(FarmerService farmerService)
        {
            this.farmerService = farmerService;
        }

        [Command("dairy")]
        [HasEnoughCredits(0)]
        public async Task BuyDairy(CommandContext ctx, int upgradePrice)
        {
            if (farmerService.DoesFarmerHaveDairy(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync("You already own a dairy.").ConfigureAwait(false);
            }
            else if (upgradePrice != 10000)
            {
                await ctx.Channel.SendMessageAsync("You have not entered the correct price for the shelter.");
            }
            else
            {
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "insert into dairy (ownerId) values (?discordId)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                farmerService.DeductCreditsFromFarmer(ctx.User.Id, 10000);

                await ctx.Channel.SendMessageAsync("You have successfully brought a dairy.").ConfigureAwait(false);
            }
        }

        [Command("shelter")]
        [HasEnoughCredits(0)]
        public async Task BuyKiddingBarn(CommandContext ctx, int upgradePrice)
        {
            if (farmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync("You already own a shelter.").ConfigureAwait(false);
            }
            else if (upgradePrice != 5000)
            {
                await ctx.Channel.SendMessageAsync("You have not entered the correct price for the shelter.");
            }
            else
            {
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "insert into kiddingpens (ownerId) values (?discordId)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                farmerService.DeductCreditsFromFarmer(ctx.User.Id, 5000);
                await ctx.Channel.SendMessageAsync("You have successfully brought a shelter.").ConfigureAwait(false);
            }
        }

        [Command("barn")]
        [HasEnoughCredits(0)]
        public async Task UpgradeBarn(CommandContext ctx, int upgradePrice)
        {
            var farmer = farmerService.ReturnFarmerInfo(ctx.User.Id);

            if (farmer.Barnspace == 0)
            {
                await ctx.Channel
                    .SendMessageAsync("Looks like you don't have a character yet. Use `create` to make one.")
                    .ConfigureAwait(false);
            }
            else
            {
                var barnUpgradeCost = (farmer.Barnspace + 10) * 100;
                if (upgradePrice >= barnUpgradeCost)
                {
                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query =
                            "UPDATE farmers SET barnsize = ?barnsize, credits = ?credits WHERE DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?barnsize", MySqlDbType.Int32).Value = farmer.Barnspace + 10;
                        command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.Credits - barnUpgradeCost;
                        command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    await ctx.Channel.SendMessageAsync("Barn has now been upgraded and can hold 10 more goats!")
                        .ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel
                        .SendMessageAsync(
                            "Either the upgrade price you entered is not enough or you do not have enough credits!")
                        .ConfigureAwait(false);
                }
            }
        }

        [Command("pasture")]
        [HasEnoughCredits(0)]
        public async Task UpgradeGrazing(CommandContext ctx, int upgradePrice)
        {
            var farmer = farmerService.ReturnFarmerInfo(ctx.User.Id);
            if (farmer.Barnspace == 0)
            {
                await ctx.Channel
                    .SendMessageAsync("Looks like you don't have a character yet. Use `create` to make one.")
                    .ConfigureAwait(false);
            }
            else
            {
                var grazingUpgradeCost = (farmer.Grazingspace + 10) * 100;
                if (upgradePrice >= grazingUpgradeCost)
                {
                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query =
                            "UPDATE farmers SET grazesize = ?grazesize, credits = ?credits WHERE DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?grazesize", MySqlDbType.Int32).Value = farmer.Grazingspace + 10;
                        command.Parameters.Add("?credits", MySqlDbType.Int32).Value =
                            farmer.Credits - grazingUpgradeCost;
                        command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    await ctx.Channel
                        .SendMessageAsync("Pasture space has been expanded and can now feed 10 more goats!")
                        .ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel
                        .SendMessageAsync(
                            "Either the upgrade price you entered is not enough or you do not have enough credits!")
                        .ConfigureAwait(false);
                }
            }
        }

        [Command("alfalfa")]
        [HasEnoughCredits(0)]
        [Description("purchase some alfalfa")]
        public async Task PurchaseAlfalfa(CommandContext ctx, int cost)
        {
            if (cost < 500)
            {
                await ctx.Channel.SendMessageAsync("Alfalfa costs 500 credits.").ConfigureAwait(false);
            }
            else
            {
                if (farmerService.DoesFarmerHaveAlfalfa(ctx.User.Id))
                {
                    await ctx.Channel.SendMessageAsync("You already have alfalfa.").ConfigureAwait(false);
                }
                else
                {
                    farmerService.AddAlfalfaToFarmer(ctx.User.Id);
                    farmerService.DeductCreditsFromFarmer(ctx.User.Id, 500);
                    await ctx.Channel
                        .SendMessageAsync(
                            "Alfalfa has been purchased for 500 credits and will be used next time you run daily.")
                        .ConfigureAwait(false);
                }
            }
        }

        [Command("dust")]
        [HasEnoughCredits(0)]
        [Description("purchase some dust")]
        public async Task PurchaseDust(CommandContext ctx, int cost)
        {
            if (cost != 1000)
            {
                await ctx.Channel.SendMessageAsync("Dust costs 1,000 credits.").ConfigureAwait(false);
            }
            else if (farmerService.DoesFarmerHaveOatsOrAlfalfa(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync($"You cannot purchase dust while you have unused alfalfa or oats. Please try again after using them.")
                    .ConfigureAwait(false);
            }
            else
            {
                farmerService.AddAlfalfaToFarmer(ctx.User.Id);
                farmerService.AddOatsToFarmer(ctx.User.Id);
                farmerService.DeductCreditsFromFarmer(ctx.User.Id, 1000);
                await ctx.Channel.SendMessageAsync(
                        $"Dust has been purchased and will be used next time you milk and run your daily.")
                    .ConfigureAwait(false);
            }
        }
        
        [Command("oats")]
        [HasEnoughCredits(0)]
        [Description("purchase some oats")]
        public async Task PurchaseOats(CommandContext ctx, int cost)
        {
            try
            {
                if (cost < 250)
                {
                    await ctx.Channel.SendMessageAsync("Oats cost 250 credits.").ConfigureAwait(false);
                }
                else
                {
                    if (farmerService.DoesFarmerHaveOats(ctx.User.Id))
                    {
                        await ctx.Channel
                            .SendMessageAsync(
                                "You already have oats. Please use your current batch before buying more.")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        farmerService.AddOatsToFarmer(ctx.User.Id);
                        await ctx.Channel
                            .SendMessageAsync(
                                "Oats have been purchased and will be used next time you milk your goats.")
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                ctx.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                    ctx.User.Username, ctx.Command?.QualifiedName ?? "<unknown command>",
                    ex.GetType(), ex.Message);
            }
        }
    }
}