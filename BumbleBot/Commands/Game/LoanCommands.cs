using System;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using MySqlConnector;

namespace BumbleBot.Commands.Game
{
    [Group("loan")]
    [IsUserAvailable]
    public class LoanCommands : BaseCommandModule
    {
        private readonly FarmerService farmerService;
        private readonly GoatService goatService;
        private DbUtils dbUtils = new();

        public LoanCommands(FarmerService farmerService, GoatService goatService)
        {
            this.farmerService = farmerService;
            this.goatService = goatService;
        }

        [GroupCommand]
        public async Task DisplayLoanStatus(CommandContext ctx)
        {
            var hasLoan = farmerService.DoesFarmerHaveALoan(ctx.User.Id);
            var barnSpace = farmerService.GetFarmersBarnSize(ctx.User.Id);
            var maxBorrowAmount = barnSpace * 1000;
            var embed = new DiscordEmbedBuilder
            {
                Title = "BumbleBot Loans",
                Description = "Here you will find information about your loans and loan status",
                Color = DiscordColor.Aquamarine
            };
            embed.AddField(new DiscordEmbedField("Loan Status",
                hasLoan ? "You currently have a loan" : "You currently don't have a loan"));
            if (!hasLoan)
            {
                embed.AddField(new DiscordEmbedField("Max loan",
                    $"The maximum you can currently borrow is {maxBorrowAmount:n0}"));
            }
            else
            {
                embed.AddField(new DiscordEmbedField("Left to repay", $"You currently have " +
                                                                      $"{farmerService.AmountLeftOnLoan(ctx.User.Id):n0} left to repay on your loan"));
            }
            await ctx.Channel.SendMessageAsync(embed).ConfigureAwait(false);
        }

        [Command("new")]
        [Description("Take out a new loan")]
        public async Task StartNewLoan(CommandContext ctx, int amount)
        {
            var hasLoan = farmerService.DoesFarmerHaveALoan(ctx.User.Id);
            var barnSpace = farmerService.GetFarmersBarnSize(ctx.User.Id);
            var maxBorrowAmount = barnSpace * 1000;
            if (hasLoan)
            {
                await ctx.RespondAsync(
                        "You already have a loan. New loans cannot be taken out till the current loan is paid off")
                    .ConfigureAwait(false);
            }
            else if (amount < 0)
            {
                await ctx.RespondAsync("You cannot take out a negative loan").ConfigureAwait(false);
            }
            else if (amount > maxBorrowAmount)
            {
                await ctx.RespondAsync($"You can only borrow up to {maxBorrowAmount:n0}").ConfigureAwait(false);
            }
            else
            {
                farmerService.AddLoanCreditsToFarmer(ctx.User.Id, amount);
                var owedAmount = (int) Math.Ceiling(amount * 1.10);
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "insert into loans (farmerId, amountOwed) values (?userId, ?amount)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("?userId", ctx.User.Id);
                    command.Parameters.AddWithValue("?amount", owedAmount);
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }

                await ctx.RespondAsync(
                        $"You have now taken out a loan of {amount:n0} and will need to repay {owedAmount:n0}")
                    .ConfigureAwait(false);
            }
        }

        [Command("repay")]
        [Description("Repay some or all of your outstanding loan")]
        [HasEnoughCredits(0)]
        public async Task RepayLoan(CommandContext ctx, int amount)
        {
            var hasLoan = farmerService.DoesFarmerHaveALoan(ctx.User.Id);
            var amountOwed = farmerService.AmountLeftOnLoan(ctx.User.Id);
            if (!hasLoan)
            {
                await ctx.RespondAsync("You currently do not have an existing loan.").ConfigureAwait(false);
            }
            else if (amount > amountOwed)
            {
                await ctx.RespondAsync(
                        $"You only owe {amountOwed:n0} which is less than the amount you are trying to repay.")
                    .ConfigureAwait(false);
            }
            else
            {
                var remainingAmount = amountOwed - amount;
                if (remainingAmount == 0)
                {
                    farmerService.RemoveLoanFromFarmer(ctx.User.Id);
                    farmerService.DeductCreditsFromFarmer(ctx.User.Id, amount);
                    await ctx.RespondAsync("Congratulations you have now paid of your loan in full")
                        .ConfigureAwait(false);
                }
                else
                {
                    farmerService.AlterLoanAmountForFarmer(ctx.User.Id, remainingAmount);
                    farmerService.DeductCreditsFromFarmer(ctx.User.Id, amount);
                    await ctx.RespondAsync(
                            $"You have paid off {amount:n0} from your loan." +
                            $" There is {remainingAmount:n0} left on your loan.")
                        .ConfigureAwait(false);
                }
            }
        }
    }
}