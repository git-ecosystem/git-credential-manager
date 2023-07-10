using System.Collections.Generic;
using Newtonsoft.Json;

namespace Atlassian.Bitbucket.DataCenter
{
    public class LoginOptions
    {
        [JsonProperty("results")]
        public List<LoginOption> Results { get; set; }
    }
}