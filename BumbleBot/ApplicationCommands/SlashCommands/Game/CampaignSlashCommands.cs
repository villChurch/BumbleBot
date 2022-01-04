using System;
using System.Threading.Tasks;
using BumbleBot.Services;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;

namespace BumbleBot.ApplicationCommands.SlashCommands.Game;

public class CampaignSlashCommands : ApplicationCommandsModule
{
    private GoatService _goatService;
    private FarmerService _farmerService;
    
    public CampaignSlashCommands(GoatService goatService, FarmerService farmerService)
    {
        _farmerService = farmerService;
        _goatService = goatService;
    }
    [SlashCommand("campaign", "Run the campaign command for a chance to level a goat")]
    public async Task CampaignCommand(InteractionContext ctx,
        [Option("goatId", "Id of the goat to run campaign with")]
        int goatId, [Option("bet", "price of campaign, defaults to 1000")] int bet = 1000)
    {
        if (!_farmerService.HasEnoughCredits(ctx.User.Id, bet))
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
            return;
        }
        await DoCampaign(ctx, goatId, bet);
    }
    
    private async Task DoCampaign(InteractionContext ctx, int goatId, int bet)
    {
        var goats = _goatService.ReturnUsersGoats(ctx.User.Id);
        if (bet != 1000)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"Campaign costs 1000 credits to enter not {bet} credits."));
            return;
        }

        if (!goats.Exists(g => g.Id == goatId))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"You do not own a goat with id {goatId}"));
            return;
        }

        _farmerService.DeductCreditsFromFarmer(ctx.User.Id, 1000);
        var goat = goats.Find(g => g.Id == goatId);
        var rnd = new Random();
        var scenarioNumber = rnd.Next(0, 40);
        int expToAdd;
        string campaignMessage;
        var loanString = "";
        var hasLoan = _farmerService.DoesFarmerHaveALoan(ctx.User.Id);
        switch (scenarioNumber)
        {
            case 0:
                if (hasLoan)
                {
                    var (repaymentAmount, loanAmount) = _farmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, 1000);
                    loanString =
                        $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                        $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 1000 - repaymentAmount);
                }
                else
                {
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 1000);
                }

                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage = $"{goat?.Name} won Grand Champion, congratulations!" +
                                  $" They earned {expToAdd} XP and a prize of 1000 credits.";
                break;
            case 1:
                if (hasLoan)
                {
                    var (repaymentAmount, loanAmount) = _farmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, 500);
                    loanString =
                        $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                        $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 500 - repaymentAmount);
                }
                else
                {
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 500);
                }

                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} won Best in Show, congratulations! They earned {expToAdd} XP and a prize of 500 credits.";
                break;
            case 2:
                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage = $"{goat?.Name} won Best of Breed, congratulations! They earned {expToAdd} XP";
                break;
            case 3:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} did really well at the show, placing in the top three. They earned {expToAdd} XP.";
                break;
            case 4:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} did their best at the show, placing in the top five. They earned {expToAdd} XP.";
                break;
            case 5:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} did their best at the show, placing in the top ten. They earned {expToAdd} XP.";
                break;
            case 6:
                expToAdd = rnd.Next(0, 250) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} tried their best at the show, but did not place. They earned {expToAdd} XP.";
                break;
            case 7:
                expToAdd = rnd.Next(0, 60) + 1;
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage =
                    $"{goat?.Name} tried their best at the show, but became a little upset and did not place. " +
                    $"They lost {expToAdd} XP from the stress.";
                break;
            case 8:
                expToAdd = rnd.Next(60, 150) + 1;
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage =
                    $"{goat?.Name} tried their best at the show, but became agitated and did not place. They lost {expToAdd} XP from the stress.";
                break;
            case 9:
                if (hasLoan)
                {
                    var (repaymentAmount, loanAmount) = _farmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, 1000);
                    loanString =
                        $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                        $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 1000 - repaymentAmount);
                }
                else
                {
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 1000);
                }

                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} strutted their stuff at the show and was Grand Champion, after being praised by the judge." +
                    $" They earned {expToAdd} XP and a prize of 1000 credits.";
                break;
            case 10:
                if (hasLoan)
                {
                    var (repaymentAmount, loanAmount) = _farmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, 500);
                    loanString =
                        $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                        $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";   
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 500 - repaymentAmount);
                }
                else
                {
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 500);
                }

                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} wasn't quite sure of themself, but won Best Udder! They earned {expToAdd} XP and a prize of 500 credits.";
                break;
            case 11:
                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} was full of confidence and easily took the Best of Breed ribbon. They earned {expToAdd} XP";
                break;
            case 12:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} did really well at the show despite refusing to eat, placing in the top three. They earned {expToAdd} XP.";
                break;
            case 13:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} was on their best behavior, but was too nervous to win, placing in the top five. They earned {expToAdd} XP.";
                break;
            case 14:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} was very naughty in the ring but was complimented by the judge, placing in the top ten. They earned {expToAdd} XP.";
                break;
            case 15:
                expToAdd = rnd.Next(0, 250) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} balked at the ring entrance and fought the lead, and did not place. Despite this they earned {expToAdd} XP.";
                break;
            case 16:
                expToAdd = rnd.Next(0, 60) + 1;
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage =
                    $"{goat?.Name} wanted to do their best, but got an upset stomach and developed scours last minute. " +
                    $"They lost {expToAdd} XP from the stress.";
                break;
            case 17:
                expToAdd = rnd.Next(60, 150) + 1;
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage =
                    $"{goat?.Name} was scared of a mean neighboring doe in the ring and couldn't settle. " +
                    $"They did not place and lost {expToAdd} XP due to fright.";
                break;
            case 18:
                expToAdd = rnd.Next(150, 250) + 1;
                _farmerService.DeductCreditsFromFarmer(ctx.User.Id, 100);
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage =
                    $"{goat?.Name} somehow got their head stuck in your goat trailer during transport to the show and needed rescue." +
                    $" They lost {expToAdd} XP from the stress and cost you 100 credits in trailer repairs.";
                break;
            case 19:
                if (hasLoan)
                {
                    var (repaymentAmount, loanAmount) = _farmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, 1000);
                    loanString =
                        $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                        $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";   
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 1000 - repaymentAmount);
                }
                else
                {
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 1000);
                }

                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} did not set a single hoof wrong, and stole the entire show as Grand Champion." +
                    $" They earned {expToAdd} XP and a prize of 1000 credits.";
                break;
            case 20:
                if (hasLoan)
                {
                    var (repaymentAmount, loanAmount) = _farmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, 500);
                    loanString =
                        $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                        $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";   
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 500 - repaymentAmount);
                }
                else
                {
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 500);
                }

                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} had a rough start, but went on to win over several experienced does. " +
                    $"They earned {expToAdd} XP and a prize of 500 credits.";
                break;
            case 21:
                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    "{goat?.Name} stopped to pee multiple times during their show, but the judge was indulgent. They earned {expToAdd} XP";
                break;
            case 22:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} enjoyed the show and even placed in the top three. They earned {expToAdd} XP.";
                break;
            case 23:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} was intimidated by the other goats but did their best for you, placing in the top five. They earned {expToAdd} XP.";
                break;
            case 24:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} stood pretty as a picture and walked nicely on the lead, placing in the top ten. They earned {expToAdd} XP.";
                break;
            case 25:
                expToAdd = rnd.Next(0, 250) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} bit the judge and was cast from the ring. Despite this they earned {expToAdd} XP and was pleased.";
                break;
            case 26:
                expToAdd = rnd.Next(0, 60) + 1;
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage =
                    $"{goat?.Name} was so frightened in the ring they flopped to the ground and made you drag them out. " +
                    $"They lost {expToAdd} XP and a good bit of self respect.";
                break;
            case 27:
                expToAdd = rnd.Next(60, 150) + 1;
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage =
                    $"{goat?.Name} was ready for the show, but you trimmed their hooves too short and they developed a limp." +
                    $" They lost {expToAdd} XP from the temporary soreness.";
                break;
            case 28:
                expToAdd = rnd.Next(150, 250) + 1;
                _farmerService.DeductCreditsFromFarmer(ctx.User.Id, 100);
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage = $"{goat?.Name} escaped their holding pen and caused havoc in the parking lot." +
                                  $" They lost {expToAdd} XP from being chased and cost you 100 credits in repair fees.";
                break;
            case 29:
                if (hasLoan)
                {
                    var (repaymentAmount, loanAmount) = _farmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, 1000);
                    loanString =
                        $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                        $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";   
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 1000 - repaymentAmount);
                }
                else
                {
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 1000);
                }
                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} was greatly admired by the judge and made the other farmers jealous, winning Grand Champion." +
                    $" They earned {expToAdd} XP and a prize of 1000 credits.";
                break;
            case 30:
                if (hasLoan)
                {
                    var (repaymentAmount, loanAmount) = _farmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, 500);
                    loanString =
                        $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                        $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 500 - repaymentAmount);
                }
                else
                {
                    _farmerService.AddCreditsToFarmer(ctx.User.Id, 500);
                }
                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} wasn't sure at first, but soon was owning the ring to win Best in Show. " +
                    $"They earned {expToAdd} XP and a prize of 500 credits.";
                break;
            case 31:
                expToAdd = rnd.Next(600, 1000) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} put their trust in you and followed your lead, straight to Champion status. They earned {expToAdd} XP";
                break;
            case 32:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} had a great time at the show, bouncing through the ring, and was Reserve Champion. They earned {expToAdd} XP.";
                break;
            case 33:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} did not do as well in the looks department, but they outshone the other does with their beautiful udder. " +
                    $"They earned {expToAdd} XP.";
                break;
            case 34:
                expToAdd = rnd.Next(250, 600) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} tried to bite every goat that came near them, but that fire helped in the show ring. They earned {expToAdd} XP.";
                break;
            case 35:
                expToAdd = rnd.Next(0, 250) + 1;
                _goatService.GiveGoatExp(goat, expToAdd);
                campaignMessage =
                    $"{goat?.Name} tried to run the entire time in the ring and would not stand still. Despite this they earned {expToAdd} XP.";
                break;
            case 36:
                expToAdd = rnd.Next(0, 60) + 1;
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage =
                    $"{goat?.Name} got bit by a neighboring goat and refused to finish the show, sulking in their pen " +
                    $"They lost {expToAdd} XP from the incident.";
                break;
            case 37:
                expToAdd = rnd.Next(60, 150) + 1;
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage =
                    $"{goat?.Name} found the noise and bustle of the show overwhelming and screamed the entire time. " +
                    $"They lost {expToAdd} XP - and their voice.";
                break;
            case 38:
                expToAdd = rnd.Next(150, 250) + 1;
                _farmerService.DeductCreditsFromFarmer(ctx.User.Id, 100);
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage =
                    $"{goat?.Name} reached through the bars of their pen, tore open the bag of travel feed, and ate it all." +
                    $" They lost {expToAdd} XP due to a bad stomachache and cost you 100 credits in lost feed.";
                break;
            default:
                expToAdd = rnd.Next(150, 250) + 1;
                _farmerService.DeductCreditsFromFarmer(ctx.User.Id, 100);
                _goatService.GiveGoatExp(goat, expToAdd * -1);
                campaignMessage =
                    "{goat?.Name} did not enjoy the show at all, became very sick and was scratched." +
                    $" They lost {expToAdd} XP from the stress and cost you 100 credits in veterinary fees.";
                break;
        }

        var negResult = _goatService.CheckForNegativeExp(goatId, ctx.User.Id);
        if (!string.IsNullOrEmpty(negResult))
        {
            campaignMessage =
                $"Oh no! {goat?.Name} has lost all of their XP through the stress of showing. " +
                $"They became ill, and passed away shortly after returning home. Rest in peace {goat?.Name}.";
        }
        else
        {
            _goatService.UpdateGoatImagesForKidsThatAreAdults(ctx.User.Id);
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"{campaignMessage} {loanString}"));
    }
}