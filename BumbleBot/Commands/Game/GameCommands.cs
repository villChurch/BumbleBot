using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.Game
{
    public class GameCommands : BaseCommandModule
    {
        private DBUtils dbUtils = new DBUtils();

        [Command("create")]
        [Description("Create your character")]
        [Hidden, RequireOwner]
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
        [Hidden, RequireOwner]
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
                await ctx.Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        [Command("spawngoat")]
        [RequireOwner]
        [Hidden]
        public async Task SpawnGoat(CommandContext ctx)
        {
            await ctx.Channel.DeleteMessageAsync(ctx.Message).ConfigureAwait(false);
            await SpawnRandomGoat(ctx);
        }

        [Command("equip")]
        [Description("Equip a goat as your current goat")]
        [Hidden, RequireOwner]
        public async Task EquipGoat(CommandContext ctx, params string[] goatsName)
        {
            string searchGoat = string.Join(" ", goatsName).Trim();
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
                        string query = "INSERT INTO goats (level, name, type, breed, baseColour, ownerID) " +
                            "VALUES (?level, ?name, ?type, ?breed, ?baseColour, ?ownerID)";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?level", MySqlDbType.Int32).Value = randomGoat.level;
                        command.Parameters.Add("?name", MySqlDbType.VarChar, 255).Value = randomGoat.name;
                        command.Parameters.Add("?type", MySqlDbType.VarChar).Value = "Kid";
                        command.Parameters.Add("?breed", MySqlDbType.VarChar).Value = Enum.GetName(typeof(Breed), randomGoat.breed);
                        command.Parameters.Add("?baseColour", MySqlDbType.VarChar).Value = Enum.GetName(typeof(BaseColour), randomGoat.baseColour);
                        command.Parameters.Add("?ownerID", MySqlDbType.VarChar).Value = msg.Result.Author.Id;
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
