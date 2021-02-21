using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Text;
using BumbleBot.Models;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace BumbleBot.Commands.Game
{
    [Group("show")]
    public class ShowCommands :  BaseCommandModule
    {

        private GoatService GoatService { get; }
        private FarmerService FarmerService { get; }

        public ShowCommands(GoatService goatService, FarmerService farmerService)
        {
            GoatService = goatService;
            FarmerService = farmerService;
        }

        [GroupCommand]
        public async Task DisplayShowGoats(CommandContext ctx)
        {
            var goatIds = GoatService.GetAllShowGoatIds().Item1;
            if (goatIds.Count < 1)
            {
                await ctx.Channel.SendMessageAsync("There are no goats in the current show").ConfigureAwait(false);
            }
            else
            {
                var showGoats = GoatService.GetAllGoats().Where(x => goatIds.Contains(x.Id)).ToList();
                var showGoatsDictionary = GoatService.GetAllShowGoatIds().Item2;
                var url = "http://williamspires.com/";
                var pages = new List<Page>();
                var interactivity = ctx.Client.GetInteractivity();
                foreach (var goat in showGoats)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"Contestant number {showGoatsDictionary[goat.Id]}",
                        ImageUrl = url + goat.FilePath.Replace(" ", "%20")
                    };
                    embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed).Replace("_", " "), true);
                    embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                    var page = new Page
                    {
                        Embed = embed
                    };
                    pages.Add(page);
                }
                _ = Task.Run(async () => await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false));
            }
        }
        
        [Command("add")]
        [Description("Start a dialogue to add your goat to the current showing competition")]
        public async Task AddGoatToShowingCompetition(CommandContext ctx, int goatId)
        {
            await ctx.Message.DeleteAsync().ConfigureAwait(false);
            if (GoatService.DoesUserHaveGoatShowing(ctx.User.Id))
            {
                await new DiscordMessageBuilder()
                    .WithContent("You already have a goat in the current show")
                    .SendAsync(ctx.Channel);
            }
            else
            {
                var goats = GoatService.ReturnUsersGoats(ctx.User.Id);
                var goat = goats.Find(x => x.Id == goatId);
                if (null == goat)
                {
                    await ctx.Channel.SendMessageAsync($"Could not find a goat with id {goatId}").ConfigureAwait(false);
                }
                else
                {
                    GoatService.AddGoatToShow(goatId, ctx.User.Id);
                    await ctx.Channel.SendMessageAsync($"{goat.Name} has been added to the show").ConfigureAwait(false);
                }
            }
        }

        [Command("vote")]
        [Description("Vote for a goat in the show by using it's contestant id")]
        public async Task VoteForGoatInShow(CommandContext ctx, int contestantId)
        {
            if (!GoatService.DoesShowGoatExistByContestantId(contestantId))
            {
                await ctx.Channel
                    .SendMessageAsync($"There no goat in the current contestant with a contestant id of {contestantId}")
                    .ConfigureAwait(false);
            }
            else
            {
                GoatService.IncreaseContestantsVote(contestantId, 1);
                await ctx.Channel.SendMessageAsync($"One vote has been added for contestant number {contestantId}")
                    .ConfigureAwait(false);
            }
        }

        [Command("votes")]
        [Description("Show vote count for all goats")]
        [RequirePermissions(Permissions.KickMembers)]
        public async Task ShowVotesForGoats(CommandContext ctx)
        {
            var results = GoatService.GetContestsVotes();
            var sb = new StringBuilder();
            foreach (var key in results.Keys)
            {
                sb.AppendLine($"Contest number {key} has {results[key]} votes.");
            }

            await ctx.Channel.SendMessageAsync(sb.ToString()).ConfigureAwait(false);
        }

        [Command("end")]
        [Description("Ends the current show contest")]
        [RequirePermissions(Permissions.KickMembers)]
        public async Task EndShowContest(CommandContext ctx)
        {
            var winnerInfo = GoatService.GetShowWinner();
            var numberOfContestants = GoatService.GetNumberOfShowContestants();
            var numberOfCreditsForWin = numberOfContestants * 250;
            GoatService.ResetContest();
            FarmerService.AddCreditsToFarmer(ctx.User.Id, numberOfCreditsForWin);
            var winner = await ctx.Guild.GetMemberAsync(winnerInfo.Item2).ConfigureAwait(false);
            await ctx.Channel.SendMessageAsync($"Congrats {winner.DisplayName} you have won the current competition and " +
                                               $"collected your prize money of {numberOfCreditsForWin}").ConfigureAwait(false);
        }
    }
}