using System.Text.Json.Serialization;

namespace GitCredentialManager.Authentication.OAuth.Json;

[JsonSerializable(typeof(DeviceAuthorizationEndpointResponseJson))]
[JsonSerializable(typeof(TokenEndpointResponseJson))]
[JsonSerializable(typeof(ErrorResponseJson))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
public partial class OAuthJsonContext : JsonSerializerContext;
