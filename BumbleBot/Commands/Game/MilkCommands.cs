using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Linq;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.Game
{
    [Group("Milk")]
    [Hidden]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class MilkCommands : BaseCommandModule
    {
        private DBUtils dbUtils = new DBUtils();
        GoatService goatService { get; }

        public MilkCommands(GoatService goatService)
        {
            this.goatService = goatService;
        }

        [GroupCommand]
        public async Task MilkGoats(CommandContext ctx)
        {
            try
            {
                List<Goat> milkableGoats = goatService.ReturnUsersGoats(ctx.User.Id).Where(x => x.level >= 100).ToList();

                if (milkableGoats.Count <= 0)
                {
                    await ctx.Channel.SendMessageAsync("You currently don't have any adult goats that can be milked").ConfigureAwait(false);
                }
                else
                {
                    double milkGained = 0;
                    decimal currentMilk = 0;
                    milkableGoats.ForEach(x => milkGained += ((x.level - 100) * 0.3));
                    using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "select milk from farmers where DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                        connection.Open();
                        var reader = command.ExecuteReader();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                currentMilk = reader.GetDecimal("milk");
                            }
                        }
                        reader.Close();
                    }
                    currentMilk += Convert.ToDecimal(milkGained);
                    using (MySqlConnection connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
                    {
                        string query = "Update farmers SET milk = ?milk where DiscordID = ?discordId";
                        var command = new MySqlCommand(query, connection);
                        command.Parameters.Add("?milk", MySqlDbType.Decimal).Value = currentMilk;
                        command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    await ctx.Channel.SendMessageAsync($"You have successfully milked {milkableGoats.Count} goats and " +
                        $"gained {milkGained} lbs of milk").ConfigureAwait(false);
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
