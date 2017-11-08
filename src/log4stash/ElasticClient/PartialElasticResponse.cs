using Newtonsoft.Json;

namespace log4stash
{
    internal sealed class PartialElasticResponse
    {
        [JsonProperty("errors")]
        public bool Errors { get; set; }
    }
}