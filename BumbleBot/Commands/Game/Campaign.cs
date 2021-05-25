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
            if (bet != 100)
            {
                await ctx.Channel.SendMessageAsync($"Campaign costs 100 credits to enter not {bet} credits.")
                    .ConfigureAwait(false);
                return;
            }
            if (!goats.Exists(g => g.Id == goatId))
            {
                await ctx.Channel.SendMessageAsync($"You do not own a goat with id {goatId}")
                    .ConfigureAwait(false);
                return;
            }
            var goat = goats.Find(g => g.Id == goatId);
            Random rnd = new Random();
            var scenarioNumber = rnd.Next(0, 10);
            var expToAdd = 0;
            switch (scenarioNumber)
            {
                case 0:
                    farmerService.AddCreditsToFarmer(ctx.User.Id, 1000);
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel.SendMessageAsync($"{goat?.Name} won Grand Champion, congratulations!" +
                                                       $" She earned {expToAdd} XP and a prize of 1000 credits.")
                        .ConfigureAwait(false);
                    break;
                case 1:
                    farmerService.AddCreditsToFarmer(ctx.User.Id, 500);
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} won Best in Show, congratulations! She earned {expToAdd} XP and a prize of 500 credits.")
                        .ConfigureAwait(false);
                    break;
                case 2:
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync($"{goat?.Name} won Best of Breed, congratulations! She earned {expToAdd} XP")
                        .ConfigureAwait(false);
                    break;
                case 3:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} did really well at the show, placing in the top three. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 4:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} did her best at the show, placing in the top five. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 5:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel.SendMessageAsync(
                            $"{goat?.Name} did her best at the show, placing in the top ten. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 6:
                    expToAdd = rnd.Next(0, 250) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} tried her best at the show, but did not place. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 7:
                    expToAdd = rnd.Next(0, 60) + 1;
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} tried her best at the show, but became a little upset and did not place. " +
                            $"She lost {expToAdd} XP from the stress.")
                        .ConfigureAwait(false);
                    break;
                case 8:
                    expToAdd = rnd.Next(60, 150) + 1;
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} tried her best at the show, but became agitated and did not place. She lost {expToAdd} XP from the stress.")
                        .ConfigureAwait(false);
                    break;
                default:
                    expToAdd = rnd.Next(150, 250) + 1;
                    farmerService.DeductCreditsFromFarmer(ctx.User.Id, 100);
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} did not enjoy the show at all, became very sick and was scratched." +
                            $" She lost {expToAdd} XP from the stress and cost you 100 credits in veterinary fees.")
                        .ConfigureAwait(false);
                    break;
            }
            goatService.UpdateGoatImagesForKidsThatAreAdults(ctx.User.Id);
        }
    }
}