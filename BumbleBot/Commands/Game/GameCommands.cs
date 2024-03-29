﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Type = BumbleBot.Models.Type;

namespace BumbleBot.Commands.Game
{
    [IsUserAvailable]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class GameCommands : BaseCommandModule
    {
        private readonly DbUtils dbUtils = new DbUtils();
        private Timer equipTimer;
        private bool equipTimerrunning;

        public GameCommands(GoatService goatService, FarmerService farmerService, GoatSpawningService goatSpawningService, PerkService perkService)
        {
            GoatService = goatService;
            FarmerService = farmerService;
            GoatSpawningService = goatSpawningService;
            this.perkService = perkService;
        }

        private readonly PerkService perkService;
        private FarmerService FarmerService { get; }
        private GoatService GoatService { get; }
        
        private GoatSpawningService GoatSpawningService { get; }

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
        
        [Command("givec")]
        [Hidden]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task GiveCredits(CommandContext ctx, DiscordUser member, int credits)
        {
            FarmerService.AddCreditsToFarmer(member.Id, credits);
            await ctx.Channel.SendMessageAsync($"{credits} have been given to {member.Mention}").ConfigureAwait(false);
        }

        [Command("create")]
        [Description("Create Your Character")]
        public async Task CreateCharacter(CommandContext ctx)
        {
            if (DoesUserHaveCharacter(ctx.Member.Id))
            {
                await ctx.Channel.SendMessageAsync("You already have an account").ConfigureAwait(false);
                return;
            }

            try
            {
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    const string query = "INSERT INTO farmers (DiscordID) VALUES (?discordID)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync("Your character has now been created!").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ctx.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                    ctx.User.Username, ctx.Command?.QualifiedName ?? "<unknown command>",
                    ex.GetType(), ex.Message);
            }
        }

