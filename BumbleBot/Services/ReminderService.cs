using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using BumbleBot.Models;
using BumbleBot.Utilities;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer.Localisation;
using System.Linq;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace BumbleBot.Services
{
    public class ReminderService
    {
        private DbUtils dbUtils = new DbUtils();
        private static Timer timer;
        private static DiscordClient discordClient;

        public static void StartReminderTimer(DiscordClient client)
        {
            discordClient = client;
            timer = new Timer();
            timer.Interval = 60000; // 60 seconds
            timer.Elapsed += CheckForAndSendReminders;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private static async void CheckForAndSendReminders(object source, ElapsedEventArgs e)
        {
            List<Reminders> remindersList = new List<Reminders>();
            using (var con = new MySqlConnection(DbUtils.ReturnPopulatedConnectionStringStatic()))
            {
                const string query = "select * from reminders where time <= ?time";
                var command = new MySqlCommand(query, con);
                var dateTimeOffset = DateTimeOffset.Now.LocalDateTime;
                command.Parameters.AddWithValue("?time", dateTimeOffset);
                con.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        remindersList.Add(new Reminders(reader));
                    }
                }
                reader.Close();
                await con.CloseAsync();
            }

            if (remindersList.Count > 0)
            {
                try
                {
                    remindersList.ForEach(async reminder =>
                    {
                        var guild = await discordClient.GetGuildAsync(reminder.guild);
                        if (guild != null)
                        {
                            DiscordMember member = await guild.GetMemberAsync(reminder.userId);
                            var dmChannel = await member.CreateDmChannelAsync();
                            var embed = new DiscordEmbedBuilder
                            {
                                Title = "Your Reminder",
                                Description = reminder.message,
                                Color = DiscordColor.Aquamarine
                            };
                            embed.AddField("Original Message", reminder.dml);
                            await dmChannel.SendMessageAsync(embed).ConfigureAwait(false);
                        }
                    });
                    using (var con = new MySqlConnection(DbUtils.ReturnPopulatedConnectionStringStatic()))
                    {
                        var idArray = remindersList.Select(x => x.id).ToArray();
                        var idList = remindersList.Count > 0 ? string.Join(", ", remindersList.Select(x => x.id)) : "";
                        string query = $"delete from reminders where id in ({idList})";
                        var command = new MySqlCommand(query, con);
                        await con.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                        await con.CloseAsync();
                    }
                }
                catch (NotFoundException nfe)
                {
                    discordClient.Logger.Log(LogLevel.Error,
                        "Guild could not be found. Exception message was {Message}",
                        nfe.Message);
                }
                catch (UnauthorizedException uae)
                {
                    discordClient.Logger.Log(LogLevel.Error,
                        "Could not access that guild. Exception message was {Message}",
                        uae.Message);
                }
            }
        }
        public async Task AddReminderToDataBase(DateTimeOffset dateTimeOffset, 
            DiscordMember member, string message, DiscordMessage discordMessage)
        {
            using (var con = new MySqlConnection(dbUtils.ReturnPopulatedConnectionStringAsync()))
            {
                const string query = "insert into reminders (userId, message, discordMessageLink, time, guild)"
                                     + " values(?userId, ?message, ?dml, ?time, ?guild)";
                var command = new MySqlCommand(query, con);
                command.Parameters.AddWithValue("?userId", member.Id);
                command.Parameters.AddWithValue("?message", message);
                command.Parameters.AddWithValue("?dml", discordMessage.JumpLink.ToString());
                command.Parameters.AddWithValue("?time", dateTimeOffset.LocalDateTime);
                command.Parameters.AddWithValue("?guild", discordMessage.Channel.Guild.Id);
                await con.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await con.CloseAsync();
            }
        }
    }
}