using System.Collections.Generic;
using System.Linq;
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
                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                await connection.CloseAsync().ConfigureAwait(false);
            }
            await SubtractPerkPointsFromUser(userId, perkToAdd.perkCost, currentPerkPoints).ConfigureAwait(false);
        }

        private async Task SubtractPerkPointsFromUser(ulong userId, int perkPointsToSubtract, int currentPerkPoints)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "update farmers set perkpoints = ?perkpoints where DiscordID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?perkpoints", currentPerkPoints - perkPointsToSubtract);
                command.Parameters.AddWithValue("?userId", userId);
                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }

        public async Task RemovePerksFromUser(ulong userId, List<Perks> usersPerks, int currentPerkPoints)
        {
            int perkPointsToAdd = usersPerks.Sum(perk => perk.perkCost);
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "delete from farmerperks where farmerid = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?userId", userId);
                await connection.OpenAsync().ConfigureAwait(false);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                await connection.CloseAsync().ConfigureAwait(false);
            }
            await AddPerkPointsToUser(userId, perkPointsToAdd, currentPerkPoints).ConfigureAwait(false);
        }

        private async Task AddPerkPointsToUser(ulong userId, int perkPointsToAdd, int currentPerkPoints)
        {
            await SubtractPerkPointsFromUser(userId, perkPointsToAdd * -1, currentPerkPoints).ConfigureAwait(false);
        }

        public async Task<List<Perks>> GetUsersPerks(ulong userId)
        {
            List<Perks> userPerks = new List<Perks>();
            var allPerks = await GetAllPerks().ConfigureAwait(false);
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select * from farmerperks where farmerid = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?userId", userId);
                await connection.OpenAsync().ConfigureAwait(false);
                var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        int perkId = reader.GetInt32("perkid");
                        userPerks.Add(allPerks.First(perk => perk.id == perkId));
                    }
                }
                await connection.CloseAsync().ConfigureAwait(false);
            }
            return userPerks;
        }
        
        public async Task<List<Perks>> GetAllPerks()
        {
            var allPerks = new List<Perks>();
            using (var con = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select * from perks order by levelUnlocked, perkName ASC";
                var command = new MySqlCommand(query, con);
                await con.OpenAsync().ConfigureAwait(false);
                var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
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
                            levelUnlocked = reader.GetInt16("levelUnlocked"),
                            requires = reader.GetInt32("requires")
                        };
                        allPerks.Add(perk);
                    }
                }
                await con.CloseAsync().ConfigureAwait(false);
            }
            return allPerks;
        }
    }
}