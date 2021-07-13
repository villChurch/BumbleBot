using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using MySqlConnector;

namespace BumbleBot.Commands.Game
{
    [Group("perks")]
    [Description("For details of perks and other perk commands")]
    public class PerkCommands : BaseCommandModule
    {
        private DbUtils dbUtils = new DbUtils();
        
        [GroupCommand]
        public async Task ShowAllPerks(CommandContext ctx)
        {
            List<Perks> allPerks = new List<Perks>();
            using (var con = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select * from perks order by levelUnlocked, perkName ASC";
                var command = new MySqlCommand(query, con);
                con.Open();
                var reader = await command.ExecuteReaderAsync();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        var perk = new Perks
                        {
                            id = reader.GetInt16("id"),
                            perkName = reader.GetString("perkName"),
                            perkBonusText = reader.GetString("perkBonusText"),
                            perkCost = reader.GetInt16("perkCost"),
                            levelUnlocked = reader.GetInt16("levelUnlocked")
                        };
                        allPerks.Add(perk);
                    }
                }
            }
            var pages = new List<Page>();
            foreach (var perk in allPerks)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{perk.perkName}",
                    Color = DiscordColor.Aquamarine
                };
                embed.AddField("Description", perk.perkBonusText);
                embed.AddField("Perk Point Cost", perk.perkCost.ToString());
                embed.AddField("Level Unlocked", perk.levelUnlocked.ToString());
                var page = new Page {Embed = embed};
                pages.Add(page);
            }
            var interactivity = ctx.Client.GetInteractivity();
            _ = Task.Run(async () =>await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages).ConfigureAwait(false));
        }
    }
}