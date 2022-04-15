using System;
using System.Collections.Generic;
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
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace BumbleBot.ApplicationCommands.SlashCommands.Game;

[ApplicationCommandModuleLifespan(ApplicationCommandModuleLifespan.Transient)]
public class GeneralGameSlashCommands : ApplicationCommandsModule
{
    private readonly DbUtils dbUtils = new();
    private Timer equipTimer;
    private bool equipTimerrunning;

    public GeneralGameSlashCommands(GoatService goatService, FarmerService farmerService,
        GoatSpawningService goatSpawningService, PerkService perkService)
    {
        this.perkService = perkService;
        this.farmerService = farmerService;
        this.goatService = goatService;
        this.goatSpawningService = goatSpawningService;
    }

    private readonly PerkService perkService;
    private readonly FarmerService farmerService;
    private readonly GoatService goatService;
    private readonly GoatSpawningService goatSpawningService;
    
     private void SetEquipTimer()
        {
            equipTimer = new Timer(240000);
            equipTimer.Elapsed += FinishTimer;
            equipTimer.Enabled = true;
            equipTimerrunning = true;
        }

        private void FinishTimer(object? source, ElapsedEventArgs e)
        {
            equipTimerrunning = false;
            equipTimer.Stop();
            equipTimer.Dispose();
        }

