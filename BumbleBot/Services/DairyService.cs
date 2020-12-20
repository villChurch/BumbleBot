using System;
using BumbleBot.Models;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services
{
    public class DairyService
    {
        private DBUtils dBUtils = new DBUtils();
        public DairyService()
        {
        }

        public bool HasDairy(ulong userId)
        {
            bool hasDairy = false;
            using(MySqlConnection conneciton = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select * from dairy where ownerId = ?userId";
                var command = new MySqlCommand(query, conneciton);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                conneciton.Open();
                var reader = command.ExecuteReader();
                hasDairy = reader.HasRows;
            }
            return hasDairy;
        }

        public bool CanMilkFitInDairy(ulong userId, int milkAmount)
        {
            Dairy dairy = GetUsersDairy(userId);
            if (dairy.slots < 1)
            {
                return false;
            }
            int milkCapacity = dairy.slots * 1000;
            return dairy.milk + milkAmount <= milkCapacity;
        }

        public Dairy GetUsersDairy(ulong userId)
        {
            Dairy dairy = new Dairy();
            using (MySqlConnection connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                string query = "select * from dairy where ownerID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        dairy.milk = reader.GetDecimal("milk");
                        dairy.slots = reader.GetInt32("slots");
                        dairy.softCheese = reader.GetDecimal("softcheese");
                        dairy.hardCheese = reader.GetDecimal("hardcheese");
                    }
                }
                reader.Close();
            }
            return dairy;
        }
    }
}
