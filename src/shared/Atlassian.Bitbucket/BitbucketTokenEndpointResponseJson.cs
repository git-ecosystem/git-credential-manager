using System;
using System.Text.Json;
using GitCredentialManager.Authentication.OAuth.Json;
using System.Text.Json.Serialization;

namespace Atlassian.Bitbucket
{
    [JsonConverter(typeof(BitbucketCustomTokenEndpointResponseJsonConverter))]
    public class BitbucketTokenEndpointResponseJson : TokenEndpointResponseJson
    {
        // To ensure the "scopes" property used by Bitbucket is deserialized successfully with System.Text.Json, we must
        // use a custom converter. Otherwise, ordering will matter (i.e. if "scopes" is the final property, its value
        // will be used, but if "scope" is the final property, its value will be used).
    }

    public class BitbucketCustomTokenEndpointResponseJsonConverter : JsonConverter<BitbucketTokenEndpointResponseJson>
    {
        public override BitbucketTokenEndpointResponseJson Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            var response = new BitbucketTokenEndpointResponseJson();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return response;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();
                    if (propertyName != null)
                    {
                        switch (propertyName)
                        {
                            case "access_token":
                                response.AccessToken = reader.GetString();
                                break;
                            case "token_type":
                                response.TokenType = reader.GetString();
                                break;
                            case "expires_in":
                                if (reader.TryGetUInt32(out var expiration))
                                    response.ExpiresIn = expiration;
                                else
                                    response.ExpiresIn = null;
                                break;
                            case "refresh_token":
                                response.RefreshToken = reader.GetString();
                                break;
                            case "scopes":
                                response.Scope = reader.GetString();
                                break;
                        }
                    }
                }
            }

            throw new JsonException();
        }

        public override void Write(
            Utf8JsonWriter writer, BitbucketTokenEndpointResponseJson tokenEndpointResponseJson, JsonSerializerOptions options)
        { }
    }
}
