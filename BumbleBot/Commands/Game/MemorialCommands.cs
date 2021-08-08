using System.Threading.Tasks;
using BumbleBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Linq;
using BumbleBot.Attributes;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Commands.Game
{
    [Group("memorial")]
    [IsUserAvailable]
    public class MemorialCommands : BaseCommandModule
    {

        GoatService goatService;
        FarmerService farmerService;
        private readonly DbUtils dBUtils = new DbUtils();

        public MemorialCommands(GoatService goatService, FarmerService farmerService)
        {
            this.goatService = goatService;
            this.farmerService = farmerService;
        }
        
        [Command("rename")]
        [Description("Rename a dead goat for 250 credits")]
        private async Task RenameMemorialGoat(CommandContext ctx, int goatId, [RemainingText] string goatName)
        {
            var deadGoats = goatService.ReturnUsersDeadGoats(ctx.User.Id);
            var credits = farmerService.ReturnFarmerInfo(ctx.User.Id).Credits;
            if (deadGoats.Any(dg => dg.Id == goatId) && credits > 250)
            {
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    const string query = "Update deadgoats Set name = ?name where goatId = ?goatId";
                    var command = new MySqlCommand(query, connection);
                    connection.Open();
                    command.Parameters.Add("?name", MySqlDbType.VarChar).Value = goatName;
                    command.Parameters.Add("?goatId", MySqlDbType.Int32).Value = goatId;
                    command.ExecuteNonQuery();
                }

                farmerService.DeductCreditsFromFarmer(ctx.User.Id, 250);
                await ctx.Channel.SendMessageAsync($"{deadGoats.Find(dg => dg.Id == goatId)?.Name}" +
                                                   $" has been renamed to {goatName}.").ConfigureAwait(false);
            }
            else
            {
                await ctx.Channel
                    .SendMessageAsync(
                        $"Either no goat was found in your memorial with id {goatId} or you do not " +
                        $"have the 250 credit fee needed to perform this action.")
                    .ConfigureAwait(false);
            }
        }
    }
}