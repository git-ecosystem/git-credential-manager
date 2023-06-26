using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Atlassian.Bitbucket.DataCenter
{
    public class LoginOptions
    {
        [JsonPropertyName("results")]
        public List<LoginOption> Results { get; set; }
    }
}
