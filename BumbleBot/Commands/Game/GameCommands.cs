using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.Game
{
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class GameCommands : BaseCommandModule
    {
        private DBUtils dbUtils = new DBUtils();
        private GoatService goatService { get; }
        private Timer equipTimer;
        private bool equipTimerrunning = false;

        public GameCommands(GoatService goatService)
        {
            this.goatService = goatService;
        }


        private void SetEquipTimer()
        {
            equipTimer = new Timer(240000);
            equipTimer.Elapsed += FinishTimer;
            equipTimer.Enabled = true;
            equipTimerrunning = true;
        }

        private void FinishTimer(Object source, ElapsedEventArgs e)
        {
            equipTimerrunning = false;
            equipTimer.Stop();
            equipTimer.Dispose();
        }

        [Command("create")]
        [Description("Create your character")]
        [Hidden, OwnerOrPermission(DSharpPlus.Permissions.KickMembers)]
        public async Task CreateCharacter(CommandContext ctx)
        {
            if (DoesUserHaveCharacter(ctx.Member.Id))
            {
                await ctx.Channel.SendMessageAsync("You already have an account").ConfigureAwait(false);
                return;
            }
            try
            {
                using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "INSERT INTO farmers (DiscordID) VALUES (?discordID)";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                await ctx.Channel.SendMessageAsync("Your character has now been created!").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("profile")]
        [Description("shows your game profile")]
        [Hidden, OwnerOrPermission(DSharpPlus.Permissions.KickMembers)]
        public async Task ShowProfile(CommandContext ctx)
        {
            try
            {
                if (!DoesUserHaveCharacter(ctx.Member.Id))
                {
                    await ctx.Channel.SendMessageAsync("You do not have an account yet. Use gb?create to create one.")
                        .ConfigureAwait(false);
                    return;
                }
                int credits = 10;
                int barnSize = 10;
                int grazingSize = 0;
                using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "select * from farmers where DiscordID = ?discordID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        credits = reader.GetInt32("credits");
                        barnSize = reader.GetInt32("barnsize");
                        grazingSize = reader.GetInt32("grazesize");
                    }
                    reader.Close();
                }

                int numberOfGoats = 0;

                using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "select COUNT(*) as amount from goats where ownerID = ?discordID";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = ctx.Member.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        numberOfGoats = reader.GetInt32("amount");
                    }
                    reader.Close();
                }
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"Farmer {ctx.Member.DisplayName}'s Profile",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                    {
                        Url = ctx.Member.AvatarUrl
                    },
                    Color = DiscordColor.Aquamarine
                };
                embed.AddField("Credits", credits.ToString(), false);
                embed.AddField("Total number of Goats in Barn", numberOfGoats.ToString(), false);
                embed.AddField("Barn Size", barnSize.ToString(), true);
                embed.AddField("Grazing space", $"Space for {grazingSize} goats", true);
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        [Command("spawngoat")]
        [OwnerOrPermission(DSharpPlus.Permissions.KickMembers)]
        [Hidden]
        public async Task SpawnGoat(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message).ConfigureAwait(false);
            await SpawnRandomGoat(ctx);
        }

        [Command("equip")]
        [Description("Equip a goat as your current goat")]
        [Hidden]
        [OwnerOrPermission(DSharpPlus.Permissions.KickMembers)]
        public async Task EquipGoat(CommandContext ctx)
        {
            try
            {
                List<Goat> goats = new List<Goat>();
                using(MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    string query = "Select * from goats where ownerID = ?ownerId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ctx.Member.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Goat goat = new Goat();
                            goat.id = reader.GetInt32("id");
                            goat.level = reader.GetInt32("level");
                            goat.name = reader.GetString("name");
                            goat.type = (Models.Type)Enum.Parse(typeof(Models.Type), reader.GetString("type"));
                            goat.breed = (Breed)Enum.Parse(typeof(Breed), reader.GetString("breed"));
                            goat.baseColour = (BaseColour)Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"));
                            goat.levelMulitplier = reader.GetDecimal("levelMultiplier");
                            goat.equiped = reader.GetBoolean("equipped");
                            goat.experience = reader.GetDecimal("experience");
                            goats.Add(goat);
                        }
                    }
                    reader.Close();
                }
                if (goats.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("You currently don't own any goats that can be equipped").ConfigureAwait(false);
                }
                else
                {
                    List<Page> pages = new List<Page>();
                    var interactivity = ctx.Client.GetInteractivity();
                    DiscordEmoji backward = DiscordEmoji.FromName(ctx.Client, ":arrow_backward:");
                    DiscordEmoji forward = DiscordEmoji.FromName(ctx.Client, ":arrow_forward:");
                    DiscordEmoji equipBarn = DiscordEmoji.FromName(ctx.Client, ":1barn:");

                    foreach (var goat in goats)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = goat.id.ToString(),
                        };
                        embed.AddField("Name", goat.name, true);
                        embed.AddField("Level", goat.level.ToString(), true);
                        embed.AddField("Experience", goat.experience.ToString(), true);
                        Page page = new Page
                        {
                            Embed = embed
                        };
                        pages.Add(page);
                    }
                    int pageCounter = 0;
                    var msg = await ctx.Channel.SendMessageAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                    SetEquipTimer();
                    while (equipTimerrunning)
                    {
                        await msg.CreateReactionAsync(backward).ConfigureAwait(false);
                        await msg.CreateReactionAsync(forward).ConfigureAwait(false);
                        await msg.CreateReactionAsync(equipBarn).ConfigureAwait(false);

                        var result = await interactivity.WaitForReactionAsync(x => x.Channel == ctx.Channel && x.User == ctx.User
                        && (x.Emoji == backward || x.Emoji == forward || x.Emoji == equipBarn), TimeSpan.FromMinutes(4));

                        if (result.TimedOut)
                        {
                            equipTimerrunning = false;
                        }
                        else if (result.Result.Emoji == backward)
                        {
                            if ((pageCounter - 1) < 0)
                            {
                                pageCounter = pages.Count - 1;
                            }
                            else
                            {
                                pageCounter--;
                            }
                            await msg.DeleteReactionAsync(backward, ctx.User).ConfigureAwait(false);
                            await msg.ModifyAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                        }
                        else if (result.Result.Emoji == forward)
                        {
                            if ((pageCounter + 1) >= pages.Count)
                            {
                                pageCounter = 0;
                            }
                            else
                            {
                                pageCounter++;
                            }
                            await msg.DeleteReactionAsync(forward, ctx.User).ConfigureAwait(false);
                            await msg.ModifyAsync(embed: pages[pageCounter].Embed).ConfigureAwait(false);
                        }
                        else if (result.Result.Emoji == equipBarn)
                        {
                            if (!int.TryParse(pages[pageCounter].Embed.Title, out int id))
                            {
                                await ctx.Channel.SendMessageAsync("Something went wrong while trying to equip this goat").ConfigureAwait(false);
                                return;
                            }
                            using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                            {
                                string query = "Update goats set equipped = 0 where ownerID = ?ownerId";
                                var command = new MySqlCommand(query, connection);
                                command.Parameters.Add("?ownerId", MySqlDbType.VarChar).Value = ctx.User.Id;
                                connection.Open();
                                command.ExecuteNonQuery();
                            }

                            using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                            {
                                string query = "Update goats set equipped = 1 where id = ?id";
                                var command = new MySqlCommand(query, connection);
                                command.Parameters.Add("?id", MySqlDbType.Int32).Value = id;
                                connection.Open();
                                command.ExecuteNonQuery();
                            }
                            await ctx.Channel.SendMessageAsync("Goat is now equipped").ConfigureAwait(false);
                            //pages.RemoveAt(pageCounter);
                            await msg.DeleteAllReactionsAsync().ConfigureAwait(false);
                            equipTimerrunning = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        public async Task SpawnRandomGoat(CommandContext ctx) {
            Random rnd = new Random();
            int breed = rnd.Next(0, 2);
            int baseColour = rnd.Next(0, 4);
            var randomGoat = new Goat();
            randomGoat.baseColour = (BaseColour)Enum.Parse(typeof(BaseColour), Enum.GetName(typeof(BaseColour), baseColour));
            randomGoat.breed = (Breed)Enum.Parse(typeof(Breed), Enum.GetName(typeof(Breed), breed));
            randomGoat.type = Models.Type.Kid;
            randomGoat.level = RandomLevel.GetRandomLevel();
            randomGoat.levelMulitplier = 1;
            randomGoat.name = "Goaty McGoatFace";
            randomGoat.special = false;

            var embed = new DiscordEmbedBuilder
            {
                Title = $"{randomGoat.name} has spawned, type purchase to purchase her",
                Color = DiscordColor.Aquamarine
            };
            embed.AddField("Colour", Enum.GetName(typeof(BaseColour), randomGoat.baseColour), false);
            embed.AddField("Breed", Enum.GetName(typeof(Breed), randomGoat.breed).Replace("_", " "), true);
            embed.AddField("Level", randomGoat.level.ToString(), true);

            var interactivtiy = ctx.Client.GetInteractivity();

            var goatMsg = await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            var msg = await interactivtiy.WaitForMessageAsync(x => x.Channel == ctx.Channel
            && x.Content.ToLower().Trim() == "purchase", TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            await goatMsg.DeleteAsync();
            if (msg.TimedOut)
            {
                await ctx.Channel.SendMessageAsync($"No one managed to purchase {randomGoat.name}").ConfigureAwait(false);
                return;
            }
            else if (!goatService.CanGoatFitInBarn(msg.Result.Author.Id))
            {
                DiscordMember member = await ctx.Guild.GetMemberAsync(msg.Result.Author.Id);
                await ctx.Channel.SendMessageAsync($"Unfortunately {member.DisplayName} your barn is full and the goat has now escaped!")
                    .ConfigureAwait(false);
            }
            else
            {
                if (!DoesUserHaveCharacter(msg.Result.Author.Id))
                {
                    using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "INSERT INTO farmers (DiscordID) VALUES (?discordID)";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = msg.Result.Author.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }
                await msg.Result.DeleteAsync();
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "INSERT INTO goats (level, name, type, breed, baseColour, ownerID, experience) " +
                            "VALUES (?level, ?name, ?type, ?breed, ?baseColour, ?ownerID, ?exp)";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?level", MySqlDbType.Int32).Value = randomGoat.level;
                        command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = randomGoat.name;
                        command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                        command.Parameters.Add("?breed", MySqlDbType.VarChar).Value = Enum.GetName(typeof(Breed), randomGoat.breed);
                        command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value = Enum.GetName(typeof(BaseColour), randomGoat.baseColour);
                        command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
                        command.Parameters.Add("?exp", MySqlDbType.Decimal).Value =
                                (int)Math.Ceiling(10 * Math.Pow(1.05, (randomGoat.level - 1)));
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    await ctx.Channel.SendMessageAsync($"Congrats " +
                        $"{ctx.Guild.GetMemberAsync(msg.Result.Author.Id).Result.DisplayName} you caught " +
                        $"{randomGoat.name}").ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.Out.WriteLine(ex.Message);
                    Console.Out.WriteLine(ex.StackTrace);
                }
            }
        }

        private bool DoesUserHaveCharacter(ulong discordID)
        {
            bool hasCharacter = false;
            using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select * from farmers where DiscordID = ?discordID";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordID", MySqlDbType.VarChar, 40).Value = discordID;
                connection.Open();
                var reader = command.ExecuteReader();
                hasCharacter = reader.HasRows;
                reader.Close();
            }
            return hasCharacter;
        }

        //if (reader.HasRows)
        //{
        //    while(reader.Read())
        //    {
        //        Goat newGoat = new Goat();
        //        newGoat.id = reader.GetInt32("id");
        //        newGoat.level = reader.GetInt32("level");
        //        newGoat.levelMulitplier = reader.GetDecimal("levelMultiplier");
        //        newGoat.name = reader.GetString("name");
        //        newGoat.baseColour = (BaseColour)Enum.Parse(typeof(BaseColour), reader.GetString("baseColour"), true);
        //        newGoat.breed = (Breed)Enum.Parse(typeof(Breed), reader.GetString("breed"), true);
        //        newGoat.type = (Models.Type)Enum.Parse(typeof(Models.Type), reader.GetString("type"), true);
        //    }
        //}
    }
}
