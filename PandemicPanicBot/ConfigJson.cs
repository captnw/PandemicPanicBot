using Newtonsoft.Json;

namespace PandemicPanicBot
{
    public struct ConfigJson
    {
        // This struct contains the info from the .json file
        [JsonProperty("token")]
        public string Token { get; private set; }
        [JsonProperty("prefix")]
        public string Prefix { get; private set; }
    }
}
