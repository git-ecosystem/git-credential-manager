using System.Text.Json.Serialization;

namespace Atlassian.Bitbucket.DataCenter
{
    public class LoginOption
    {
        [JsonPropertyName("type")]
        public string Type { get ; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }
    }
}
