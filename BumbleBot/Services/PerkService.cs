using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using MySqlConnector;

namespace BumbleBot.Services
{
    public class PerkService
    {
        private readonly DbUtils dBUtils = new();

        public async Task AddPerkToUser(ulong userId, Perks perkToAdd, int currentPerkPoints)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "insert into farmerperks (farmerid, perkid) values (?farmerid, ?perkid)";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?farmerid", userId);
                command.Parameters.AddWithValue("?perkid", perkToAdd.id);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }

            await SubtractPerkPointsFromUser(userId, perkToAdd.perkCost, currentPerkPoints);
        }

        private async Task SubtractPerkPointsFromUser(ulong userId, int perkPointsToSubtract, int currentPerkPoints)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "update farmers set perkpoints = ?perkpoints where DiscordID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?perkpoints", currentPerkPoints - perkPointsToSubtract);
                command.Parameters.AddWithValue("?userId", userId);
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
        }
    }
}