using System;
using System.Threading.Tasks;
using DisCatSharp.Entities;
using MySql.Data.MySqlClient;

namespace BumbleBot.Utilities
{
    public class WelcomeUtilities
    {
        private DbUtils dbUtils = new();

        public async Task InsertOrUpdateWelcomeMessage(DiscordGuild guild, string channelId, string message)
        {
            if (HasWelcomeMessage(guild))
            {
                await DeleteWelcomeMessage(guild);
            }
            await using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "Insert into welcome (value, guildId, channelId) VALUES (?value, ?guildId, ?channelId)";
                await connection.OpenAsync();
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?guildId", guild.Id);
                command.Parameters.AddWithValue("?channelId", channelId);
                command.Parameters.AddWithValue("?value", message);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteWelcomeMessage(DiscordGuild guild)
        {
            await using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "delete from welcome where guildId = ?guildId";
                await connection.OpenAsync();
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?guildId", guild.Id);
                await command.ExecuteNonQueryAsync();
            }
        }

        public String ReturnCompletedWelcomeMessage(DiscordGuild discordGuild, DiscordUser discordUser)
        {
            var message = $"Welcome to **{discordGuild.Name}** {discordUser.Mention}";
            var dbMessage = "";
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "Select value from welcome where guildId = ?guildId";
                connection.Open();
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?guildId", discordGuild.Id);
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        dbMessage = reader.GetString("value");
                    }
                }
                reader.Close();
                connection.Close();
            }
            return message + Environment.NewLine + dbMessage;
        }

        public bool HasWelcomeMessage(DiscordGuild discordGuild)
        {
            var hasMessage = false;
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "select value from welcome where guildId = ?guildId";
                connection.Open();
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?guildId", discordGuild.Id);
                var reader = command.ExecuteReader();
                hasMessage = reader.HasRows;
                reader.Close();
                connection.Close();
            }
            return hasMessage;
        }

        public String ReturnChannelId(DiscordGuild discordGuild)
        {
            var channelId = "";
            using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
            {
                const string query = "select channelId from welcome where guildId = ?guildId";
                connection.Open();
                var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("?guildId", discordGuild.Id);
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    while(reader.Read())
                    {
                        channelId = reader.GetString("channelId");
                    }
                }
            }
            return channelId;
        }
    }
}

