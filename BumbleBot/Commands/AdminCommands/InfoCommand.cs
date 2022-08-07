using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Utilities;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using BumbleBot.ApplicationCommands.SlashCommands.AutoCompletes;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.Extensions;
using MySql.Data.MySqlClient;
using Dapper;

namespace BumbleBot.Commands.AdminCommands
{
    public class InfoCommand : ApplicationCommandsModule
    {
        private DbUtils dbUtils = new();

        [SlashCommand("info_add", "Add a new info prompt")]
        [OwnerOrPermissionSlash(DisCatSharp.Permissions.KickMembers)]
        public async Task AddNewInfo(InteractionContext ctx)
        {
            var id = RandomID(8);
            var nameId = RandomID(8);
            var valueId = RandomID(8);
            var modal = await ctx.Interaction.CreatePaginatedModalResponseAsync(
                new List<ModalPage>()
                {
                    new ModalPage(new DisCatSharp.Entities.DiscordInteractionModalBuilder()
                .WithCustomId(id)
                .WithTitle("Info prompt to add")
                .AddModalComponents(new DisCatSharp.Entities.DiscordTextComponent(DisCatSharp.Enums.TextComponentStyle.Small,
                nameId,"Name"))
                .AddModalComponents(new DisCatSharp.Entities.DiscordTextComponent(DisCatSharp.Enums.TextComponentStyle.Paragraph,
                valueId, "Value")))
                });

            if (modal.TimedOut)
            {
                await modal.Interaction.EditOriginalResponseAsync(new DisCatSharp.Entities.DiscordWebhookBuilder()
                    .WithContent("Interaction has timed out"));
            }
            else
            {
                var info = new Info()
                {
                    Name = modal.Responses[nameId],
                    Value = modal.Responses[valueId]
                };
                await using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    try
                    {
                        const string query = "Insert into info (name, value) VALUES (?name, ?value)";
                        await connection.OpenAsync();
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("?name", info.Name);
                        command.Parameters.AddWithValue("?value", info.Value);
                        await command.ExecuteNonQueryAsync();
                        await modal.Interaction.EditOriginalResponseAsync(new DisCatSharp.Entities.DiscordWebhookBuilder()
                            .WithContent($"Created info command {info.Name}."));
                    } catch (MySqlException mse)
                    {
                        await modal.Interaction.EditOriginalResponseAsync(new DisCatSharp.Entities.DiscordWebhookBuilder()
                            .WithContent($"Info command already exists for {info.Name}"));
                    }
                    finally
                    {
                        await connection.CloseAsync();
                    }
                }
            }
        }

        [SlashCommand("info", "Display information on a topic")]
        public async Task DisplayInfo(InteractionContext ctx, [Autocomplete(typeof(InfoAutoComplete))]
            [Option("name", "Name of Info command to show", true)] string name)
        {
            await ctx.CreateResponseAsync(DisCatSharp.InteractionResponseType.DeferredChannelMessageWithSource);
            List<Info> infos;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                connection.Open();
                infos = connection.Query<Info>("select * from info").ToList();
            }
            if (infos.Count < 1)
            {
                await ctx.EditResponseAsync(new DisCatSharp.Entities.DiscordWebhookBuilder()
                    .WithContent($"No info value for {name}"));
            }
            else
            {
                var info = infos.Where(i => i.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
                await ctx.EditResponseAsync(new DisCatSharp.Entities.DiscordWebhookBuilder()
                    .WithContent(info.First().Value));
            }
        }

        [SlashCommand("info_delete", "Delete an info prompt")]
        public async Task DeleteInfo(InteractionContext ctx, [Autocomplete(typeof(InfoAutoComplete))]
            [Option("name", "Name of Info command to delete", true)] string name)
        {
            await ctx.CreateResponseAsync(DisCatSharp.InteractionResponseType.DeferredChannelMessageWithSource);
            List<Info> infos;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                connection.Open();
                infos = connection.Query<Info>("select * from info").ToList();
            }
            if (infos.Count < 1)
            {
                await ctx.EditResponseAsync(new DisCatSharp.Entities.DiscordWebhookBuilder()
                    .WithContent($"No info value for {name}"));
            }
            else
            {
                using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "delete from info where name = ?name";
                    connection.Open();
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("?name", name);
                    command.ExecuteNonQuery();
                    connection.Close();
                }
                await ctx.EditResponseAsync(new DisCatSharp.Entities.DiscordWebhookBuilder()
                    .WithContent($"Deleted info prompt {name}"));
            }
        }

        private String RandomID(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}

