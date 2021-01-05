using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    [Group("pasture")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class GrazingCommands : BaseCommandModule
    {
        private readonly DBUtils dBUtils = new DBUtils();

        public GrazingCommands(GoatService goatService, FarmerService farmerService)
        {
            this.goatService = goatService;
            this.farmerService = farmerService;
        }

        private GoatService goatService { get; }
        private FarmerService farmerService { get; }

        [Command("show")]
        [Description("Show a list of current goats you have grazing")]
        public async Task ShowPasture(CommandContext ctx)
        {
            try
            {
                var ids = new List<int>();
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "select goatId from grazing where farmerId = ?userId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = ctx.User.Id;
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                            ids.Add(reader.GetInt32("goatId"));
                }

                var goats = goatService.ReturnUsersGoats(ctx.User.Id);
                var goatsInPasture = goats.Where(goat => ids.Contains(goat.id)).ToList();
                if (goatsInPasture.Count < 1)
                {
                    await ctx.Channel.SendMessageAsync("You do not have goats out in the pasture yet")
                        .ConfigureAwait(false);
                }
                else
                {
                    var url = "http://williamspires.com/";
                    var pages = new List<Page>();
                    var interactivity = ctx.Client.GetInteractivity();
                    foreach (var goat in goatsInPasture)
                    {
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = $"{goat.id}",
                            ImageUrl = url + goat.filePath.Replace(" ", "%20")
                        };
                        embed.AddField("Name", goat.name);
                        embed.AddField("Level", goat.level.ToString(), true);
                        embed.AddField("Experience", goat.experience.ToString(), true);
                        embed.AddField("Breed", Enum.GetName(typeof(Breed), goat.breed).Replace("_", " "), true);
                        embed.AddField("Colour", Enum.GetName(typeof(BaseColour), goat.baseColour), true);
                        var page = new Page
                        {
                            Embed = embed
                        };
                        pages.Add(page);
                    }

                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("remove")]
        [Description("Remove one or more goats from pasture")]
        public async Task RemoveFromPasture(CommandContext ctx,
            [RemainingText] [Description("IDs of goats to move to pasture seperated by a space")]
            string goatIDs)
        {
            try
            {
                var ids = new List<int>();
                var goats = goatService.ReturnUsersGoats(ctx.User.Id);
                var ownedGoatIds = goats.Select(goat => goat.id).ToList();
                var notYours = ids.Where(id => !ownedGoatIds.Contains(id)).ToList();
                if (!goatIDs.Contains(" "))
                    ids.Add(int.Parse(goatIDs));
                else
                    ids = goatIDs.Split(' ').Select(int.Parse).ToList();
                ownedGoatIds = goats.Select(goat => goat.id).ToList();
                notYours = ids.Where(id => !ownedGoatIds.Contains(id)).ToList();
                if (notYours.Count > 0)
                {
                    await ctx.Channel.SendMessageAsync("Looks like one or more of the id's provided are not your goats")
                        .ConfigureAwait(false);
                }
                else
                {
                    goats.Where(goat => ids.Contains(goat.id)).ToList().ForEach(goat =>
                    {
                        using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                        {
                            var query = "delete from grazing where goatId = ?goatId";
                            var command = new MySqlCommand(query, connection);
                            command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goat.id;
                            connection.Open();
                            command.ExecuteNonQuery();
                        }
                    });
                    var goatOrGoats = ids.Count <= 1 ? "goat has" : "goats have";
                    await ctx.Channel.SendMessageAsync($"{ids.Count} {goatOrGoats} have been removed from your pasture")
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }

        [Command("add")]
        [Description("Move one or more goats to the pasture")]
        public async Task MoveToPasture(CommandContext ctx,
            [RemainingText] [Description("IDs of goats to move to pasture seperated by a space")]
            string goatIDs)
        {
            try
            {
                var ids = new List<int>();
                var goats = goatService.ReturnUsersGoats(ctx.User.Id);
                var ownedGoatIds = goats.Select(goat => goat.id).ToList();
                var notYours = ids.Where(id => !ownedGoatIds.Contains(id)).ToList();
                if (!goatIDs.Contains(" "))
                    ids.Add(int.Parse(goatIDs));
                else
                    ids = goatIDs.Split(' ').Select(int.Parse).ToList();
                ownedGoatIds = goats.Select(goat => goat.id).ToList();
                notYours = ids.Where(id => !ownedGoatIds.Contains(id)).ToList();
                if (notYours.Count > 0)
                {
                    await ctx.Channel.SendMessageAsync("Looks like one or more of the id's provided are not your goats")
                        .ConfigureAwait(false);
                }
                else
                {
                    var grazingSize = 0;
                    var currentlyGrazing = 0;
                    var farmer = farmerService.ReturnFarmerInfo(ctx.User.Id);
                    grazingSize = farmer.grazingspace;
                    using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        var query = "select COUNT(*) as goatsGrazing from grazing where farmerId = ?farmerId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?farmerId", MySqlDbType.VarChar).Value = ctx.User.Id;
                        connection.Open();
                        var reader = command.ExecuteReader();
                        if (reader.HasRows)
                            while (reader.Read())
                                currentlyGrazing = reader.GetInt32("goatsGrazing");
                        reader.Close();
                    }

                    var remainingSpace = grazingSize - currentlyGrazing;
                    if (remainingSpace > goats.Count)
                    {
                        await ctx.Channel.SendMessageAsync(
                            "You have provided more goats than can fit in the pasture. " +
                            $"Your remaining pasture space is {remainingSpace}.").ConfigureAwait(false);
                    }
                    else if (remainingSpace > 0)
                    {
                        goats.Where(goat => ids.Contains(goat.id)).ToList().ForEach(goat =>
                        {
                            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                            {
                                var query = "replace into grazing (goatId, farmerId) Values (?goatId, ?farmerId)";
                                var command = new MySqlCommand(query, connection);
                                command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goat.id;
                                command.Parameters.Add("?farmerId", MySqlDbType.VarChar).Value = ctx.User.Id;
                                connection.Open();
                                command.ExecuteNonQuery();
                            }
                        });
                        var goatOrGoats = ids.Count <= 1 ? "goat has" : "goats have";

                        await ctx.Channel
                            .SendMessageAsync($"{ids.Count} {goatOrGoats} now been moved into your pasture")
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await ctx.Channel.SendMessageAsync("You currently have no more pasture space available")
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (FormatException fe)
            {
                await ctx.Channel.SendMessageAsync("It appears you have entered a non numeric value for a goat id")
                    .ConfigureAwait(false);
                Console.Out.WriteLine(fe.StackTrace);
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
            }
        }
    }
}