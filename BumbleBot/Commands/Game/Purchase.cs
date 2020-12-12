using System;
using System.Threading.Tasks;
using BumbleBot.Models;
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
        FarmerService farmerService;
        DBUtils dBUtils = new DBUtils();
        public Purchase(FarmerService farmerService)
        {
            this.farmerService = farmerService;
        }

        [Command("shelter")]
        public async Task BuyKiddingBarn(CommandContext ctx, int upgradePrice)
        {
            if (farmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
            {
                await ctx.Channel.SendMessageAsync("You already own a kidding pen.").ConfigureAwait(false);
            }
            else if (upgradePrice != 5000)
            {
                await ctx.Channel.SendMessageAsync("You have not entered the correct price for the kidding pen");
            }
            else
            {
                Farmer farmer = farmerService.ReturnFarmerInfo(ctx.User.Id);
                using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "insert into kiddingpens (ownerId) values (?discordId)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                using(MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "update farmers set credits = ?credits where DiscordID = ?discordId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.credits - 5000;
                    command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                await ctx.Channel.SendMessageAsync("You have successfully brought a kidding pen.").ConfigureAwait(false);
            }
        }

        [Command ("barn")]
        public async Task UpgradeBarn(CommandContext ctx, int upgradePrice)
        {
            Farmer farmer = farmerService.ReturnFarmerInfo(ctx.User.Id);

            if (farmer.barnspace == 0)
            {
                await ctx.Channel.SendMessageAsync("Looks like you don't have a character yet. Use `create` to make one.")
                    .ConfigureAwait(false);
            }
            else
            {
                int barnUpgradeCost = (farmer.barnspace + 10) * 100;
                if (upgradePrice >= barnUpgradeCost && farmer.credits >= upgradePrice)
                {
                    using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "UPDATE farmers SET barnsize = ?barnsize, credits = ?credits WHERE DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?barnsize", MySqlDbType.Int32).Value = farmer.barnspace + 10;
                        command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.credits - barnUpgradeCost;
                        command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    await ctx.Channel.SendMessageAsync("Barn has now been upgraded and can hold 10 more goats!").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync("Either the upgrade price you entered is not enough or you do not have enough credits!")
                        .ConfigureAwait(false);
                }
            }
        }

        [Command ("pasture")]
        public async Task UpgradeGrazing(CommandContext ctx, int upgradePrice)
        {
            Farmer farmer = farmerService.ReturnFarmerInfo(ctx.User.Id);
            if (farmer.barnspace == 0)
            {
                await ctx.Channel.SendMessageAsync("Looks like you don't have a character yet. Use `create` to make one.")
                    .ConfigureAwait(false);
            }
            else
            {
                int grazingUpgradeCost = (farmer.grazingspace + 10) * 100;
                if (upgradePrice >= grazingUpgradeCost && farmer.credits >= upgradePrice)
                {
                    using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "UPDATE farmers SET grazesize = ?grazesize, credits = ?credits WHERE DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?grazesize", MySqlDbType.Int32).Value = farmer.grazingspace + 10;
                        command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.credits - grazingUpgradeCost;
                        command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    await ctx.Channel.SendMessageAsync("Pasture space has been expanded and can now feed 10 more goats!").ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync("Either the upgrade price you entered is not enough or you do not have enough credits!")
                        .ConfigureAwait(false);


                }
            }
        }

        [Command("oats")]
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
                    bool hasOats = false;
                    using (MySqlConnection conncetion = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "select oats from farmers where DiscordID = ?discordID";
                        var command = new MySqlCommand(query, conncetion);
                        command.Parameters.Add("?discordID", MySqlDbType.VarChar).Value = ctx.User.Id;
                        conncetion.Open();
                        var reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                hasOats = reader.GetBoolean("oats");
                            }
                        }
                        reader.Close();
                    }
                    if (hasOats)
                    {
                        await ctx.Channel.SendMessageAsync("You already have oats. Please use your current batch before buying more.")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        Farmer farmer = farmerService.ReturnFarmerInfo(ctx.User.Id);
                        if (farmer.credits >= 250)
                        {
                            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                            {
                                string query = "update farmers set oats = 1, credits = ?credits where DiscordID = ?discordID";
                                var command = new MySqlCommand(query, connection);
                                command.Parameters.Add("?discordID", MySqlDbType.VarChar).Value = ctx.User.Id;
                                command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.credits - 250;
                                connection.Open();
                                command.ExecuteNonQuery();
                            }
                            await ctx.Channel.SendMessageAsync("Oats have been purchased and will be used next time you milk your goats.")
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await ctx.Channel.SendMessageAsync("You do not have enough credits to purchase more oats.").ConfigureAwait(false);
                        }
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
