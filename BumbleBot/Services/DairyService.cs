﻿using System;
using BumbleBot.Models;
using BumbleBot.Utilities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services
{
    public class DairyService
    {
        private readonly DBUtils dBUtils = new DBUtils();

        public bool DoesDairyHaveACave(ulong userId)
        {
            var hasCave = false;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select * from dairycave where ownerId = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                hasCave = reader.HasRows;
                reader.Close();
            }

            return hasCave;
        }

        public void CreateCaveInDairy(ulong userId)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "insert into dairycave (ownerID) values (?userId)";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public bool HasDairy(ulong userId)
        {
            var hasDairy = false;
            using (var conneciton = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from dairy where ownerId = ?userId";
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
            var dairy = GetUsersDairy(userId);
            if (dairy.slots < 1) return false;
            var milkCapacity = dairy.slots * 1000;
            return dairy.milk + milkAmount <= milkCapacity;
        }

        public Cave GetUsersCave(ulong userId)
        {
            var cave = new Cave();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "select * from dairycave where ownerID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        cave.softCheese = reader.GetDecimal("softCheese");
                        cave.slots = reader.GetInt32("slots");
                    }

                reader.Close();
            }

            return cave;
        }

        public Dairy GetUsersDairy(ulong userId)
        {
            var dairy = new Dairy();
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from dairy where ownerID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        dairy.milk = reader.GetDecimal("milk");
                        dairy.slots = reader.GetInt32("slots");
                        dairy.softCheese = reader.GetDecimal("softcheese");
                        dairy.hardCheese = reader.GetDecimal("hardcheese");
                    }

                reader.Close();
            }

            return dairy;
        }

        public void IncreaseCapcityOfDairy(ulong userId, int currentCapacity, int increaseBy)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "update dairy set slots = ?slots where ownerID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?slots", MySqlDbType.Int32).Value = currentCapacity + increaseBy;
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void RemoveSoftCheeseFromPlayer(ulong userId, int? softCheese)
        {
            if (null == softCheese)
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
                {
                    var query = "update dairy set softcheese = 0 where ownerID = ?userId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            else
                Console.Out.WriteLine($"Soft cheese amount was not null it was {softCheese}");
        }

        public void DeleteSoftCheeseFromExpiryTable(ulong userId)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "delete from softcheeseexpiry where DiscordID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}