using System;
using System.ComponentModel;
using System.IO;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Interop;

namespace GitCredentialManager;

public class Trace2Exception : Exception
{
    public Trace2Exception(ITrace2 trace2, string message) : base(message)
    {
        trace2.WriteError(message);
    }

    public Trace2Exception(ITrace2 trace2, string message, string messageFormat) : base(message)
    {
        trace2.WriteError(message, messageFormat);
    }
}

public class Trace2InvalidOperationException : InvalidOperationException
{
    public Trace2InvalidOperationException(ITrace2 trace2, string message) : base(message)
    {
        trace2.WriteError(message);
    }
}

public class Trace2OAuth2Exception : OAuth2Exception
{
    public Trace2OAuth2Exception(ITrace2 trace2, string message) : base(message)
    {
        trace2.WriteError(message);
    }

    public Trace2OAuth2Exception(ITrace2 trace2, string message, string messageFormat) : base(message)
    {
        trace2.WriteError(message, messageFormat);
    }
}

public class Trace2InteropException : InteropException
{
    public Trace2InteropException(ITrace2 trace2, string message, int errorCode) : base(message, errorCode)
    {
        trace2.WriteError($"message: {message} error code: {errorCode}");
    }

    public Trace2InteropException(ITrace2 trace2, string message, Win32Exception ex) : base(message, ex)
    {
        trace2.WriteError(message);
    }
}

public class Trace2GitException : GitException
{
    public Trace2GitException(ITrace2 trace2, string message, int errorCode, string gitMessage) :
        base(message, gitMessage, errorCode)
    {
        var format = $"message: '{message}' error code: '{errorCode}' git message: '{{0}}'";
        var traceMessage = string.Format(format, gitMessage);

        trace2.WriteError(traceMessage, format);
    }
}

public class Trace2FileNotFoundException : FileNotFoundException
{
    public Trace2FileNotFoundException(ITrace2 trace2, string message, string messageFormat, string fileName) :
        base(message, fileName)
    {
        trace2.WriteError(message, messageFormat);
    }
}
