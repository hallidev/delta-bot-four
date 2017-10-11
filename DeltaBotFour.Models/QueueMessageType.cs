using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DeltaBotFour.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum QueueMessageType
    {
        Comment
    }
}
