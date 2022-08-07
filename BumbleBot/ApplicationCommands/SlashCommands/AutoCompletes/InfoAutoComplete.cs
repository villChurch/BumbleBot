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

namespace BumbleBot.ApplicationCommands.SlashCommands.AutoCompletes
{
    public class InfoAutoComplete
    {
        private readonly DbUtils dbUtils = new();

        public Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext context)
        {
            var options = new List<DiscordApplicationCommandAutocompleteChoice>();
            List<Info> infos;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                connection.Open();
                infos = connection.Query<Info>("select * from info").ToList();
            }
            infos.Where(i => i.Name.StartsWith(context.Options[0].Value?.ToString(), System.StringComparison.CurrentCultureIgnoreCase)).ToList().ForEach(delegate (Info info)
            {
                options.Add(new DiscordApplicationCommandAutocompleteChoice(info.Name, info.Name));
            });
            return Task.FromResult(options.AsEnumerable());
        }
    }
}
