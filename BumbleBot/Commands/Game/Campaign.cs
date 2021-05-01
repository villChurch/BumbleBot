using System;
using System.Threading.Tasks;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using BumbleBot.Attributes;

namespace BumbleBot.Commands.Game
{
    public class Campaign : BaseCommandModule
    {
        GoatService goatService;
        FarmerService farmerService;
        public Campaign(GoatService goatService, FarmerService farmerService)
        {
            this.goatService = goatService;
            this.farmerService = farmerService;
        }

        [HasEnoughCredits(1)]
        [Command("campaign")]
        [Description("Run the campaign command for a chance to level a goat")]
        public async Task CampaignCommand(CommandContext ctx, int goatId, int bet)
        {
            var goats = goatService.ReturnUsersGoats(ctx.User.Id);
            if (!goats.Exists(g => g.Id == goatId))
            {
                await ctx.Channel.SendMessageAsync($"You do not own a goat with id {goatId}")
                    .ConfigureAwait(false);
                return;
            }
            if (bet < 100)
            {
                await ctx.Channel.SendMessageAsync("The minimum fee is 100 credits").ConfigureAwait(false);
                return;
            }

            if (bet % 100 != 0)
            {
                await ctx.Channel.SendMessageAsync("Fee must be a multiple of 100").ConfigureAwait(false);
                return;
            }
            var goat = goats.Find(g => g.Id == goatId);
            var xpTopEnd = 10 * (bet/100);
            var xpLowEnd = 3 * (bet / 100);
            Random rnd = new Random();
            var expGainedIfWin = rnd.Next(xpLowEnd, xpTopEnd + 1);
            if (rnd.Next(0, 2) == 1)
            {
                farmerService.AddCreditsToFarmer(ctx.User.Id, bet);
                goatService.GiveGoatExp(goat, expGainedIfWin);
                await ctx.Channel.SendMessageAsync(
                    $"{goat?.Name} did well at the show, and earned a prize of {bet * 2} credits." +
                    $" She also gained {expGainedIfWin} XP from the event").ConfigureAwait(false);
            }
            else
            {
                farmerService.DeductCreditsFromFarmer(ctx.User.Id, bet);
                var expLost = rnd.Next(1, 51);
                goatService.GiveGoatExp(goat, (expLost * -1));
                await ctx.Channel
                    .SendMessageAsync(
                        $"{goat?.Name} did poorly at the show, and cost you {bet} credits in show fees. " +
                        $"She also lost {expLost} XP due to the stress")
                    .ConfigureAwait(false);
            }
        }
    }
}