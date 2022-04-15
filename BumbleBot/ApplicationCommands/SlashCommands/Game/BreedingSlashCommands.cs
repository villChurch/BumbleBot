using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BumbleBot.ApplicationCommands.SlashCommands.AutoCompletes;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.Extensions.Logging;

namespace BumbleBot.ApplicationCommands.SlashCommands.Game;

[SlashCommandGroup("breed", "Breed and view goats in your shelter")]
public class BreedingSlashCommands : ApplicationCommandsModule
{
    public BreedingSlashCommands(FarmerService farmerService, GoatService goatService)
    {
        FarmerService = farmerService;
        GoatService = goatService;
    }
    
    private FarmerService FarmerService { get; }
    private GoatService GoatService { get; }
    
        [SlashCommand("show", "show current goats that have been sent to breed")]
        [IsUserAvailableSlash]
        public async Task ShowGoatsInKiddingPenToBreed(InteractionContext ctx)
        {
            try
            {
                if (!FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("You currently don't have a kidding pen"));
                }
                else if (!FarmerService.DoesFarmerHaveAdultsInKiddingPen(GoatService.ReturnUsersGoats(ctx.User.Id)))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("You currently do not have any adult goats in your kidding pen"));
                }
                else
                {
                    var (breedingIds, dictionary) = GoatService.ReturnUsersAdultGoatIdsInKiddingPen(ctx.User.Id);
                    var breedingGoats =
                        GoatService.ReturnUsersGoats(ctx.User.Id).Where(goat => breedingIds.Contains(goat.Id)).ToList();
                    const string url = "http://williamspires.com/";
                    var pages = new List<Page>();
                    var interactivity = ctx.Client.GetInteractivity();
                    foreach (var goat in breedingGoats)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{goat.Id}",
                            ImageUrl = url + goat.FilePath.Replace(" ", "%20")
                        };
                        embed.AddFields(new List<DiscordEmbedField>()
                        {
                            new("Name", goat.Name),
                            new("Due Date", dictionary[goat.Id]),
                            new("Level", goat.Level.ToString(), true),
                            new("Experience", goat.Experience.ToString(CultureInfo.CurrentCulture), true),
                            new("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true),
                            new("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true)
                        });
                        var page = new Page
                        {
                            Embed = embed
                        };
                        pages.Add(page);
                    }
                    _ = Task.Run(async () =>await interactivity.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.User, pages, null,
                        PaginationBehaviour.WrapAround,ButtonPaginationBehavior.Disable,CancellationToken.None).ConfigureAwait(false));
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

        [SlashCommand("add", "Move a goat to the shelter")]
        [IsUserAvailableSlash]
        public async Task MoveGoatToKiddingPen(InteractionContext ctx, 
            [Option("goatId", "id of goat to move to the shelter")] int goatId)
        {
            try
            {
                if (FarmerService.DoesFarmerHaveAKiddingPen(ctx.User.Id))
                {
                    var goats = GoatService.ReturnUsersGoats(ctx.User.Id);
                    var goatsBreeding = GoatService.ReturnUsersAdultGoatIdsInKiddingPen(ctx.User.Id);

                    if (goats.Where(goat => goat.Id == goatId).ToList().Count == 0)
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                .WithContent($"It appears you don't own a goat with id {goatId}"));
                    }
                    else if (goats.Find(goat => goat.Id == goatId)?.Level < 100)
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                .WithContent("This goat is not yet an adult and only adults can be moved to the shelter"));
                    }
                    else if (goats.Find(goat => goat.Id == goatId)?.BaseColour == BaseColour.Special)
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent("You cannot breed special goats"));
                    }
                    else if (goatsBreeding.Item2.ContainsKey(goatId))
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent("This goat is already in the shelter"));
                    }
                    else
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                        var uri = $"http://localhost:8080/breeding/{ctx.User.Id}/{goatId}";
                        var request = (HttpWebRequest) WebRequest.Create(uri);
                        request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                        using var response = (HttpWebResponse) await request.GetResponseAsync();
                        await using (var stream = response.GetResponseStream())
                        using (var reader = new StreamReader(stream))
                        {
                            var stringResponse = await reader.ReadToEndAsync();

                            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                                .WithContent(stringResponse));
                        }
                    }
                }
                else
                {
                    await ctx.Channel.SendMessageAsync("You need to purchase the shelter/kidding pen first")
                        .ConfigureAwait(false);
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