using System;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.AdminCommands
{
    [Group("config")]
    [Hidden]
    public class ConfigCommands : BaseCommandModule
    {
        private readonly DbUtils dbUtils = new DbUtils();

        [Command("christmas")]
        [Description("Enables or disables christmas specials")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetChristmasSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "christmasSpecials");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Christmas spawns have been {enabledOrDisabled}.")
                .ConfigureAwait(false);
        }

        [Command("november")]
        [Description("Enables or disables november special spawns")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetNovemberSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "novemberSpecials");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"November spawns have been {enabledOrDisabled}.")
                .ConfigureAwait(false);
        }
        
        [Command("birthday")]
        [Description("Enables or disables birthday special spawn")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetBotBirthdaySpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "botBirthdayEnabled");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Bot anniversary spawn has been {enabledOrDisabled}.")
                .ConfigureAwait(false); 
        }

        [Command("halloween")]
        [Description("Disable or enabled halloween special spawns")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetHalloweenSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "halloweenEnabled");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Halloween specials have been {enabledOrDisabled}.")
                .ConfigureAwait(false); 
        }

        [Command("buck")]
        [Description("Disable or enable buck special spawns")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetBuckSpecialSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "buckSpecials");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Buck spawns have been {enabledOrDisabled}.")
                .ConfigureAwait(false); 
        }
        
        [Command("summer")]
        [Description("Disable or enable dairy special spawns")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetSummerSpecialSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "summerEnabled");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Summer special spawns have been {enabledOrDisabled}.")
                .ConfigureAwait(false); 
        }
        
        [Command("dairyspecial")]
        [Description("Disable or enable dairy special spawns")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetDairySpecialSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "dairySpecials");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Dairy special spawns have been {enabledOrDisabled}.")
                .ConfigureAwait(false); 
        }
        
        [Command("memberspecial")]
        [Description("Disable or enabled member special spawns")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetMemberSpecialSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "memberSpecials");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Member special spawns have been {enabledOrDisabled}.")
                .ConfigureAwait(false); 
        }
        
        [Command("dazzle")]
        [Description(("Disable or enable Dazzle spawns"))]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetDazzleSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "bestestGoat");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Dazzle spawns have been {enabledOrDisabled}.")
                .ConfigureAwait(false); 
        }
        [Command("spring")]
        [Description("Disable or enable spring spawns")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetSpringSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "springSpecials");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Spring spawns have been {enabledOrDisabled}.")
                .ConfigureAwait(false);
        }
        [Command("shamrock")]
        [Description("Disable or enable shamrock spawns")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetShamrockSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "paddysSpecials");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Shamrock spawns have been {enabledOrDisabled}.")
                .ConfigureAwait(false);
        }
        [Command("valentine")]
        [Aliases("valentines")]
        [Description("Disable or enable valentine spawns")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetValentineSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "valentinesSpecials");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Valentine spawns have been {enabledOrDisabled}.")
                .ConfigureAwait(false);
        }
        
        [Command("tailless")]
        [Aliases("ts")]
        [Description("Disable or enable tailless spawns")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetTaillessSpawnVariable(CommandContext ctx, bool enabled)
        {
            await EnableOrDisableSpecialSpwan(enabled, "taillessEnabled");
            var enabledOrDisabled = enabled ? "enabled" : "disabled";
            await ctx.Channel.SendMessageAsync($"Tailless spawns have been {enabledOrDisabled}.").ConfigureAwait(false);
        }

        private async Task EnableOrDisableSpecialSpwan(bool enabled, string special)
        {
            await using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "update config SET boolValue = ?value where paramName = ?param";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?value", enabled);
                command.Parameters.AddWithValue("?param", special);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }
        
        [Command("goatspawns")]
        [Aliases("gs", "gsc")]
        [Description("Sets the channel for goats to spawn in")]
        [OwnerOrPermission(Permissions.KickMembers)]
        public async Task SetGoatSpawnChannel(CommandContext ctx, DiscordChannel discordChannel)
        {
            try
            {
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Update config SET stringResponse = ?spawnChannel where paramName = ?paramName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?spawnChannel", MySqlDbType.VarChar).Value = discordChannel.Id;
                    command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "spawnChannel";
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync($"Goats will spawn in {discordChannel.Mention}")
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("asshole")]
        [Hidden]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task EnableAssholeMode(CommandContext ctx)
        {
            try
            {
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Update config SET boolValue = ?boolValue where paramName = ?paramName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?boolValue", MySqlDbType.Int16).Value = 1;
                    command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeMode";
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                await ctx.Channel.SendMessageAsync("Parameter set").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("assholeresponse")]
        [Aliases("ar")]
        [Hidden]
        [OwnerOrPermission(Permissions.Administrator)]
        public async Task SetAssholeResponse(CommandContext ctx)
        {
            try
            {
                var currentResponse = "";
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "Select stringResponse from config where paramName = ?paramName";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeResponse";
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                            currentResponse = reader.GetString("stringResponse");
                    reader.Close();
                }

                var interactivity = ctx.Client.GetInteractivity();
                await ctx.Channel.SendMessageAsync($"Current reponse is: {currentResponse} Please enter the new one")
                    .ConfigureAwait(false);
                var responseMsg = await interactivity.WaitForMessageAsync(
                    x => x.Author == ctx.Message.Author && x.Channel == ctx.Channel,
                    TimeSpan.FromMinutes(5));
                if (responseMsg.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync("This command has timed out").ConfigureAwait(false);
                }
                else
                {
                    using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                    {
                        var query = "Update config set stringResponse = ?stringResponse where paramName = ?paramName";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?stringResponse", MySqlDbType.VarChar).Value =
                            responseMsg.Result.Content;
                        command.Parameters.Add("?paramName", MySqlDbType.VarChar).Value = "assholeResponse";
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                    await ctx.Channel.SendMessageAsync("Response now updated").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}