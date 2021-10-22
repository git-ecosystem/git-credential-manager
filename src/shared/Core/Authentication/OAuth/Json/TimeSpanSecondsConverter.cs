using System;
using Newtonsoft.Json;

namespace GitCredentialManager.Authentication.OAuth.Json
{
    public class TimeSpanSecondsConverter : JsonConverter<TimeSpan?>
    {
        public override void WriteJson(JsonWriter writer, TimeSpan? value, JsonSerializer serializer)
        {
            if (value.HasValue)
            {
                writer.WriteValue(value.Value.TotalSeconds);
            }
        }

        public override TimeSpan? ReadJson(JsonReader reader, Type objectType, TimeSpan? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string valueString = reader.Value?.ToString();
            if (valueString != null)
            {
                if (int.TryParse(valueString, out int valueInt))
                {
                    return TimeSpan.FromSeconds(valueInt);
                }
            }

            return null;
        }
    }
}
