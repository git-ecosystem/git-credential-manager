using System.Text.Json.Serialization;

namespace Atlassian.Bitbucket;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(BitbucketTokenEndpointResponseJson))]
[JsonSerializable(typeof(Cloud.UserInfo), TypeInfoPropertyName = "Cloud_UserInfo")]
[JsonSerializable(typeof(DataCenter.UserInfo), TypeInfoPropertyName = "DataCenter_UserInfo")]
[JsonSerializable(typeof(DataCenter.LoginOptions))]
internal partial class BitbucketJsonSerializerContext : JsonSerializerContext
{
}
