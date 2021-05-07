using System;
using MySqlConnector;

namespace BumbleBot.Models
{
    public class Reminders
    {
        public int id { get; set; }
        public ulong userId { get; }
        public string message { get; }
        public string dml { get; set; }
        public DateTime DateTime { get; set; }
        
        public ulong guild { get; }

        public Reminders(MySqlDataReader reader)
        {
            id = reader.GetInt32("id");
            userId = reader.GetUInt64("userId");
            dml = reader.GetString("discordMessageLink");
            DateTime = reader.GetDateTime("time");
            message = reader.GetString("message");
            guild = reader.GetUInt64("guild");
        }
    }
}