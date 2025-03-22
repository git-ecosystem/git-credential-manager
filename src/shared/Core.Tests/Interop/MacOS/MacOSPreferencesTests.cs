using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using GitCredentialManager.Interop.MacOS;
using static GitCredentialManager.Tests.TestUtils;

namespace GitCredentialManager.Tests.Interop.MacOS;

public class MacOSPreferencesTests
{
    private const string TestAppId = "com.example.gcm-test";
    private const string DefaultsPath = "/usr/bin/defaults";

    [MacOSFact]
    public async Task MacOSPreferences_ReadPreferences()
    {
        try
        {
            await SetupTestPreferencesAsync();

            var pref = new MacOSPreferences(TestAppId);

            // Exists
            string stringValue = pref.GetString("myString");
            int? intValue = pref.GetInteger("myInt");
            IDictionary<string, string> dictValue = pref.GetDictionary("myDict");

            Assert.NotNull(stringValue);
            Assert.Equal("this is a string", stringValue);
            Assert.NotNull(intValue);
            Assert.Equal(42, intValue);
            Assert.NotNull(dictValue);
            Assert.Equal(2, dictValue.Count);
            Assert.Equal("value1", dictValue["dict-k1"]);
            Assert.Equal("value2", dictValue["dict-k2"]);

            // Does not exist
            string missingString = pref.GetString("missingString");
            int? missingInt = pref.GetInteger("missingInt");
            IDictionary<string, string> missingDict = pref.GetDictionary("missingDict");

            Assert.Null(missingString);
            Assert.Null(missingInt);
            Assert.Null(missingDict);
        }
        finally
        {
            await CleanupTestPreferencesAsync();
        }
    }

    private static async Task SetupTestPreferencesAsync()
    {
        // Using the defaults command set up preferences for the test app
        await RunCommandAsync(DefaultsPath, $"write {TestAppId} myString \"this is a string\"");
        await RunCommandAsync(DefaultsPath, $"write {TestAppId} myInt -int 42");
        await RunCommandAsync(DefaultsPath, $"write {TestAppId} myDict -dict dict-k1 value1 dict-k2 value2");
    }

    private static async Task CleanupTestPreferencesAsync()
    {
        // Delete the test app preferences
        // defaults delete com.example.gcm-test
        await RunCommandAsync(DefaultsPath, $"delete {TestAppId}");
    }
}
