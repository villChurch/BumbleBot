using System;
using System.Threading.Tasks;
using BumbleBot.Utilities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MySql.Data.MySqlClient;

namespace BumbleBot.Attributes
{
    public class HasEnoughCredits : CheckBaseAttribute
    {
        private readonly DBUtils dBUtils = new DBUtils();

        public HasEnoughCredits(int argumentPosition)
        {
            this.argumentPosition = argumentPosition;
        }

        private int balance { get; set; }
        public int argumentPosition { get; set; }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionStringAsync()))
            {
                var query = "select * from farmers where DiscordID = ?discordId";
                var command = new MySqlCommand(query, connection);
                command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
                connection.Open();
                var reader = command.ExecuteReader();
                if (reader.HasRows)
                    while (reader.Read())
                        balance = reader.GetInt32("credits");
                else
                    balance = 0;
                reader.Close();
            }

            var arguments = ctx.RawArgumentString.Split(' ');
            if (ctx.Command.Name.ToLower().Equals("help")) return Task.FromResult(1 == 1);
            var buyPrice = Convert.ToInt32(arguments[argumentPosition]);
            return Task.FromResult(balance >= buyPrice);
        }
    }
}