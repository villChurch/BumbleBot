using Newtonsoft.Json;

namespace BumbleBot
{
    public struct ConfigJson
    {
        [JsonProperty("token")] public string Token { get; private set; }
        [JsonProperty("prefix")] public string Prefix { get; private set; }
        [JsonProperty("databaseName")] public string DatabaseName { get; private set; }
        [JsonProperty("databaseServer")] public string DatabaseServer { get; private set; }
        [JsonProperty("databasePassword")] public string DatabasePassword { get; private set; }
        [JsonProperty("databaseUser")] public string DatabaseUser { get; private set; }
        [JsonProperty("databasePort")] public uint DatabasePort { get; private set; }
    }
}