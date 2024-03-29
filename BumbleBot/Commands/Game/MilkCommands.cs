﻿using System;
using System.Net;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.Game
{
    [Group("Milk")]
    [IsUserAvailable]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class MilkCommands : BaseCommandModule
    {
        private readonly DbUtils dbUtils = new DbUtils();

        public MilkCommands(GoatService goatService, FarmerService farmerService)
        {
            this.GoatService = goatService;
            this.farmerService = farmerService;
        }

        private GoatService GoatService { get; }
        private FarmerService farmerService { get; }

        [GroupCommand]
        public async Task MilkGoats(CommandContext ctx)
        {
            try
            {
                var uri = $"http://localhost:8080/milk/{ctx.User.Id}";
                var request = (HttpWebRequest) WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                var response = (HttpWebResponse) await request.GetResponseAsync();
                ctx.Client.Logger.Log(LogLevel.Information,
                    "{Username} milked and got the following status code {Response}",
                    ctx.User.Username, response.StatusCode);
            }
            catch (Exception ex)
            {
                ctx.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                    ctx.User.Username, ctx.Command?.QualifiedName ?? "<unknown command>",
                    ex.GetType(), ex.Message);
            }
        }

        [Command("sell")]
        public async Task SellMilk(CommandContext ctx)
        {
            try
            {
                var farmer = new Farmer();
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Select * from farmers where DiscordID = ?discordID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.User.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                        {
                            farmer.Credits = reader.GetInt32("credits");
                            farmer.Milk = reader.GetDecimal("milk");
                            farmer.DiscordId = reader.GetUInt64("DiscordID");
                        }

                    reader.Close();
                }

                if (farmer.Milk <= 0)
                {
                    await ctx.Channel.SendMessageAsync("You do not have any milk you can sell.").ConfigureAwait(false);
                }
                else
                {
                    int milkEarnings = (int) Math.Ceiling(farmer.Milk * 3);
                    var hasLoan = farmerService.DoesFarmerHaveALoan(ctx.User.Id);
                    var loanString = "";
                    if (hasLoan)
                    {
                        var (repaymentAmount, loanAmount) = farmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, milkEarnings);
                        farmer.Credits += (milkEarnings - repaymentAmount);
                        loanString =
                            $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                            $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";
                    }
                    else
                    {
                        farmer.Credits += milkEarnings;
                    }

                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                    {
                        var query = "Update farmers set milk = ?milk, credits = ?credits where DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?milk", MySqlDbType.Decimal).Value = 0;
                        command.Parameters.Add("?credits", MySqlDbType.Int32).Value = farmer.Credits;
                        command.Parameters.Add("?discordId", MySqlDbType.VarChar, 40).Value = ctx.User.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                    {
                        var query = "Delete from milkexpiry where DiscordID = ?discordID";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?discordID", MySqlDbType.VarChar).Value = ctx.User.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    await ctx.Channel.SendMessageAsync(
                        $"Congratulations {ctx.User.Mention} you have sold {farmer.Milk:n2} lbs of milk for " +
                        $"{milkEarnings:n0} credits. {loanString}").ConfigureAwait(false);
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