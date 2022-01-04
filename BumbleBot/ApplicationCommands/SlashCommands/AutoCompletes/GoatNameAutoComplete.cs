using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Services;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace BumbleBot.ApplicationCommands.SlashCommands.AutoCompletes;

public class GoatNameAutoComplete : IAutocompleteProvider
{
    public Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext context)
    {
        var goatService = context.Services.GetService<GoatService>() ?? new GoatService();
        var goats = goatService.ReturnUsersGoats(context.User.Id).Where(goat => goat.Level > 99).OrderBy(goat => goat.Level).ToList();
        if (!goats.Any())
        {
            return Task.FromResult(new List<DiscordApplicationCommandAutocompleteChoice>().AsEnumerable());
        }
        var searchGoats = goats.Where(goat => goat.Name.StartsWith(context.Options[0].Value.ToString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase));
        var options = searchGoats.Take(25).Select(goat => new DiscordApplicationCommandAutocompleteChoice(goat.Name, goat.Id)).ToList();

        return Task.FromResult(options.AsEnumerable());
    }
}