using System;
using System.Linq;
using System.Threading.Tasks;
using BumbleBot.Utilities;
using DisCatSharp.ApplicationCommands;
using MySql.Data.MySqlClient;

namespace BumbleBot.Attributes;

public class HasEnoughCreditsSlash : SlashCheckBaseAttribute
{
    private readonly DbUtils dBUtils = new();

    private int Balance { get; set; }

    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
    {
        using (var connection = new MySqlConnection(dBUtils.ReturnPopulatedConnectionString()))
        {
            var query = "select * from farmers where DiscordID = ?discordId";
            var command = new MySqlCommand(query, connection);
            command.Parameters.Add("?discordId", MySqlDbType.VarChar).Value = ctx.User.Id;
            connection.Open();
            var reader = command.ExecuteReader();
            if (reader.HasRows)
                while (reader.Read())
                    Balance = reader.GetInt32("credits");
            else
                Balance = 0;
            reader.Close();
        }
        
        var buyPrice = Convert.ToInt32(ctx.Interaction.Data.Options.Take(1).First(opt => opt.Name.Equals("cost", StringComparison.OrdinalIgnoreCase)).Value);
        return Task.FromResult(Balance >= buyPrice);
    }
}