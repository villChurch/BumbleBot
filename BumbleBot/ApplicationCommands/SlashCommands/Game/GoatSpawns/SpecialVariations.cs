using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.ApplicationCommands.SlashCommands.AutoCompletes;
using BumbleBot.Attributes;
using BumbleBot.Models;
using BumbleBot.Services;
using BumbleBot.Utilities;
using Dapper;
using DisCatSharp;
using DisCatSharp.Enums;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.Entities;
using MySql.Data.MySqlClient;
#pragma warning disable CS4014

namespace BumbleBot.ApplicationCommands.SlashCommands.Game.GoatSpawns;

public class SpecialVariations : ApplicationCommandsModule
{
    private DbUtils dbUtils = new();

    private GoatSpawningService goatSpawningService;

    public SpecialVariations(GoatSpawningService goatSpawningService)
    {
        this.goatSpawningService = goatSpawningService;
    }


    [OwnerOrPermissionSlash(Permissions.KickMembers)]
    [SlashCommand("spawn_goat", "Spawns a goat")]
    public async Task SpawnGoat(InteractionContext ctx,
        [Option("channel", "channel to spawn in"), ChannelTypes(ChannelType.Text)] DiscordChannel channel,
        [Autocomplete(typeof(SpawnAutocomplete))]
        [Option("variation", "Variation to spawn. Will Spawn normal goat if blank.", true)]
        string variation = "normal")
    {
        if (variation.Equals("normal"))
        {
            _ = Task.Run(() =>
            {
                var normalGoatToSpawn = goatSpawningService.GenerateNormalGoatToSpawn();
                goatSpawningService.SpawnGoatFromGoatObject(channel, ctx.Guild, normalGoatToSpawn, ctx.Client);
            });
        }
        else
        {
            var variationList = new List<SpecialGoats>();
            await using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "select * from specialgoats where variation = ?Variation";
                var parameters = new { Variation = variation };
                connection.Open();
                variationList = connection.Query<SpecialGoats>(query, parameters).ToList();
            }

            var chosenSpecialGoat = variationList[new Random().Next(variationList.Count)];
            var chosenSpecialGoatObject =
                GenerateSpecialGoatToSpawn(chosenSpecialGoat.kidFileLink, chosenSpecialGoat.variation);
            _ = Task.Run(() =>
            {
                goatSpawningService.SpawnGoatFromGoatObject(channel, ctx.Guild, chosenSpecialGoatObject, ctx.Client);
            });
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"Spawned a {variation} goat in {channel.Mention}."));
    }
    
    [OwnerOrPermissionSlash(Permissions.KickMembers)]
    [SlashCommand("add_spawn", "Adds a new special for spawn")]
    public async Task AddNewSpecialVariation(InteractionContext ctx,
        [Option("variation", "Name of the variation, eg. Valentines or Christmas")] string variation,
        [Option("KidFilePath", "Path to the Kid file")] string kidFilePath,
        [Option("AdultFilePath", "Path to Adult file")] string adultFilePath)
    {
        await using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            const string query =
                "insert into specialgoats (variation, KidFileLink, AdultFileLink) values (?variation, ?kidFilePath, ?adultFilePath)";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("?variation", variation);
            command.Parameters.AddWithValue("?kidFilePath", kidFilePath);
            command.Parameters.AddWithValue("?adultFilePath", adultFilePath);
            connection.Open();
            command.ExecuteNonQuery();
            await connection.CloseAsync();
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"New {variation} variation added."));
    }
    
    [OwnerOrPermissionSlash(Permissions.KickMembers)]
    [SlashCommand("enable_spawn", "Enables spawning of particular special")]
    public async Task EnableSpecialVariation(InteractionContext ctx, [Autocomplete(typeof(SpawnAutocomplete))]
        [Option("variation", "Variation to enable", true)] string variation)
    {
        await using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            const string query = "update specialgoats set enabled = 1 where variation = ?variation";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("?variation", variation);
            connection.Open();
            command.ExecuteNonQuery();
            await connection.CloseAsync();
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"{variation} spawns are now enabled."));
    }

    [OwnerOrPermissionSlash(Permissions.KickMembers)]
    [SlashCommand("disable_spawn", "Disables spawning of particular special")]
    public async Task DisableSpecialVariation(InteractionContext ctx,
        [Autocomplete(typeof(SpawnAutocomplete))] [Option("variation", "Variation to disable", true)] string variation)
    {
        await using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            const string query = "update specialgoats set enabled = 0 where variation = ?variation";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("?variation", variation);
            connection.Open();
            command.ExecuteNonQuery();
            await connection.CloseAsync();
        }

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent($"{variation} spawns are now disabled."));
    }
    
    private (Goat, string) GenerateSpecialGoatToSpawn(string goatFilePath, string variation)
    {
        var specialGoat = new Goat();
        specialGoat.Breed = (Breed) Enum.Parse(typeof(Breed), variation);
        specialGoat.BaseColour = BaseColour.Special;
        specialGoat.Level = new Random().Next(76, 100);
        specialGoat.Experience = (int) Math.Ceiling(10 * Math.Pow(1.05, specialGoat.Level - 1));
        specialGoat.Name = $"{variation} Goat";
        specialGoat.FilePath = goatFilePath;
        var filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}{specialGoat.FilePath}";
        return (specialGoat, filePath);
    }
}