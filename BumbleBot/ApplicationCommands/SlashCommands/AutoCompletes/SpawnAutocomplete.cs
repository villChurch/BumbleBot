using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using Dapper;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.Entities;
using MySqlConnector;

namespace BumbleBot.ApplicationCommands.SlashCommands.AutoCompletes;

public class SpawnAutocomplete : IAutocompleteProvider
{
    private readonly DbUtils dbUtils = new();
    
    public Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext context)
    {
        var options = new List<DiscordApplicationCommandAutocompleteChoice>();
        List<SpecialGoats> variations;
        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            connection.Open();
            variations = connection.Query<SpecialGoats>("select * from specialgoats group by variation").ToList();
        }
        variations.ForEach(delegate(SpecialGoats variation)
        {
            options.Add(new DiscordApplicationCommandAutocompleteChoice(variation.variation, variation.variation));
        });
        return Task.FromResult(options.AsEnumerable());
    }
}