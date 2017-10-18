using Newtonsoft.Json;

namespace DeltaBotFour.Models
{
    public class UserWikiDeltaInfo
    {
        [JsonProperty("b")]
        public string PostLink { get; set; }
        [JsonProperty("dc")]
        public string ThingShortId { get; set; }
        [JsonProperty("t")]
        public string PostTitle { get; set; }
        [JsonProperty("ab")]
        public string Username { get; set; }
        [JsonProperty("uu")]
        public string CreatedUTC { get; set; }
    }
}
