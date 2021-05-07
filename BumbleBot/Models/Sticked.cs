using MySql.Data.MySqlClient;

namespace BumbleBot.Models
{
    public class Sticked
    {
        private int id { get; }
        public ulong recipientId { get; }
        public ulong stickerId { get; }
        public string messageLink { get; }

        public Sticked(MySqlDataReader reader)
        {
            id = reader.GetInt32("id");
            recipientId = reader.GetUInt64("recipientId");
            stickerId = reader.GetUInt64("stickerId");
            messageLink = reader.GetString("messageLink");
        }
    }
}