        #region SpawnGoats
        // public enum GoatTypes
        // {
        //     Spring,
        //     Dazzle,
        //     Member,
        //     Dairy,
        //     Holiday,
        //     Tailless,
        //     Valentines,
        //     Shamrock,
        //     Summer,
        //     Buck,
        //     Bot_Birthday,
        //     Special,
        //     Halloween,
        //     November,
        //     Options,
        //     Normal
        // }
        // [SlashCommand("spawn", "spawns a goat of certain type")]
        // [OwnerOrPermissionSlash(Permissions.KickMembers)]
        // public async Task SpawnGoat(CommandContext ctx, [Option("type", "type of goat to spawn")] GoatTypes goatType)
        // {
        //     try
        //     {
        //         await ctx.Message.DeleteAsync();
        //         switch (goatType)
        //         {
        //             case GoatTypes.Spring:
        //                 var springGoatToSpawn = goatSpawningService.GenerateSpecialSpringGoatToSpawn();
        //                 _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     springGoatToSpawn.Item1, springGoatToSpawn.Item2, ctx.Client));
        //                 break;
        //             case GoatTypes.Dazzle:
        //                 var bestGoat = goatSpawningService.GenerateBestestGoatToSpawn();
        //                 _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     bestGoat.Item1, bestGoat.Item2, ctx.Client));
        //                 break;
        //             case GoatTypes.Member:
        //                 var memberGoatToSpawn = goatSpawningService.GenerateMemberSpecialGoatToSpawn();
        //                 _ = Task.Run(() =>
        //                     goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild, memberGoatToSpawn.Item1,
        //                         memberGoatToSpawn.Item2, ctx.Client));
        //                 break;
        //             case GoatTypes.Dairy:
        //                 var dairySpecial = goatSpawningService.GenerateSpecialDairyGoatToSpawn();
        //                 _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     dairySpecial.Item1, dairySpecial.Item2, ctx.Client));
        //                 break;
        //             case GoatTypes.Holiday:
        //                 var christmasGoat = goatSpawningService.GenerateChristmasSpecialToSpawn();
        //                 _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     christmasGoat.Item1, christmasGoat.Item2, ctx.Client));
        //                 break;
        //             case GoatTypes.Tailless:
        //                 var taillessGoat = goatSpawningService.GenerateTaillessSpecialGoatToSpawn();
        //                 _ = Task.Run(() =>
        //                     goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild, taillessGoat.Item1,
        //                         taillessGoat.Item2, ctx.Client));
        //                 break; 
        //             case GoatTypes.Valentines:
        //                 var valentinesGoat = goatSpawningService.GenerateValentinesGoatToSpawn();
        //                 _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     valentinesGoat.Item1, valentinesGoat.Item2, ctx.Client));
        //                 break;
        //             case GoatTypes.Shamrock:
        //                 var goat = goatSpawningService.GenerateSpecialPaddyGoatToSpawn();
        //                 _ = Task.Run(() =>
        //                     goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild, goat.Item1, goat.Item2,
        //                         ctx.Client));
        //                 break;
        //             case GoatTypes.Summer:
        //                 var summerGoat = goatSpawningService.GenerateSummerGoatToSpawn();
        //                 _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     summerGoat.Item1, summerGoat.Item2,
        //                     ctx.Client));
        //                 break;
        //             case GoatTypes.Buck:
        //                 var buckGoat = goatSpawningService.GenerateBuckSpecialToSpawn();
        //                 _ = Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     buckGoat.Item1, buckGoat.Item2, ctx.Client));
        //                 break;
        //             case GoatTypes.Bot_Birthday:
        //                 var birthdayGoat = goatSpawningService.GenerateBotBirthdaySpecialToSpawn();
        //                 _ = Task.Run(() =>
        //                     goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild, birthdayGoat.Item1,
        //                         birthdayGoat.Item2, ctx.Client));
        //                 break;
        //             case GoatTypes.Special:
        //                 var specialGoat = goatSpawningService.GenerateSpecialGoatToSpawn();
        //                 _ = Task.Run(() =>
        //                     goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild, specialGoat.Item1,
        //                         specialGoat.Item2, ctx.Client));
        //                 break;
        //             case GoatTypes.Halloween:
        //                 var halloweenGoat = goatSpawningService.GenerateHalloweenSpecialToSpawn();
        //                 _ = Task.Run(() =>
        //                     goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild, halloweenGoat.Item1,
        //                         halloweenGoat.Item2, ctx.Client));
        //                 break;
        //             case GoatTypes.November:
        //                 var novGoat = goatSpawningService.GenerateNovemberGoatToSpawn();
        //                 _ = Task.Run(() =>
        //                     goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild, novGoat.Item1,
        //                         novGoat.Item2, ctx.Client));
        //                 break;
        //             case GoatTypes.Options:
        //                 await ctx.Channel.SendMessageAsync(
        //                     $@"Options are {Formatter.BlockCode
        //                         ($"spring {Environment.NewLine}dazzle {Environment.NewLine}member special {Environment.NewLine}dairy {Environment.NewLine}holiday " +
        //                          $"{Environment.NewLine}tailless {Environment.NewLine}valentines {Environment.NewLine}shamrock {Environment.NewLine}summer " +
        //                          $"{Environment.NewLine}buck {Environment.NewLine}bot birthday {Environment.NewLine}special {Environment.NewLine}halloween")}");
        //                 break;
        //             case GoatTypes.Normal:
        //                 _ = new Random().Next(0, 100) == 69
        //                     ? Task.Run(() =>
        //                         goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                             goatSpawningService.GenerateSpecialGoatToSpawn(), ctx.Client))
        //                     : Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                         goatSpawningService.GenerateNormalGoatToSpawn(), ctx.Client));
        //                 break;
        //             default:
        //                 _ = new Random().Next(0, 100) == 69
        //                     ? Task.Run(() =>
        //                         goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                             goatSpawningService.GenerateSpecialGoatToSpawn(), ctx.Client))
        //                     : Task.Run(() => goatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                         goatSpawningService.GenerateNormalGoatToSpawn(), ctx.Client));
        //                 break;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         await Console.Out.WriteAsync(ex.Message);
        //     }
        // }
        // #endregion
        //
        // [Command("spawn")]
        // [Hidden]
        // [OwnerOrPermission(Permissions.KickMembers)]
        // public async Task SpawnGoat(CommandContext ctx)
        // {
        //     try
        //     {
        //         await ctx.Message.DeleteAsync();
        //         var random = new Random();
        //         var number = random.Next(0, 5);
        //         switch (number)
        //         {
        //             case 0 or 1 when GoatSpawningService.IsSpecialSpawnEnabled("springSpecials"):
        //                 var springGoatToSpawn = GoatSpawningService.GenerateSpecialSpringGoatToSpawn();
        //                 _ = Task.Run(() => GoatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     springGoatToSpawn.Item1, springGoatToSpawn.Item2, ctx.Client));
        //                 break;
        //             case 0 or 1 when GoatSpawningService.IsSpecialSpawnEnabled("bestestGoat"):
        //                 var bestGoat = GoatSpawningService.GenerateBestestGoatToSpawn();
        //                 _ = Task.Run(() => GoatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     bestGoat.Item1, bestGoat.Item2, ctx.Client));
        //                 break;
        //             case 0 or 1 when GoatSpawningService.IsSpecialSpawnEnabled("memberSpecials"):
        //                 var memberGoatToSpawn = GoatSpawningService.GenerateMemberSpecialGoatToSpawn();
        //                 _ = Task.Run(() =>
        //                     GoatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild, memberGoatToSpawn.Item1,
        //                         memberGoatToSpawn.Item2, ctx.Client));
        //                 break;
        //             case 1 when GoatSpawningService.IsSpecialSpawnEnabled("dairySpecials"):
        //                 var dairySpecial = GoatSpawningService.GenerateSpecialDairyGoatToSpawn();
        //                 _ = Task.Run(() => GoatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     dairySpecial.Item1, dairySpecial.Item2, ctx.Client));
        //                 break;
        //             case 0 or 1 when GoatSpawningService.IsSpecialSpawnEnabled("botBirthdayEnabled"):
        //                 var birthdayGoat = GoatSpawningService.GenerateBotBirthdaySpecialToSpawn();
        //                 _ = Task.Run(() =>
        //                     GoatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild, birthdayGoat.Item1,
        //                         birthdayGoat.Item2, ctx.Client));
        //                 break;
        //             case 1 when GoatSpawningService.IsSpecialSpawnEnabled("christmasSpecials"):
        //                 var christmasGoat = GoatSpawningService.GenerateChristmasSpecialToSpawn();
        //                 _ = Task.Run(() => GoatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     christmasGoat.Item1, christmasGoat.Item2, ctx.Client));
        //                 break;
        //             case 2 when GoatSpawningService.IsSpecialSpawnEnabled("taillessEnabled"):
        //                 var taillessGoat = GoatSpawningService.GenerateTaillessSpecialGoatToSpawn();
        //                 _ = Task.Run(() =>
        //                     GoatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild, taillessGoat.Item1,
        //                         taillessGoat.Item2, ctx.Client));
        //                 break;
        //             case 3 when GoatSpawningService.IsSpecialSpawnEnabled("valentinesSpecials"):
        //                 var valentinesGoat = GoatSpawningService.GenerateValentinesGoatToSpawn();
        //                 _ = Task.Run(() => GoatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                     valentinesGoat.Item1, valentinesGoat.Item2, ctx.Client));
        //                 break;
        //             case 4 when GoatSpawningService.IsSpecialSpawnEnabled("paddysSpecials"):
        //                 var goat = GoatSpawningService.GenerateSpecialPaddyGoatToSpawn();
        //                 _ = Task.Run(() =>
        //                     GoatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild, goat.Item1, goat.Item2,
        //                         ctx.Client));
        //                 break;
        //             default:
        //             {
        //                 _ = random.Next(0, 100) == 69
        //                     ? Task.Run(() =>
        //                         GoatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                             GoatSpawningService.GenerateSpecialGoatToSpawn(), ctx.Client))
        //                     : Task.Run(() => GoatSpawningService.SpawnGoatFromGoatObject(ctx.Channel, ctx.Guild,
        //                         GoatSpawningService.GenerateNormalGoatToSpawn(), ctx.Client));
        //                 break;
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         await Console.Out.WriteLineAsync(ex.Message);
        //     }
        // }
        #endregion

