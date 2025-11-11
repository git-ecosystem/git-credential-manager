namespace GitHub;

using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(GitHubUserInfo))]
[JsonSerializable(typeof(GitHubMetaInfo))]
internal partial class GitHubJsonSerializerContext : JsonSerializerContext
{
}
