/* Neslouèené zmìny z projektu 'Core (net472)'
Pøed:
namespace GitCredentialManager.Authentication.OAuth.Json;
Po:
using GitCredentialManager;
using GitCredentialManager;
using GitCredentialManager.Authentication;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Authentication.OAuth.Json;
*/


/* Neslouèené zmìny z projektu 'Core (net472)'
Pøed:
namespace GitCredentialManager.Authentication.OAuth.Json;
Po:
using GitCredentialManager;
using GitCredentialManager;
using GitCredentialManager.Authentication;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Authentication.OAuth.Json;
*/
using GitCredentialManager.Authentication.OAuth.Json;

namespace GitCredentialManager
{
}

namespace GitCredentialManager.UI
{
    public interface ITimeSpanSecondsConverter3
    {
        TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, global::System.Boolean hasExistingValue, JsonSerializer serializer);
        void WriteJson(
            JsonWriter writer,
            TimeSpan value,
            JsonSerializer serializer);
    }

    public class TimeSpanSecondsConverter : JsonConverter<TimeSpan>, ITimeSpanSecondsConverter, ITimeSpanSecondsConverter1, ITimeSpanSecondsConverter2, ITimeSpanSecondsConverter3, ITimeSpanSecondsConverter
    {
        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
        {
            if (value.HasValue)
            {
                writer.WriteValue(value.Value.TotalSeconds);
            }
        }

        public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string valueString = reader.Value?.ToString();
            if (valueString != null && int.TryParse(valueString, out int valueInt))
            {
                return TimeSpan.FromSeconds(valueInt);
            }

            return null;
        }
    }
}