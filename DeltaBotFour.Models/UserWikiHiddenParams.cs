using Newtonsoft.Json;
using System.Collections.Generic;

namespace DeltaBotFour.Models
{
    public class UserWikiHiddenParams
    {
        [JsonProperty("comment")]
        public string Comment { get; set; }
        [JsonProperty("deltas")]
        public List<UserWikiDeltaInfo> DeltasReceived { get; set; }
        [JsonProperty("deltasGiven")]
        public List<UserWikiDeltaInfo> DeltasGiven { get; set; }
    }
}
