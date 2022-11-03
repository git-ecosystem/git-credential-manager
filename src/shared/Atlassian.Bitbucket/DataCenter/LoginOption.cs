using Newtonsoft.Json;

namespace Atlassian.Bitbucket.DataCenter
{
    public class LoginOption
    {
        [JsonProperty("type")]
        public string Type { get ; set; }

        [JsonProperty("id")]
        public int Id { get; set; }
    }
}