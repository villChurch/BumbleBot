using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using BumbleBot.Commands.Game;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using Chronic;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Type = BumbleBot.Models.Type;

namespace BumbleBot.Commands.ButtonCommands
{
    public class TestButtonCommands : BaseCommandModule
    {

        private DbUtils dbUtils = new();
        private Timer equipTimer;
        private bool equipTimerrunning;
        private GoatService goatService;
        private FarmerService farmerService;

        public TestButtonCommands(FarmerService farmerService, GoatService goatService)
        {
            this.goatService = goatService;
            this.farmerService = farmerService;
        }
        private void SetEquipTimer()
        {
            equipTimer = new Timer(240000);
            equipTimer.Elapsed += FinishTimer;
            equipTimer.Enabled = true;
            equipTimerrunning = true;
        }
        
        private void FinishTimer(object source, ElapsedEventArgs e)
        {
            equipTimerrunning = false;
            equipTimer.Stop();
            equipTimer.Dispose();
        }

        [Command("tb")]
        public async Task TestButtonCommand(CommandContext ctx)
        {
            var myButton = new DiscordButtonComponent(ButtonStyle.Primary, "my_very_cool_button", "Very cool button!",
                false, new DiscordComponentEmoji("😀"));
            var builder = new DiscordMessageBuilder();
            builder.WithContent("This message has buttons! Pretty neat innit?");
            builder.AddComponents(myButton);
            var msg = await builder.SendAsync(ctx.Channel);
            var interactivity = ctx.Client.GetInteractivity();
            var buttonComponents = new List<DiscordButtonComponent> {myButton};
            IEnumerable<DiscordButtonComponent> buttonEnum = buttonComponents;
            var result = await interactivity.WaitForButtonAsync(msg, buttonEnum, TimeSpan.FromMinutes(1));
            var interaction = result.Result;
            await interaction.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"{result.Result.User.Mention} pressed the button first")
                    .AddComponents(myButton)).ConfigureAwait(false);
        }
        
        [Command("bhandle")]
        public async Task ButtonEquipGoat(CommandContext ctx)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    var goats = new List<Goat>();
                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "Select * from goats where ownerID = ?ownerId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ctx.Member.Id;
                        connection.Open();
                        var reader = command.ExecuteReader();
                        if (reader.HasRows)
                            while (reader.Read())
                            {
                                var goat = new Goat
                                {
                                    Id = reader.GetInt32("id"),
                                    Level = reader.GetInt32("level"),
                                    Name = reader.GetString("name"),
                                    Type = (Type) Enum.Parse(typeof(Type), reader.GetString("type")),
                                    Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed")),
                                    BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour),
                                        reader.GetString("baseColour")),
                                    LevelMulitplier = reader.GetDecimal("levelMultiplier"),
                                    Equiped = reader.GetBoolean("equipped"),
                                    Experience = reader.GetDecimal("experience"),
                                    FilePath = reader.GetString("imageLink")
                                };
                                goats.Add(goat);
                            }

                        reader.Close();
                    }

                    if (goats.Count < 1)
                    {
                        await ctx.Channel.SendMessageAsync("You don't currently own any goats that can be handled")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        var url = "https://williamspires.com/";
                        var pages = new List<Page>();
                        var interactivity = ctx.Client.GetInteractivity();
                        foreach (var goat in goats)
                        {
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = $"{goat.Id}",
                                ImageUrl = url + goat.FilePath.Replace(" ", "%20")
                            };
                            embed.AddField("Name", goat.Name, true);
                            embed.AddField("Level", goat.Level.ToString(), true);
                            embed.AddField("Experience", goat.Experience.ToString(CultureInfo.CurrentCulture), true);
                            var page = new Page
                            {
                                Embed = embed
                            };
                            pages.Add(page);
                        }
                        var backward = DiscordEmoji.FromName(ctx.Client, ":arrow_backward:");
                        var forward = DiscordEmoji.FromName(ctx.Client, ":arrow_forward:");
                        var sellEmoji = DiscordEmoji.FromName(ctx.Client, ":dollar:");
                        var barnEmoji = DiscordEmoji.FromName(ctx.Client, ":1barn:");
                        var forwardButton = new DiscordButtonComponent(ButtonStyle.Secondary, "next_goat", null,
                            false, new DiscordComponentEmoji(forward));
                        var backwardButton = new DiscordButtonComponent(ButtonStyle.Secondary, "previous_goat",
                            null, false, new DiscordComponentEmoji(backward));
                        var handleButton = new DiscordButtonComponent(ButtonStyle.Secondary, "handle", "Handle", 
                            false, new DiscordComponentEmoji(barnEmoji));
                        var sellButton = new DiscordButtonComponent(ButtonStyle.Secondary, "sell", "Sell",
                            false, new DiscordComponentEmoji(sellEmoji));
                        var campaignButton = new DiscordButtonComponent(ButtonStyle.Secondary, "campaign", "Campaign");
                        var pageCounter = 0;
                        var builder = new DiscordMessageBuilder()
                            .WithEmbed(pages[pageCounter].Embed)
                            .AddComponents(backwardButton, forwardButton, handleButton, sellButton, campaignButton);
                        var msg = await builder.SendAsync(ctx.Channel).ConfigureAwait(false);
                        SetEquipTimer();
                        while (equipTimerrunning)
                        {
                            var buttonResult = await interactivity
                                .WaitForButtonAsync(msg, ctx.User, TimeSpan.FromSeconds(4))
                                .ConfigureAwait(false);
                            
                            if (buttonResult.TimedOut)
                            {
                                equipTimerrunning = false;
                            }
                            else if (buttonResult.Result.Id == backwardButton.CustomId)
                            {
                                if (pageCounter - 1 < 0)
                                    pageCounter = pages.Count - 1;
                                else
                                    pageCounter--;
                                await buttonResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                                    new DiscordInteractionResponseBuilder()
                                        .AddEmbed(pages[pageCounter].Embed)
                                        .AddComponents(backwardButton, forwardButton, handleButton, sellButton, campaignButton))
                                    .ConfigureAwait(false);
                            }
                            else if (buttonResult.Result.Id == forwardButton.CustomId)
                            {
                                if (pageCounter + 1 >= pages.Count)
                                    pageCounter = 0;
                                else
                                    pageCounter++;
                                await buttonResult.Result.Interaction.CreateResponseAsync(
                                        InteractionResponseType.UpdateMessage,
                                        new DiscordInteractionResponseBuilder()
                                            .AddEmbed(pages[pageCounter].Embed)
                                            .AddComponents(backwardButton, forwardButton, handleButton, sellButton, campaignButton))
                                    .ConfigureAwait(false);
                            }
                            else if (buttonResult.Result.Id == sellButton.CustomId)
                            {
                                if (!int.TryParse(pages[pageCounter].Embed.Title, out var id))
                                {
                                    await buttonResult.Result.Interaction.CreateResponseAsync(
                                            InteractionResponseType.UpdateMessage,
                                            new DiscordInteractionResponseBuilder()
                                                .WithContent("Something went wrong while trying to handle this goat."))
                                        .ConfigureAwait(false);
                                    return;
                                }
                                pages.Remove(pages[pageCounter]);
                                pageCounter = pageCounter >= pages.Count ? pages.Count - 1 : pageCounter; 
                                var goat = goatService.ReturnUsersGoats(ctx.User.Id).First(g => g.Id == id);
                                goatService.DeleteGoat(id);
                                var creditsToAdd = goat.Type == Type.Adult ? (int)Math.Ceiling(goat.Level * 1.35) : (int)Math.Ceiling(goat.Level * 0.75);
                                farmerService.AddCreditsToFarmer(ctx.User.Id, creditsToAdd);
                                await buttonResult.Result.Interaction.CreateResponseAsync(
                                        InteractionResponseType.UpdateMessage,
                                        new DiscordInteractionResponseBuilder()
                                            .AddEmbed(pages[pageCounter].Embed)
                                            .WithContent($"You have sold {goat.Name} to market for {creditsToAdd} " +
                                                         "credits")
                                            .AddComponents(backwardButton, forwardButton, handleButton, sellButton, campaignButton))
                                    .ConfigureAwait(false);
                            }
                            else if (buttonResult.Result.Id == campaignButton.CustomId)
                            {
                                if (!int.TryParse(pages[pageCounter].Embed.Title, out var id))
                                {
                                    await buttonResult.Result.Interaction.CreateResponseAsync(
                                            InteractionResponseType.UpdateMessage,
                                            new DiscordInteractionResponseBuilder()
                                                .WithContent("Something went wrong while trying to handle this goat."))
                                        .ConfigureAwait(false);
                                    return;
                                }

                                await Campaign.DoCampaign(ctx, id, 1000);
                                await buttonResult.Result.Interaction.CreateResponseAsync(
                                        InteractionResponseType.UpdateMessage,
                                        new DiscordInteractionResponseBuilder()
                                            .AddEmbed(pages[pageCounter].Embed)
                                            .AddComponents(backwardButton, forwardButton, handleButton, sellButton, campaignButton))
                                    .ConfigureAwait(false);
                            }
                            else if (buttonResult.Result.Id == handleButton.CustomId)
                            {
                                if (!int.TryParse(pages[pageCounter].Embed.Title, out var id))
                                {
                                    await buttonResult.Result.Interaction.CreateResponseAsync(
                                            InteractionResponseType.UpdateMessage,
                                            new DiscordInteractionResponseBuilder()
                                                .WithContent("Something went wrong while trying to handle this goat."))
                                        .ConfigureAwait(false);
                                    return;
                                }

                                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                                {
                                    var query = "Update goats set equipped = 0 where ownerID = ?ownerId";
                                    var command = new MySqlCommand(query, connection);
                                    command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ctx.User.Id;
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                }

                                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                                {
                                    var query = "Update goats set equipped = 1 where id = ?id";
                                    var command = new MySqlCommand(query, connection);
                                    command.Parameters.Add("?id", MySqlDbType.Int32).Value = id;
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                }

                                await buttonResult.Result.Interaction.CreateResponseAsync(
                                    InteractionResponseType.UpdateMessage,
                                    new DiscordInteractionResponseBuilder()
                                        .WithContent("Goat is now in hand.")).ConfigureAwait(false);
                                equipTimerrunning = false;
                            }
                        }
                    }
                });
                
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