namespace BumbleBot.Models;

public class AuditCommandEvent
{
    public string commandName { get; set; }
    public string discordId { get; set; }
    public string arguments { get; set; }
}