using System;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.Game
{
    [Group("purchase")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class Purchase : BaseCommandModule
    {
        private readonly DBUtils dBUtils = new DBUtils();
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
                var farmer = farmerService.ReturnFarmerInfo(ctx.User.Id);
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

            if (farmer.barnspace == 0)
            {
                await ctx.Channel
                    .SendMessageAsync("Looks like you don't have a character yet. Use `create` to make one.")
                    .ConfigureAwait(false);
            }
            else
            {
                var barnUpgradeCost = (farmer.barnspace + 10) * 100;
                if (upgradePrice >= barnUpgradeCost)
                {
                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query =
                            "UPDATE farmers SET barnsize = ?barnsize, credits = ?credits WHERE DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?barnsize", MySqlDbType.Int32).Value = farmer.barnspace + 10;
                        command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.credits - barnUpgradeCost;
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
            if (farmer.barnspace == 0)
            {
                await ctx.Channel
                    .SendMessageAsync("Looks like you don't have a character yet. Use `create` to make one.")
                    .ConfigureAwait(false);
            }
            else
            {
                var grazingUpgradeCost = (farmer.grazingspace + 10) * 100;
                if (upgradePrice >= grazingUpgradeCost)
                {
                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query =
                            "UPDATE farmers SET grazesize = ?grazesize, credits = ?credits WHERE DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?grazesize", MySqlDbType.Int32).Value = farmer.grazingspace + 10;
                        command.Parameters.Add("?credits", MySqlDbType.Int32).Value =
                            farmer.credits - grazingUpgradeCost;
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
                    var hasOats = false;
                    using (var conncetion = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "select oats from farmers where DiscordID = ?discordID";
                        var command = new MySqlCommand(query, conncetion);
                        command.Parameters.Add("?discordID", MySqlDbType.VarChar).Value = ctx.User.Id;
                        conncetion.Open();
                        var reader = command.ExecuteReader();
                        if (reader.HasRows)
                            while (reader.Read())
                                hasOats = reader.GetBoolean("oats");
                        reader.Close();
                    }

                    if (hasOats)
                    {
                        await ctx.Channel
                            .SendMessageAsync(
                                "You already have oats. Please use your current batch before buying more.")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        var farmer = farmerService.ReturnFarmerInfo(ctx.User.Id);
                        using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                        {
                            var query = "update farmers set oats = 1, credits = ?credits where DiscordID = ?discordID";
                            var command = new MySqlCommand(query, connection);
                            command.Parameters.Add("?discordID", MySqlDbType.VarChar).Value = ctx.User.Id;
                            command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.credits - 250;
                            connection.Open();
                            command.ExecuteNonQuery();
                        }

                        await ctx.Channel
                            .SendMessageAsync(
                                "Oats have been purchased and will be used next time you milk your goats.")
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.StackTrace);
                Console.Out.WriteLine(ex.Message);
            }
        }
    }
}