using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using Dapper;
using DisCatSharp.CommandsNext;
using MySqlConnector;
using Type = BumbleBot.Models.Type;

namespace BumbleBot.Services;

public class MilkService
{
    private readonly DbUtils dbUtils = new();
    public async Task MilkGoats(CommandContext ctx, ulong userId)
    {
        Farmer? farmer;
        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            connection.Open();
            farmer = connection.QueryFirst<Farmer>("select * from farmers where DiscordID = @discordID",
                new { discordID = userId });
        }

        if (farmer is null)
        {
            await ctx.RespondAsync($"Farmer not found for member with id: {userId}");
            return;
        }

        List<Goat>? farmersGoats;
        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            connection.Open();
            farmersGoats = connection.Query<Goat>("select * from goats where ownerID = @ownerID",
                new { ownerID = userId }).Where(goat => goat.Type == Type.Adult && goat.Breed != Breed.Buck).ToList();
        }

        List<int>? cookingDoesIds;
        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            connection.Open();
            cookingDoesIds = connection.Query<CookingDoes>("select * from cookingdoes").Select(x => x.goatId).ToList();
        }
        if (cookingDoesIds.Count > 0)
        {
            farmersGoats.ForEach(goat =>
            {
                if (cookingDoesIds.Contains(goat.Id))
                {
                    farmersGoats.Remove(goat);
                }
            });
        }
        if (farmersGoats.Count < 1)
        {
            await ctx.RespondAsync("You don't have any goats that can be milked");
            return;
        }

        List<int>? grazingGoatsIds;
        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            connection.Open();
            grazingGoatsIds = connection.Query<Grazing>("select * from grazing where farmerId = @farmerId",
                new { farmerId = userId }).Select(x => x.goatId).ToList();
        }

        List<Goat>? boostedGoats = new List<Goat>();
        if (grazingGoatsIds.Count > 0)
        {
            boostedGoats.AddRange(farmersGoats.Where(x => grazingGoatsIds.Contains(x.Id)));
        }

        List<Goat>? dazzles = farmersGoats.FindAll(x => x.Breed == Breed.Dazzle);
        List<Goat>? naughtyDazzles = new List<Goat>();
        dazzles.ForEach(dazzle =>
        {
            var randomNumber = new Random().Next(6);
            if (randomNumber != 2) return;
            naughtyDazzles.Add(dazzle);
            dazzles.Remove(dazzle);
        });
        farmersGoats = farmersGoats.Except(boostedGoats).Except(dazzles).Except(naughtyDazzles).ToList();
        boostedGoats = boostedGoats.Except(dazzles).Except(naughtyDazzles).ToList();
        double milkAmount = farmersGoats.Sum(goat => (goat.Level - 99) * 0.3);
        milkAmount += boostedGoats.Sum(goat => (goat.Level - 99) * 0.3) * 1.25;
        milkAmount += dazzles.Sum(goat => (goat.Level - 99) * 0.3) * 1.5;
        List<FarmerPerks>? farmerPerksIdList = null;
        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            connection.Open();
            farmerPerksIdList = connection.Query<FarmerPerks>("select * from farmerperks where farmerid = @discordID",
                new { discordID = userId }).ToList();
        }
        // sort out mastits etc...
    }
}