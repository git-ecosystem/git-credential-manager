using System.Text.Json.Serialization;

namespace GitCredentialManager;

/// <summary>
/// Classifications of processes invoked by GCM.
/// </summary>
public enum Trace2ProcessClass
{
    [JsonStringEnumMemberName("none")]
    None,
    [JsonStringEnumMemberName("ui_helper")]
    UiHelper,
    [JsonStringEnumMemberName("git")]
    Git,
    [JsonStringEnumMemberName("other")]
    Other
}
