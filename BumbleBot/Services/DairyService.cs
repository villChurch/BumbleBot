﻿using System;
using System.Collections.Generic;
using System.Linq;
using BumbleBot.Models;
using BumbleBot.Utilities;
using Dapper;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services
{
    public class DairyService
    {
        private readonly DbUtils dBUtils = new DbUtils();

        public void IncreaseCaveSlots(ulong userId, int slots)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "update dairycave set slots = ?slots where ownerID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?slots", MySqlDbType.Int32).Value = slots;
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        public void DeductAllHardCheeseFromDairy(ulong userId)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "update dairy set hardcheese = 0 where ownerID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                command.ExecuteNonQuery();
            }

            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "delete from aging where DiscordID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        public bool DoesDairyHaveACave(ulong userId)
        {
            var hasCave = false;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
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
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
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
            List<Dairy> dairyList;
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
            {
                connection.Open();
                dairyList = connection
                    .Query<Dairy>("select * from dairy where ownerId = ?ownerId", new { ownerId = userId }).ToList();
            }

            return dairyList.Count == 1;
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
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "select * from dairycave where ownerID = ?userId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                    {
                        cave.SoftCheese = reader.GetDecimal("softCheese");
                        cave.Slots = reader.GetInt32("slots");
                    }

                reader.Close();
            }

            return cave;
        }

        public Dairy GetUsersDairy(ulong userId)
        {
            using var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString());
            connection.Open();
            var dairy = connection.QueryFirst<Dairy>("select * from dairy where ownerID =@ownerID", new {ownerID = userId});
            return dairy ?? new Dairy();
        }

        public void IncreaseCapcityOfDairy(ulong userId, int currentCapacity, int increaseBy)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
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
            {
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    var query = "update dairy set softcheese = 0 where ownerID = ?userId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.Add("?userId", MySqlDbType.VarChar).Value = userId;
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                Console.Out.WriteLine($"Soft cheese amount was not null it was {softCheese}");
                var currentSoftCheese = GetUsersDairy(userId).softcheese;
                var newSoftCheese = currentSoftCheese - softCheese;
                using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
                {
                    const string query = "update dairy set softcheese = ?softCheese where ownerID = ?userId";
                    var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("?softCheese", newSoftCheese);
                    command.Parameters.AddWithValue("?userId", userId);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteSoftCheeseFromExpiryTable(ulong userId)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
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