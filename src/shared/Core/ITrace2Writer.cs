using System;

namespace GitCredentialManager;

public interface ITrace2Writer : IDisposable
{
    bool Failed { get; }

    void Write(Trace2Message message);
}
