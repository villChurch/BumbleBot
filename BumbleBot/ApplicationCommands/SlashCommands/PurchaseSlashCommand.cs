using System;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace BumbleBot.ApplicationCommands.SlashCommands;

[SlashCommandGroup("purchase", "purchase items from the farm shop and other farm equipment")]
public class PurchaseSlashCommand : ApplicationCommandsModule
{
    private readonly DbUtils dBUtils = new ();
    private readonly FarmerService farmerService;
    private readonly PerkService perkService;
    
    public PurchaseSlashCommand(FarmerService farmerService, PerkService perkService)
    {
        this.farmerService = farmerService;
        this.perkService = perkService;
    }
    
        [SlashCommand("dairy", "purchase yourself a dairy")]
        [IsUserAvailableSlash]
        public async Task BuyDairy(InteractionContext ctx, [Option("cost", "cost of update")] int upgradePrice)
        {
            int dairyPrice = 10000;
            var userPerks = await perkService.GetUsersPerks(ctx.User.Id);
            if (userPerks.Any(perk => perk.id == 14))
            {
                dairyPrice = (int) Math.Ceiling(dairyPrice * 0.9);
            }
            if (farmerService.DoesFarmerHaveDairy(ctx.User.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You already own a dairy.")
                        .AsEphemeral(true));
            }
            else if (upgradePrice != dairyPrice)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent("You have not entered the correct price for the shelter."));
            }
            else if (!farmerService.HasEnoughCredits(ctx.User.Id, upgradePrice))
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Lack of funds",
                    Description = "You do not have enough credits to perform this action",
                    Color = DiscordColor.Aquamarine
                };
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed));
            }
            else
            {
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "insert into dairy (ownerId) values (?discordId)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                farmerService.DeductCreditsFromFarmer(ctx.User.Id, dairyPrice);

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent("You have successfully brought a dairy."));
            }
        }

        [SlashCommand("shelter", "purchase a shelter")]
        [IsUserAvailableSlash]
        public async Task BuyKiddingBarn(InteractionContext ctx, [Option("cost", "cost of update")] int upgradePrice)
        {
            int shelterPrice = 5000;
            var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
            if (usersPerks.Any(perk => perk.id == 14))
            {
                shelterPrice = (int) Math.Ceiling(shelterPrice * 0.9);
            }
            if (farmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You already own a shelter."));
            }
            else if (upgradePrice != shelterPrice)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent("You have not entered the correct price for the shelter."));
            }
            else if (!farmerService.HasEnoughCredits(ctx.User.Id, upgradePrice))
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Lack of funds",
                    Description = "You do not have enough credits to perform this action",
                    Color = DiscordColor.Aquamarine
                };
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed));
            }
            else
            {
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "insert into kiddingpens (ownerId) values (?discordId)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                farmerService.DeductCreditsFromFarmer(ctx.User.Id, shelterPrice);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent("You have successfully brought a shelter."));
            }
        }

        [SlashCommand("barn", "purchase more barn space")]
        [IsUserAvailableSlash]
        public async Task UpgradeBarn(InteractionContext ctx, [Option("cost", "cost of update")] int upgradePrice)
        {
            var farmer = farmerService.ReturnFarmerInfo(ctx.User.Id);

            if (farmer.Barnspace == 0)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent("Looks like you don't have a character yet. Use `create` to make one."));
            }
            else if (!farmerService.HasEnoughCredits(ctx.User.Id, upgradePrice))
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Lack of funds",
                    Description = "You do not have enough credits to perform this action",
                    Color = DiscordColor.Aquamarine
                };
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed));
            }
            else
            {
                var barnUpgradeCost = (farmer.Barnspace + 10) * 100;
                var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
                if (usersPerks.Any(perk => perk.id == 9))
                {
                    barnUpgradeCost = (int) Math.Ceiling(barnUpgradeCost * 0.75);
                }
                else if (usersPerks.Any(perk => perk.id == 4))
                {
                    barnUpgradeCost = (int) Math.Ceiling(barnUpgradeCost * 0.9);
                }

                if (usersPerks.Any(perk => perk.id == 14))
                {
                    barnUpgradeCost = (int) Math.Ceiling(barnUpgradeCost * 0.9);
                }
                if (upgradePrice >= barnUpgradeCost)
                {
                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
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

                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("Barn has now been upgraded and can hold 10 more goats!"));
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("Either the upgrade price you entered is not enough or you do not have enough credits!"));
                }
            }
        }

        [SlashCommand("pasture", "purchase more pasture space")]
        [IsUserAvailableSlash]
        public async Task UpgradeGrazing(InteractionContext ctx, [Option("cost", "cost of update")] int upgradePrice)
        {
            var farmer = farmerService.ReturnFarmerInfo(ctx.User.Id);
            if (farmer.Barnspace == 0)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("Looks like you don't have a character yet. Use `create` to make one."));
            }
            else if (!farmerService.HasEnoughCredits(ctx.User.Id, upgradePrice))
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Lack of funds",
                    Description = "You do not have enough credits to perform this action",
                    Color = DiscordColor.Aquamarine
                };
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed));
            }
            else
            {
                var grazingUpgradeCost = (farmer.Grazingspace + 10) * 100;
                var userPerks = await perkService.GetUsersPerks(ctx.User.Id);
                if (userPerks.Any(perk => perk.id == 11))
                {
                    grazingUpgradeCost = (int) Math.Ceiling(grazingUpgradeCost * 0.75);
                }
                else if (userPerks.Any(perk => perk.id == 5))
                {
                    grazingUpgradeCost = (int) Math.Ceiling(grazingUpgradeCost * 0.9);
                }

                if (userPerks.Any(perk => perk.id == 14))
                {
                    grazingUpgradeCost = (int) Math.Ceiling(grazingUpgradeCost * 0.9);
                }
                if (upgradePrice >= grazingUpgradeCost)
                {
                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
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

                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("Pasture space has been expanded and can now feed 10 more goats!"));
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("Either the upgrade price you entered is not enough or you do not have enough credits!"));
                }
            }
        }

        [SlashCommand("alfalfa", "purchase some alfalfa")]
        [IsUserAvailableSlash]
        public async Task PurchaseAlfalfa(InteractionContext ctx, [Option("cost", "cost of update")] int cost)
        {
            var alfalfaCost = 500;
            var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
            if (usersPerks.Any(perk => perk.id == 14))
            {
                alfalfaCost = (int) Math.Ceiling(alfalfaCost * 0.9);
            }
            if (cost < alfalfaCost)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"Alfalfa costs {alfalfaCost} credits."));
            }
            else if (!farmerService.HasEnoughCredits(ctx.User.Id, cost))
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Lack of funds",
                    Description = "You do not have enough credits to perform this action",
                    Color = DiscordColor.Aquamarine
                };
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed));
            }
            else
            {
                if (farmerService.DoesFarmerHaveAlfalfa(ctx.User.Id))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("You already have alfalfa."));
                }
                else
                {
                    farmerService.AddAlfalfaToFarmer(ctx.User.Id);
                    farmerService.DeductCreditsFromFarmer(ctx.User.Id, alfalfaCost);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("Alfalfa has been purchased for 500 credits and will be used next time you run daily."));
                }
            }
        }

        [SlashCommand("dust", "purchase some dust")]
        [IsUserAvailableSlash]
        public async Task PurchaseDust(InteractionContext ctx, [Option("cost", "cost of update")] int cost)
        {
            var dustCost = 1000;
            var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
            if (usersPerks.Any(perk => perk.id == 14))
            {
                dustCost = (int) Math.Ceiling(dustCost * 0.9);
            }
            if (cost != dustCost)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"Dust costs {dustCost} credits."));
            }
            else if (!farmerService.HasEnoughCredits(ctx.User.Id, cost))
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Lack of funds",
                    Description = "You do not have enough credits to perform this action",
                    Color = DiscordColor.Aquamarine
                };
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed));
            }
            else if (farmerService.DoesFarmerHaveOatsOrAlfalfa(ctx.User.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("You cannot purchase dust while you have unused alfalfa or oats. Please try again after using them."));
            }
            else
            {
                farmerService.AddAlfalfaToFarmer(ctx.User.Id);
                farmerService.AddOatsToFarmer(ctx.User.Id);
                farmerService.DeductCreditsFromFarmer(ctx.User.Id, dustCost);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("Dust has been purchased and will be used next time you milk and run your daily."));
            }
        }
        
        [SlashCommand("oats", "purchase some oats")]
        [IsUserAvailableSlash]
        public async Task PurchaseOats(InteractionContext ctx, [Option("cost", "cost of update")] int cost)
        {
            try
            {
                var oatCost = 250;
                var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
                if (usersPerks.Any(perk => perk.id == 14))
                {
                    oatCost = (int) Math.Ceiling(oatCost * 0.9);
                }
                if (cost < oatCost)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("Oats cost {oatCost} credits."));
                }
                else if (!farmerService.HasEnoughCredits(ctx.User.Id, cost))
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Lack of funds",
                        Description = "You do not have enough credits to perform this action",
                        Color = DiscordColor.Aquamarine
                    };
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .AddEmbed(embed));
                }
                else
                {
                    if (farmerService.DoesFarmerHaveOats(ctx.User.Id))
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                .WithContent("You already have oats. Please use your current batch before buying more."));
                    }
                    else
                    {
                        farmerService.AddOatsToFarmer(ctx.User.Id);
                        farmerService.DeductCreditsFromFarmer(ctx.User.Id, oatCost);
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                .WithContent("Oats have been purchased and will be used next time you milk your goats."));
                    }
                }
            }
            catch (Exception ex)
            {
                ctx.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                    ctx.User.Username, ctx.Interaction.Data?.Name ?? "<unknown command>",
                    ex.GetType(), ex.Message);
            }
        }
}