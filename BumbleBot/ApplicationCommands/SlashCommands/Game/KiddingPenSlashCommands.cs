using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.Extensions.Logging;

namespace BumbleBot.ApplicationCommands.SlashCommands.Game;

[SlashCommandGroup("shelter", "Shelter Commands")]
public class KiddingPenSlashCommands : ApplicationCommandsModule
{
    public KiddingPenSlashCommands(FarmerService farmerService, GoatService goatService, PerkService perkService)
    {
        this.FarmerService = farmerService;
        this.GoatService = goatService;
        this.perkService = perkService;
    }

    private readonly PerkService perkService;
    private FarmerService FarmerService { get; }
    private GoatService GoatService { get; }

    [SlashCommand("upgrade_info", "Show upgrade information on your shelter")]
    public async Task ShowUpgradeOptions(InteractionContext ctx)
    {
        var embed = new DiscordEmbedBuilder
        {
            Title = "Shelter upgrade options",
            Color = DiscordColor.Aquamarine,
            Description =
                "Here are the available upgrade options for your shelter. To upgrade use `/shelter upgrade {item} {price}`"
        };
        var capacity = FarmerService.GetKiddingPenCapacity(ctx.User.Id);
        var price = (int)Math.Ceiling(5000 * capacity / 2.0);
        var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
        if (usersPerks.Any(perk => perk.id == 14))
        {
            price = (int)Math.Ceiling(price * 0.9);
        }

        embed.AddField("Capacity", $"{price} credits will increase capacity by 1");
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .AddEmbed(embed));
    }

    [SlashCommand("info", "Show information about your shelter")]
    public async Task ShowShelterInfo(InteractionContext ctx)
    {
        if (FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = $"{ctx.User.Username}'s shelter",
                Color = DiscordColor.Aquamarine
            };
            embed.AddField("Capacity", FarmerService.GetKiddingPenCapacity(ctx.User.Id).ToString(), true);
            embed.AddField("In use",
                FarmerService.DoesFarmerHaveAdultsInKiddingPen(GoatService.ReturnUsersGoats(ctx.User.Id))
                    ? "Yes"
                    : "False", true);
            embed.AddField("Kids in shelter",
                FarmerService.DoesFarmerHaveKidsInKiddingPen(ctx.User.Id) ? "Yes" : "False", true);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed));
        }
        else
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("You do not own a shelter yet."));
        }
    }

    public enum PenUpgradeOptions
    {
        Capacity
    }

    [SlashCommand("upgrade", "show upgrade options for shelter and upgrade it")]
    public async Task UpgradeShelter(InteractionContext ctx,
        [Option("upgrade_option", "What to upgrade")]
        PenUpgradeOptions option,
        [Option("cost", "upgrade cost")] int price)
    {
        var options = new HashSet<string>
        {
            "capacity"
        };

        if (!FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("You do not own a shelter yet."));
            return;
        }

        if (!FarmerService.HasEnoughCredits(ctx.User.Id, price))
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("You do not have enough credits for this upgrade."));
            return;
        }

        switch (option)
        {
            case PenUpgradeOptions.Capacity:
                var capacity = FarmerService.GetKiddingPenCapacity(ctx.User.Id);
                var upgradePrice = (int)Math.Ceiling(5000 * capacity / 2.0);
                var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
                if (usersPerks.Any(perk => perk.id == 14))
                {
                    upgradePrice = (int)Math.Ceiling(upgradePrice * 0.9);
                }

                if (price == upgradePrice)
                {
                    FarmerService.IncreaseKiddingPenCapacity(ctx.User.Id, capacity, 1);
                    FarmerService.DeductCreditsFromFarmer(ctx.User.Id, upgradePrice);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent($"Your shelter can now hold {capacity + 1} does"));
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent($"Upgrade price for {option} is {upgradePrice} not {price}"));
                }

                break;
            default:
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"There is no upgrade option called {option.ToString()}"));
                break;

        }
    }

    [SlashCommand("move", "move a kid into your goat pen")]
    public async Task MoveKidIntoKiddingPen(InteractionContext ctx, [Option("kid_Id", "id of kid to move")] string idString)
    {
        try
        {
            if (!FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You currently don't have a kidding pen"));
            }
            else if (!FarmerService.DoesFarmerHaveKidsInKiddingPen(ctx.User.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("You currently don't have any kids in your kidding pen"));
            }
            else if (idString.ToLower().Equals("all"))
            {
                var userPerks = await perkService.GetUsersPerks(ctx.User.Id);
                var kids = GoatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                if (GoatService.CanGoatsFitInBarn(ctx.User.Id, kids.Count, userPerks, ctx.Client.Logger))
                {
                    kids.ForEach(kid => GoatService.MoveKidIntoGoatPen(kid, ctx.User.Id));
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent($"{kids.Count} kids have now been moved to your barn"));
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent($"There is not enough room in your barn for all {kids.Count} kids"));
                }
            }
            else
            {
                var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
                if (!int.TryParse(idString, out var id))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent($"You entered {idString} which is not a number"));
                    throw new Exception("not a number");
                }

                var kids = GoatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                var kidToMove = kids.Find(x => x.Id == id);
                if (null == kidToMove)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("You don't have a kid in your shelter with this id"));
                }
                else if (GoatService.CanGoatsFitInBarn(ctx.User.Id, 1, usersPerks, ctx.Client.Logger))
                {
                    GoatService.MoveKidIntoGoatPen(kidToMove, ctx.User.Id);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent($"Kid with id {id} has been moved into your barn"));
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("There is no room in your barn for this kid at the moment"));
                }
            }
        }
        catch (Exception ex)
        {
            ctx.Client.Logger.Log(LogLevel.Error,
                "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                ctx.User.Username, ctx.Interaction.Data?.Name ?? "<unknown command>",
                ex.GetType(), ex.Message);
        }
    }

    [SlashCommand("sell", "Sell a kid in your shelter")]
    public async Task SellKidInKiddingPen(InteractionContext ctx, [Option("kid_Id", "id of kid to sell")] string idString)
    {
        try
        {
            var hasLoan = FarmerService.DoesFarmerHaveALoan(ctx.User.Id);
            if (!FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You currently don't have a kidding pen"));
            }
            else if (!FarmerService.DoesFarmerHaveKidsInKiddingPen(ctx.User.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("You currently don't have any kids in your kidding pen"));
            }
            else if (idString.ToLower().Equals("all"))
            {
                var kids = GoatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                var total = 0;
                var deductionTotal = 0;
                var loanString = "";
                kids.ForEach(kid =>
                {
                    GoatService.DeleteKidFromKiddingPen(kid.Id);
                    if (hasLoan)
                    {
                        var (repaymentAmount, loanAmount) =
                            FarmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, (kid.Level * 2));
                        deductionTotal += repaymentAmount;
                        loanString =
                            $"{Environment.NewLine}{deductionTotal:n0} credits have been taken from your earnings to " +
                            $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";
                        FarmerService.AddCreditsToFarmer(ctx.User.Id, ((kid.Level * 2) - repaymentAmount));
                    }
                    else
                    {
                        FarmerService.AddCreditsToFarmer(ctx.User.Id, kid.Level * 2);
                    }

                    total += kid.Level * 2;
                });
                var kidOrKids = kids.Count == 1 ? "kid" : "kids";
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent($"You have sold {kids.Count} {kidOrKids} for {total:n0} credits. {loanString}"));
            }
            else
            {
                if (!int.TryParse(idString, out var id))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent($"You entered {idString} which is not a number"));
                    throw new Exception("not a number");
                }

                var kids = GoatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                var kidToSell = kids.Find(x => x.Id == id);
                if (null == kidToSell)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("You don't have a kid in your shelter with this id"));
                }
                else
                {
                    var loanString = "";
                    GoatService.DeleteKidFromKiddingPen(id);
                    if (hasLoan)
                    {
                        var (repaymentAmount, loanAmount) =
                            FarmerService.TakeLoanRepaymentFromEarnings(ctx.User.Id, kidToSell.Level * 2);
                        loanString =
                            $"{Environment.NewLine}{repaymentAmount:n0} credits have been taken from your earnings to " +
                            $"cover your loan. Remaining amount on your loan is {loanAmount:n0}.";
                        FarmerService.AddCreditsToFarmer(ctx.User.Id, ((kidToSell.Level * 2) - repaymentAmount));
                    }
                    else
                    {
                        FarmerService.AddCreditsToFarmer(ctx.User.Id, kidToSell.Level * 2);
                    }

                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent($"You have sold kid with id {id} for {(kidToSell.Level * 2):n0} credits. {loanString}"));
                }
            }
        }
        catch (Exception ex)
        {
            ctx.Client.Logger.Log(LogLevel.Error,
                "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                ctx.User.Username, ctx.Interaction.Data?.Name ?? "<unknown command>",
                ex.GetType(), ex.Message);
        }
    }

    [SlashCommand("show", "Shows the kids in your shelter")]
    public async Task ShowKidsInKiddingPen(InteractionContext ctx)
    {
        try
        {
            if (!FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You currently don't have a kidding pen"));
            }
            else if (!FarmerService.DoesFarmerHaveKidsInKiddingPen(ctx.User.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("You currently don't have any kids in your kidding pen"));
            }
            else
            {
                var kids = GoatService.ReturnUsersKidsInKiddingPen(ctx.User.Id);
                var url = "https://williamspires.com/";
                var pages = new List<Page>();
                var interactivity = ctx.Client.GetInteractivity();
                foreach (var goat in kids)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"{goat.Id}",
                        ImageUrl = url + goat.FilePath.Replace(" ", "%20")
                    };
                    embed.AddField("Name", goat.Name);
                    embed.AddField("Level", goat.Level.ToString(), true);
                    embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true);
                    embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true);
                    var page = new Page
                    {
                        Embed = embed
                    };
                    pages.Add(page);
                }

                _ = Task.Run(async () => await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.User, pages,
                        null,
                        PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable, CancellationToken.None)
                    .ConfigureAwait(false));
            }
        }
        catch (Exception ex)
        {
            ctx.Client.Logger.Log(LogLevel.Error,
                "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                ctx.User.Username, ctx.Interaction.Data?.Name ?? "<unknown command>",
                ex.GetType(), ex.Message);
        }
    }
}