        [Command("daily")]
        [Description("Collect your daily reward")]
        public async Task CollectDaily(CommandContext ctx)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    var uri = $"http://localhost:8080/daily/{ctx.User.Id}";
                    var request = (HttpWebRequest) WebRequest.Create(uri);
                    request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                    using (var response = (HttpWebResponse) await request.GetResponseAsync())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        var jsonReader = new JsonTextReader(reader);
                        var serializer = new JsonSerializer();
                        var dailyResponse = serializer.Deserialize<DailyResponse>(jsonReader);
                        GoatService.UpdateGoatImagesForKidsThatAreAdults(ctx.User.Id);
                        if (dailyResponse != null)
                            await new DiscordMessageBuilder()
                                .WithReply(ctx.Message.Id, true)
                                .WithContent(dailyResponse.Message)
                                .SendAsync(ctx.Channel);
                    }
                });
            }
            catch (Exception ex)
            {
                await ctx.Channel.SendMessageAsync("Something went wrong while collecting your daily")
                    .ConfigureAwait(false);
                ctx.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                    ctx.User.Username, ctx.Command?.QualifiedName ?? "<unknown command>",
                    ex.GetType(), ex.Message);
            }
        }
        
        [Command("profile")]
        [Description("Shows your game profile")]
        public async Task ShowProfile(CommandContext ctx)
        {
            try
            {
                if (!DoesUserHaveCharacter(ctx.Member.Id))
                {
                    await ctx.Channel.SendMessageAsync("You do not have an account yet. Use g?create to create one.")
                        .ConfigureAwait(false);
                    return;
                }

                var credits = 10;
                var barnSize = 10;
                var grazingSize = 0;
                decimal milkAmount = 0;
                int perkPoints = 0;
                var usersPerks = await perkService.GetUsersPerks(ctx.User.Id);
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "select * from farmers where DiscordID = ?discordID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        credits = reader.GetInt32("credits");
                        barnSize = reader.GetInt32("barnsize");
                        grazingSize = reader.GetInt32("grazesize");
                        milkAmount = reader.GetDecimal("milk");
                        perkPoints = reader.GetInt32("perkpoints");
                    }

                    reader.Close();
                }

                var numberOfGoats = 0;

                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "select COUNT(*) as amount from goats where ownerID = ?discordID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read()) numberOfGoats = reader.GetInt32("amount");
                    reader.Close();
                }

                var goat = GoatService.GetEquippedGoat(ctx.User.Id);
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"Farmer {ctx.Member.DisplayName}'s Profile",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                    {
                        Url = ctx.Member.AvatarUrl
                    },
                    Color = DiscordColor.Aquamarine
                };
                if (usersPerks.Any(perk => perk.id == 10))
                {
                    barnSize = (int) Math.Ceiling(barnSize * 1.1);
                }

                if (usersPerks.Any(perk => perk.id == 12))
                {
                    grazingSize = (int) Math.Ceiling(grazingSize * 1.1);
                }

                embed.AddFields(new List<DiscordEmbedField>()
                {
                    new("Credits", credits.ToString(), true),
                    new("Perk Points", perkPoints.ToString(), true),
                    new("Herd Size", numberOfGoats.ToString()),
                    new("Barn Space", barnSize.ToString(), true),
                    new("Pasture Available", $"Space For {grazingSize} Goats", true),
                    new("Milk in Storage", $"{milkAmount} lbs", true)
                });
                if (goat.Name != null)
                {
                    var url = "https://williamspires.com/";
                    embed.AddFields(new List<DiscordEmbedField>()
                    {
                        new("Goat in hand", goat.Name),
                        new("Goats level", goat.Level.ToString(), true),
                        new("Breed", Enum.GetName(typeof(Breed), goat.Breed)?.Replace("_", " "), true),
                        new("Colour", Enum.GetName(typeof(BaseColour), goat.BaseColour), true)
                    });
                    embed.ImageUrl = url + Uri.EscapeUriString(goat.FilePath); //.Replace(" ", "%20");
                }

                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ctx.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                    ctx.User.Username, ctx.Command?.QualifiedName ?? "<unknown command>",
                    ex.GetType(), ex.Message);
            }
        }

        [Command("handle")]
        [Description("Select a Goat to Handle")]
        public async Task EquipGoat(CommandContext ctx)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    var goats = new List<Goat>();
                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
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
                        var backward = DiscordEmoji.FromName(ctx.Client, ":arrow_backward:");
                        var forward = DiscordEmoji.FromName(ctx.Client, ":arrow_forward:");
                        var equipBarn = DiscordEmoji.FromName(ctx.Client, ":1barn:");
                        foreach (var goat in goats)
                        {
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = $"{goat.Id}",
                                ImageUrl = url + goat.FilePath.Replace(" ", "%20")
                            };
                            embed.AddFields(new List<DiscordEmbedField>()
                            {
                                new("Name", goat.Name, true),
                                new("Level", goat.Level.ToString(), true),
                                new("Experience", goat.Experience.ToString(CultureInfo.CurrentCulture), true)
                            });
                            var page = new Page
                            {
                                Embed = embed
                            };
                            pages.Add(page);
                        }

                        var pageCounter = 0;
                        var msg = await ctx.Channel.SendMessageAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                        SetEquipTimer();
                        while (equipTimerrunning)
                        {
                            await msg.CreateReactionAsync(backward).ConfigureAwait(false);
                            await msg.CreateReactionAsync(forward).ConfigureAwait(false);
                            await msg.CreateReactionAsync(equipBarn).ConfigureAwait(false);

                            var result = await interactivity.WaitForReactionAsync(x => x.Channel == ctx.Channel &&
                                    x.User == ctx.User
                                    && (x.Emoji == backward || x.Emoji == forward || x.Emoji == equipBarn),
                                TimeSpan.FromMinutes(4));

                            if (result.TimedOut)
                            {
                                equipTimerrunning = false;
                            }
                            else if (result.Result.Emoji == backward)
                            {
                                if (pageCounter - 1 < 0)
                                    pageCounter = pages.Count - 1;
                                else
                                    pageCounter--;
                                await msg.DeleteReactionAsync(backward, ctx.User).ConfigureAwait(false);
                                await msg.ModifyAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                            }
                            else if (result.Result.Emoji == forward)
                            {
                                if (pageCounter + 1 >= pages.Count)
                                    pageCounter = 0;
                                else
                                    pageCounter++;
                                await msg.DeleteReactionAsync(forward, ctx.User).ConfigureAwait(false);
                                await msg.ModifyAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                            }
                            else if (result.Result.Emoji == equipBarn)
                            {
                                if (!int.TryParse(pages[pageCounter].Embed.Title, out var id))
                                {
                                    await ctx.Channel
                                        .SendMessageAsync("Something went wrong while trying to handle this goat.")
                                        .ConfigureAwait(false);
                                    return;
                                }

                                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                                {
                                    var query = "Update goats set equipped = 0 where ownerID = ?ownerId";
                                    var command = new MySqlCommand(query, connection);
                                    command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ctx.User.Id;
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                }

                                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                                {
                                    var query = "Update goats set equipped = 1 where id = ?id";
                                    var command = new MySqlCommand(query, connection);
                                    command.Parameters.Add("?id", MySqlDbType.Int32).Value = id;
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                }

                                await ctx.Channel.SendMessageAsync("Goat is now in hand.").ConfigureAwait(false);
                                //pages.RemoveAt(pageCounter);
                                await msg.DeleteAllReactionsAsync().ConfigureAwait(false);
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

        private bool DoesUserHaveCharacter(ulong discordId)
        {
            bool hasCharacter;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                var query = "select * from farmers where DiscordID = ?discordID";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = discordId;
                connection.Open();
                var reader = command.ExecuteReader();
                hasCharacter = reader.HasRows;
                reader.Close();
            }

            return hasCharacter;
        }
   }
}