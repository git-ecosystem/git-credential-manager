using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace GitCredentialManager;

/// <summary>
/// The different event types tracked in the TRACE2 tracing
/// system.
/// </summary>
public enum Trace2Event
{ }

/// <summary>
/// Represents the application's TRACE2 tracing system.
/// </summary>
public interface ITrace2 : IDisposable
{ }

public class Trace2 : DisposableObject, ITrace2
{
    private readonly object _writersLock = new object();
    private List<ITrace2Writer> _writers = new List<ITrace2Writer>();

    protected override void ReleaseManagedResources()
    {
        lock (_writersLock)
        {
            try
            {
                for (int i = 0; i < _writers.Count; i += 1)
                {
                    using (var writer = _writers[i])
                    {
                        _writers.Remove(writer);
                    }
                }
            }
            catch
            {
                /* squelch */
            }
        }

        base.ReleaseManagedResources();
    }

    private void AddWriter(ITrace2Writer writer)
    {
        ThrowIfDisposed();

        lock (_writersLock)
        {
            // Try not to add the same writer more than once
            if (_writers.Contains(writer))
                return;

            _writers.Add(writer);
        }
    }

    private void WriteMessage(Trace2Message message)
    {
        ThrowIfDisposed();

        lock (_writersLock)
        {
            if (_writers.Count == 0)
            {
                return;
            }

            foreach (var writer in _writers)
            {
                if (!writer.Failed)
                {
                    writer.Write(message);
                }
            }
        }
    }
}

public abstract class Trace2Message
{
    private const int SourceColumnMaxWidth = 23;
    protected const string TimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffff'Z'";

    [JsonProperty("event", Order = 1)]
    public Trace2Event Event { get; set; }

    [JsonProperty("sid", Order = 2)]
    public string Sid { get; set; }

    [JsonProperty("thread", Order = 3)]
    public string Thread { get; set; }

    [JsonProperty("time", Order = 4)]
    public DateTimeOffset Time { get; set; }

    [JsonProperty("file", Order = 5)]
    public string File { get; set; }

    [JsonProperty("line", Order = 6)]
    public int Line { get; set; }

    public abstract string ToJson();
}
