using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication.OAuth.Json;

[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(DeviceAuthorizationEndpointResponseJson))]
[JsonSerializable(typeof(ErrorResponseJson))]
[JsonSerializable(typeof(TokenEndpointResponseJson))]
public partial class OAuthJsonSerializerContext : JsonSerializerContext
{
}
