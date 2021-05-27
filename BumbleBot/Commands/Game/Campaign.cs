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
            var rnd = new Random();
            var scenarioNumber = rnd.Next(0, 40);
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
                case 9:
                    farmerService.AddCreditsToFarmer(ctx.User.Id, 1000);
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel.SendMessageAsync($"{goat?.Name} strutted her stuff at the show and was Grand Champion, after being praised by the judge." +
                                                       $" She earned {expToAdd} XP and a prize of 1000 credits.")
                        .ConfigureAwait(false);
                    break;
                case 10:
                    farmerService.AddCreditsToFarmer(ctx.User.Id, 500);
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} wasn't quite sure of herself, but won Best Udder! She earned {expToAdd} XP and a prize of 500 credits.")
                        .ConfigureAwait(false);
                    break;
                case 11:
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync($"{goat?.Name} was full of confidence and easily took the Best of Breed ribbon. She earned {expToAdd} XP")
                        .ConfigureAwait(false);
                    break;
                case 12:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} did really well at the show despite refusing to eat, placing in the top three. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 13:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} was on her best behavior, but was too nervous to win, placing in the top five. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 14:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel.SendMessageAsync(
                            $"{goat?.Name} was very naughty in the ring but was complimented by the judge, placing in the top ten. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 15:
                    expToAdd = rnd.Next(0, 250) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} balked at the ring entrance and fought the lead, and did not place. Despite this she earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 16:
                    expToAdd = rnd.Next(0, 60) + 1;
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} wanted to do her best, but got an upset stomach and developed scours last minute. " +
                            $"She lost {expToAdd} XP from the stress.")
                        .ConfigureAwait(false);
                    break;
                case 17:
                    expToAdd = rnd.Next(60, 150) + 1;
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} was scared of a mean neighboring doe in the ring and couldn't settle. She did not place and lost {expToAdd} XP due to fright.")
                        .ConfigureAwait(false);
                    break;
                case 18:
                    expToAdd = rnd.Next(150, 250) + 1;
                    farmerService.DeductCreditsFromFarmer(ctx.User.Id, 100);
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} somehow got her head stuck in your goat trailer during transport to the show and needed rescue." +
                            $" She lost {expToAdd} XP from the stress and cost you 100 credits in trailer repairs.")
                        .ConfigureAwait(false);
                    break;
                case 19:
                    farmerService.AddCreditsToFarmer(ctx.User.Id, 1000);
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel.SendMessageAsync($"{goat?.Name} did not set a single hoof wrong, and stole the entire show as Grand Champion." +
                                                       $" She earned {expToAdd} XP and a prize of 1000 credits.")
                        .ConfigureAwait(false);
                    break;
                case 20:
                    farmerService.AddCreditsToFarmer(ctx.User.Id, 500);
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} had a rough start, but went on to win over several experienced does. She earned {expToAdd} XP and a prize of 500 credits.")
                        .ConfigureAwait(false);
                    break;
                case 21:
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync($"{goat?.Name} stopped to pee multiple times during her show, but the judge was indulgent. She earned {expToAdd} XP")
                        .ConfigureAwait(false);
                    break;
                case 22:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} enjoyed the show and even placed in the top three. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 23:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} was intimidated by the other goats but did her best for you, placing in the top five. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 24:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel.SendMessageAsync(
                            $"{goat?.Name} stood pretty as a picture and walked nicely on the lead, placing in the top ten. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 25:
                    expToAdd = rnd.Next(0, 250) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} bit the judge and was cast from the ring. Despite this she earned {expToAdd} XP and was pleased.")
                        .ConfigureAwait(false);
                    break;
                case 26:
                    expToAdd = rnd.Next(0, 60) + 1;
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} was so frightened in the ring she flopped to the ground and made you drag her out. " +
                            $"She lost {expToAdd} XP and a good bit of self respect.")
                        .ConfigureAwait(false);
                    break;
                case 27:
                    expToAdd = rnd.Next(60, 150) + 1;
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} was ready for the show, but you trimmed her hooves too short and she developed a limp. She lost {expToAdd} XP from the temporary soreness.")
                        .ConfigureAwait(false);
                    break;
                case 28:
                    expToAdd = rnd.Next(150, 250) + 1;
                    farmerService.DeductCreditsFromFarmer(ctx.User.Id, 100);
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} escaped her holding pen and caused havoc in the parking lot." +
                            $" She lost {expToAdd} XP from being chased and cost you 100 credits in repair fees.")
                        .ConfigureAwait(false);
                    break;
                case 29:
                    farmerService.AddCreditsToFarmer(ctx.User.Id, 1000);
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel.SendMessageAsync($"{goat?.Name} was greatly admired by the judge and made the other farmers jealous, winning Grand Champion." +
                                                       $" She earned {expToAdd} XP and a prize of 1000 credits.")
                        .ConfigureAwait(false);
                    break;
                case 30:
                    farmerService.AddCreditsToFarmer(ctx.User.Id, 500);
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} wasn't sure at first, but soon was owning the ring to win Best in Show. She earned {expToAdd} XP and a prize of 500 credits.")
                        .ConfigureAwait(false);
                    break;
                case 31:
                    expToAdd = rnd.Next(600, 1000) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync($"{goat?.Name} put her trust in you and followed your lead, straight to Champion status. She earned {expToAdd} XP")
                        .ConfigureAwait(false);
                    break;
                case 32:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} had a great time at the show, bouncing through the ring, and was Reserve Champion. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 33:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} did not do as well in the looks department, but she outshone the other does with her beautiful udder. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 34:
                    expToAdd = rnd.Next(250, 600) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel.SendMessageAsync(
                            $"{goat?.Name} tried to bite every goat that came near her, but that fire helped in the show ring. She earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 35:
                    expToAdd = rnd.Next(0, 250) + 1;
                    goatService.GiveGoatExp(goat, expToAdd);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} tried to run the entire time in the ring and would not stand still. Despite this she earned {expToAdd} XP.")
                        .ConfigureAwait(false);
                    break;
                case 36:
                    expToAdd = rnd.Next(0, 60) + 1;
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} got bit by a neighboring goat and refused to finish the show, sulking in her pen " +
                            $"She lost {expToAdd} XP from the incident.")
                        .ConfigureAwait(false);
                    break;
                case 37:
                    expToAdd = rnd.Next(60, 150) + 1;
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} found the noise and bustle of the show overwealming and screamed the entire time. She lost {expToAdd} XP - and her voice.")
                        .ConfigureAwait(false);
                    break;
                case 38:
                    expToAdd = rnd.Next(150, 250) + 1;
                    farmerService.DeductCreditsFromFarmer(ctx.User.Id, 100);
                    goatService.GiveGoatExp(goat, expToAdd * -1);
                    await ctx.Channel
                        .SendMessageAsync(
                            $"{goat?.Name} reached through the bars of her pen, tore open the bag of travel feed, and ate it all." +
                            $" She lost {expToAdd} XP due to a bad stomachache and cost you 100 credits in lost feed.")
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