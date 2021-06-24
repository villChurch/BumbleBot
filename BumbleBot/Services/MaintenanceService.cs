using BumbleBot.Models;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services
{
    public class MaintenanceService
    {
        private readonly DbUtils dbUtils = new();

        public Maintenance GetFarmersMaintenance(ulong farmerId)
        {
            var maintenance = new Maintenance();
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select * from maintenance where farmerid = ?farmerid";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?farmerid", farmerId);
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        maintenance.id = reader.GetInt32("id");
                        maintenance.farmerId = farmerId;
                        maintenance.needsMaintenance = reader.GetBoolean("needsMaintenance");
                        maintenance.milkingBoost = reader.GetBoolean("milkingBoost");
                        maintenance.dailyBoost = reader.GetBoolean("dailyBoost");
                    }
                }
            }

            return maintenance;
        }

        public int GetMaintenanceRepairCost(ulong farmerId)
        {
            var numberOfGoats = 0;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "SELECT COUNT(*) as numberOfGoats FROM goats WHERE ownerID = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = farmerId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        numberOfGoats = reader.GetInt32("numberOfGoats");
                reader.Close();
            }

            return numberOfGoats * 20;
        }

        public void SetMaintenanceAsCompleted(ulong farmerId)
        {
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "Update maintenance Set needsMaintenance = 0 where farmerid  = ?farmerId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?farmerId", farmerId);
                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
        }
    }
}