        [SlashCommand("givec", "Give credits to a user")]
        [OwnerOrPermissionSlash(Permissions.KickMembers)]
        public async Task GiveCredits(InteractionContext ctx, [Option("user", "user to give credits too")] DiscordUser member, 
            [Option("amount", "amount of credits to give")] int credits)
        {
            farmerService.AddCreditsToFarmer(member.Id, credits);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"{credits} have been given to {member.Mention}"));
        }

        [SlashCommand("create", "create your character")]
        [IsUserAvailableSlash]
        public async Task CreateCharacter(InteractionContext ctx)
        {
            if (DoesUserHaveCharacter(ctx.Member.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You already have an account"));
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

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Your character has now been created!"));
            }
            catch (Exception ex)
            {
                ctx.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                    ctx.User.Username, ctx.Interaction.Data?.Name ?? "<unknown command>",
                    ex.GetType(), ex.Message);
            }
        }

        [SlashCommand("daily", "Collect your daily reward")]
        [IsUserAvailableSlash]
        public async Task CollectDaily(InteractionContext ctx)
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
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
                        goatService.UpdateGoatImagesForKidsThatAreAdults(ctx.User.Id);
                        if (dailyResponse != null)
                            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                                .WithContent(dailyResponse.Message));
                    }
                });
            }
            catch (Exception ex)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("Something went wrong while collecting your daily"));
                ctx.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                    ctx.User.Username, ctx.Interaction.Data?.Name ?? "<unknown command>",
                    ex.GetType(), ex.Message);
            }
        }
        
        [SlashCommand("profile", "Shows your game profile")]
        public async Task ShowProfile(InteractionContext ctx)
        {
            try
            {
                if (!DoesUserHaveCharacter(ctx.Member.Id))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("You do not have an account yet. Use /create to create one."));
                    return;
                }
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var credits = 10;
                var barnSize = 10;
                var grazingSize = 0;
                decimal milkAmount = 0;
                int perkPoints = 0;
                int level = 0;
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
                        level = reader.GetInt32("level");
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

                var goat = goatService.GetEquippedGoat(ctx.User.Id);
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
                    new("Level", level.ToString(), true),
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

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .AddEmbed(embed));
            }
            catch (Exception ex)
            {
                ctx.Client.Logger.Log(LogLevel.Error,
                    "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
                    ctx.User.Username, ctx.Interaction.Data?.Name ?? "<unknown command>",
                    ex.GetType(), ex.Message);
            }
        }

        #region Handle 2.0
        // [SlashCommand("handle", "Select a Goat to Handle")]
        // public async Task EquipGoat(InteractionContext ctx)
        // {
        //     try
        //     {
        //         _ = Task.Run(async () =>
        //         {
        //             var goats = new List<Goat>();
        //             using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        //             {
        //                 var query = "Select * from goats where ownerID = ?ownerId";
        //                 var command = new MySqlCommand(query, connection);
        //                 command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ctx.Member.Id;
        //                 connection.Open();
        //                 var reader = command.ExecuteReader();
        //                 if (reader.HasRows)
        //                     while (reader.Read())
        //                     {
        //                         var goat = new Goat
        //                         {
        //                             Id = reader.GetInt32("id"),
        //                             Level = reader.GetInt32("level"),
        //                             Name = reader.GetString("name"),
        //                             Type = (Models.Type) Enum.Parse(typeof(Models.Type), reader.GetString("type")),
        //                             Breed = (Breed) Enum.Parse(typeof(Breed), reader.GetString("breed")),
        //                             BaseColour = (BaseColour) Enum.Parse(typeof(BaseColour),
        //                                 reader.GetString("baseColour")),
        //                             LevelMulitplier = reader.GetDecimal("levelMultiplier"),
        //                             Equiped = reader.GetBoolean("equipped"),
        //                             Experience = reader.GetDecimal("experience"),
        //                             FilePath = reader.GetString("imageLink")
        //                         };
        //                         goats.Add(goat);
        //                     }
        //
        //                 reader.Close();
        //             }
        //
        //             if (goats.Count < 1)
        //             {
        //                 await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        //                         .WithContent("You don't currently own any goats that can be handled"));
        //             }
        //             else
        //             {
        //                 var url = "https://williamspires.com/";
        //                 var pages = new List<Page>();
        //                 var interactivity = ctx.Client.GetInteractivity();
        //                 var backward = DiscordEmoji.FromName(ctx.Client, ":arrow_backward:");
        //                 var forward = DiscordEmoji.FromName(ctx.Client, ":arrow_forward:");
        //                 var equipBarn = DiscordEmoji.FromName(ctx.Client, ":1barn:");
        //                 foreach (var goat in goats)
        //                 {
        //                     var embed = new DiscordEmbedBuilder
        //                     {
        //                         Title = $"{goat.Id}",
        //                         ImageUrl = url + goat.FilePath.Replace(" ", "%20")
        //                     };
        //                     embed.AddField("Name", goat.Name, true);
        //                     embed.AddField("Level", goat.Level.ToString(), true);
        //                     embed.AddField("Experience", goat.Experience.ToString(CultureInfo.CurrentCulture), true);
        //                     var page = new Page
        //                     {
        //                         Embed = embed
        //                     };
        //                     pages.Add(page);
        //                 }
        //
        //                 var pageCounter = 0;
        //                 var msg = await ctx.Channel.SendMessageAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
        //                 SetEquipTimer();
        //                 while (equipTimerrunning)
        //                 {
        //                     await msg.CreateReactionAsync(backward).ConfigureAwait(false);
        //                     await msg.CreateReactionAsync(forward).ConfigureAwait(false);
        //                     await msg.CreateReactionAsync(equipBarn).ConfigureAwait(false);
        //
        //                     var result = await interactivity.WaitForReactionAsync(x => x.Channel == ctx.Channel &&
        //                             x.User == ctx.User
        //                             && (x.Emoji == backward || x.Emoji == forward || x.Emoji == equipBarn),
        //                         TimeSpan.FromMinutes(4));
        //
        //                     if (result.TimedOut)
        //                     {
        //                         equipTimerrunning = false;
        //                     }
        //                     else if (result.Result.Emoji == backward)
        //                     {
        //                         if (pageCounter - 1 < 0)
        //                             pageCounter = pages.Count - 1;
        //                         else
        //                             pageCounter--;
        //                         await msg.DeleteReactionAsync(backward, ctx.User).ConfigureAwait(false);
        //                         await msg.ModifyAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
        //                     }
        //                     else if (result.Result.Emoji == forward)
        //                     {
        //                         if (pageCounter + 1 >= pages.Count)
        //                             pageCounter = 0;
        //                         else
        //                             pageCounter++;
        //                         await msg.DeleteReactionAsync(forward, ctx.User).ConfigureAwait(false);
        //                         await msg.ModifyAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
        //                     }
        //                     else if (result.Result.Emoji == equipBarn)
        //                     {
        //                         if (!int.TryParse(pages[pageCounter].Embed.Title, out var id))
        //                         {
        //                             await ctx.Channel
        //                                 .SendMessageAsync("Something went wrong while trying to handle this goat.")
        //                                 .ConfigureAwait(false);
        //                             return;
        //                         }
        //
        //                         using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        //                         {
        //                             var query = "Update goats set equipped = 0 where ownerID = ?ownerId";
        //                             var command = new MySqlCommand(query, connection);
        //                             command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ctx.User.Id;
        //                             connection.Open();
        //                             command.ExecuteNonQuery();
        //                         }
        //
        //                         using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        //                         {
        //                             var query = "Update goats set equipped = 1 where id = ?id";
        //                             var command = new MySqlCommand(query, connection);
        //                             command.Parameters.Add("?id", MySqlDbType.Int32).Value = id;
        //                             connection.Open();
        //                             command.ExecuteNonQuery();
        //                         }
        //
        //                         await ctx.Channel.SendMessageAsync("Goat is now in hand.").ConfigureAwait(false);
        //                         //pages.RemoveAt(pageCounter);
        //                         await msg.DeleteAllReactionsAsync().ConfigureAwait(false);
        //                         equipTimerrunning = false;
        //                     }
        //                 }
        //             }
        //         });
        //         
        //     }
        //     catch (Exception ex)
        //     {
        //         ctx.Client.Logger.Log(LogLevel.Error,
        //             "{Username} tried executing '{QualifiedName}' but it errored: {ExceptionType}: {ExceptionMessage}",
        //             ctx.User.Username, ctx.Command?.QualifiedName ?? "<unknown command>",
        //             ex.GetType(), ex.Message);
        //     }
        // }
        #endregion
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