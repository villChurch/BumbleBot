using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BumbleBot.Models;
using BumbleBot.Utilities;
using Chronic;
using DisCatSharp.ApplicationCommands.EventArgs;
using DisCatSharp.CommandsNext;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace BumbleBot.Services;

public class AuditService
{
    private DbUtils dbUtils = new();
    public void HandleCommandExecution(CommandExecutionEventArgs ctx)
    {
        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            const string query =
                "insert into audit (discordId, command, arguments) values (?discordId, ?command, ?arguments)";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("?discordId", ctx.Context.User.Id);
            command.Parameters.AddWithValue("?command", ctx.Command.QualifiedName);
            command.Parameters.AddWithValue("?arguments", ctx.Context.RawArgumentString);
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }
    }

    public void HandleCommandExecution(SlashCommandExecutedEventArgs ctx)
    {
        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            const string query =
                "insert into audit (discordId, command, arguments) values (?discordId, ?command, ?arguments)";
            var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("?discordId", ctx.Context.User.Id);
            command.Parameters.AddWithValue("?command", ctx.Context.CommandName);
            StringBuilder argumentStringBuilder = new StringBuilder();
            if (ctx.Context.Interaction.Data.Options != null)
            {

                ctx.Context.Interaction.Data.Options.ForEach(option =>
                    argumentStringBuilder.AppendJoin(' ', $"{option.Name} {option.Value}"));
            }
            command.Parameters.AddWithValue("?arguments", argumentStringBuilder.ToString());
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }
    }

    public async Task<List<AuditCommandEvent>> GetLastTwentyCommandsRun()
    {
        var commandList = new List<AuditCommandEvent>();
        using (var connection = new MySqlConnection(dbUtils.ReturnPopulatedConnectionString()))
        {
            const string query = "select discordId, command, arguments from audit order by timestamp desc limit 20";
            var command = new MySqlCommand(query, connection);
            await connection.OpenAsync();
            var reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    commandList.Add(new AuditCommandEvent
                    {
                        commandName = reader.GetString("command"),
                        arguments = reader.GetString("arguments"),
                        discordId = reader.GetString("discordId")
                    });
                }
            }
            reader.Close();
            await connection.CloseAsync();
        }
        return commandList;
    }
}