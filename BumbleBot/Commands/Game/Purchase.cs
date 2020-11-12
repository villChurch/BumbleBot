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

        [Command ("graze")]
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
    }
}
