using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;

namespace BumbleBot.Commands.Game
{
    [Group("perks")]
    [IsUserAvailable]
    [Description("For details of perks and other perk commands")]
    public class PerkCommands : BaseCommandModule
    {
        private readonly DbUtils dbUtils = new();
        static FarmerService _farmerService;
        private static PerkService _perkService;

        public PerkCommands(FarmerService farmerService, PerkService perkService)
        {
            _farmerService = farmerService;
            _perkService = perkService;
        }
        
        [GroupCommand]
        public async Task ShowAllPerks(CommandContext ctx)
        {
            var allPerks = await _perkService.GetAllPerks().ConfigureAwait(false);
            var pages = new List<Page>();
            foreach (var perk in allPerks)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{perk.perkName}",
                    Color = DiscordColor.Aquamarine
                };
                embed.AddFields(new List<DiscordEmbedField>()
                {
                    new("Description", perk.perkBonusText),
                    new("Perk Point Cost", perk.perkCost.ToString()),
                    new("Level Unlocked", perk.levelUnlocked.ToString())
                });
                if (perk.requires != 0)
                {
                    embed.AddField(new DiscordEmbedField("Requires Perk",
                        allPerks.Find(p => p.id == perk.requires)?.perkName));
                }
                var page = new Page {Embed = embed};
                pages.Add(page);
            }
            var interactivity = ctx.Client.GetInteractivity();
            _ = Task.Run(async () => await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, null,
                PaginationBehaviour.WrapAround,ButtonPaginationBehavior.Disable,CancellationToken.None).ConfigureAwait(false));
        }

        [Command("purchase")]
        [Description("For purchasing a perk with perk points")]
        public async Task PurchasePerk(CommandContext ctx, [RemainingText] string perkName)
        {
            var allPerks = await _perkService.GetAllPerks().ConfigureAwait(false);
            var matchedPerk = allPerks.Find(perk => perk.perkName.ToLower().Equals(perkName.ToLower()));
            if (null == matchedPerk)
            {
                await ctx.RespondAsync($"Could not find a perk named {perkName}.").ConfigureAwait(false);
            } 
            else
            {
                var farmer = _farmerService.ReturnFarmerInfo(ctx.User.Id);
                var perkPoints = farmer.PerkPoints;
                var farmersPerks = await _perkService.GetUsersPerks(ctx.User.Id);
                if (perkPoints < matchedPerk.perkCost)
                {
                    await ctx.RespondAsync(
                            $"You currently have {perkPoints} perk points and {matchedPerk.perkName} costs {matchedPerk.perkCost} perk points.")
                        .ConfigureAwait(false);
                }
                else if (farmer.Level < matchedPerk.levelUnlocked)
                {
                    await ctx.RespondAsync(
                            $"You must be level {matchedPerk.levelUnlocked} to unlock this perk. You are currently level {farmer.Level}.")
                        .ConfigureAwait(false);
                }
                else if (farmersPerks.Contains(matchedPerk))
                {
                    await ctx.RespondAsync($"You already own perk {matchedPerk.perkName}.").ConfigureAwait(false);
                }
                else if (matchedPerk.requires != 0 && farmersPerks.All(perk => perk.id != matchedPerk.requires))
                {
                    await ctx.RespondAsync(
                            $"{matchedPerk.perkName} requires perk " +
                            $"{allPerks.Find(perk => perk.id == matchedPerk.requires)?.perkName} to be purchased first.")
                        .ConfigureAwait(false);
                }
                else
                {
                    await _perkService.AddPerkToUser(ctx.User.Id, matchedPerk, perkPoints);
                    await ctx.RespondAsync($"You have successfully purchased the perk {matchedPerk.perkName}.")
                        .ConfigureAwait(false);
                }
            }
        }

        [Command("reset")]
        [Description("This will reset what perks you have and give you the perk points back for a monetary cost")]
        public async Task ResetPerks(CommandContext ctx)
        {
            Farmer farmer = _farmerService.ReturnFarmerInfo(ctx.User.Id);
            if (farmer.Credits < 10000)
            {
                await ctx.RespondAsync(
                        $"You do not have enough credits to reset your perks. Perks reset costs 10,000 credits")
                    .ConfigureAwait(false);
            }
            else
            {
                var usersPerks = await _perkService.GetUsersPerks(ctx.User.Id).ConfigureAwait(false);
                int perkPointsToAdd = usersPerks.Sum(perk => perk.perkCost);
                await _perkService.RemovePerksFromUser(ctx.User.Id, usersPerks, farmer.PerkPoints)
                    .ConfigureAwait(false);
                await ctx.RespondAsync(
                        $"Your perks have been reset for 10,000 credits and {perkPointsToAdd} perk points have been added to your profile.")
                    .ConfigureAwait(false);
            }
        }

        [Command("active")]
        [Description("Displays your active perks")]
        public async Task ShowActivePerks(CommandContext ctx)
        {
            var usersPerks = await _perkService.GetUsersPerks(ctx.User.Id).ConfigureAwait(false);
            if (usersPerks == null || usersPerks.Count < 1)
            {
                await ctx.RespondAsync("You currently have no active perks").ConfigureAwait(false);
            }
            else {
                var pages = new List<Page>();
                foreach (var perk in usersPerks)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"{perk.perkName}",
                        Color = DiscordColor.Aquamarine
                    };
                    embed.AddFields(new List<DiscordEmbedField>()
                    {
                        new("Description", perk.perkBonusText),
                        new("Perk Point Cost", perk.perkCost.ToString()),
                        new("Level Unlocked", perk.levelUnlocked.ToString())
                    });
                    var page = new Page {Embed = embed};
                    pages.Add(page);
                }

                var interactivity = ctx.Client.GetInteractivity();
                _ = Task.Run(async () => await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages,
                        null,
                        PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable, CancellationToken.None)
                    .ConfigureAwait(false));
            }
        }
    }
}