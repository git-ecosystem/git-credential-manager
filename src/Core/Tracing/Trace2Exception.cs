using System;
using System.ComponentModel;
using System.IO;
using GitCredentialManager.Authentication.OAuth;
using GitCredentialManager.Interop;

namespace GitCredentialManager;

public class Trace2Exception : Exception
{
    public Trace2Exception(string message) : base(message)
    {
        Trace2.WriteError(message);
    }

    public Trace2Exception(string message, string messageFormat) : base(message)
    {
        Trace2.WriteError(message, messageFormat);
    }
}

public class Trace2InvalidOperationException : InvalidOperationException
{
    public Trace2InvalidOperationException(string message) : base(message)
    {
        Trace2.WriteError(message);
    }
}

public class Trace2OAuth2Exception : OAuth2Exception
{
    public Trace2OAuth2Exception(string message) : base(message)
    {
        Trace2.WriteError(message);
    }

    public Trace2OAuth2Exception(string message, string messageFormat) : base(message)
    {
        Trace2.WriteError(message, messageFormat);
    }
}

public class Trace2InteropException : InteropException
{
    public Trace2InteropException(string message, int errorCode) : base(message, errorCode)
    {
        Trace2.WriteError($"message: {message} error code: {errorCode}");
    }

    public Trace2InteropException(string message, Win32Exception ex) : base(message, ex)
    {
        Trace2.WriteError(message);
    }
}

public class Trace2GitException : GitException
{
    public Trace2GitException(string message, int errorCode, string gitMessage) :
        base(message, gitMessage, errorCode)
    {
        var format = $"message: '{message}' error code: '{errorCode}' git message: '{{0}}'";
        var traceMessage = string.Format(format, gitMessage);

        Trace2.WriteError(traceMessage, format);
    }
}

public class Trace2FileNotFoundException : FileNotFoundException
{
    public Trace2FileNotFoundException(string message, string messageFormat, string fileName) :
        base(message, fileName)
    {
        Trace2.WriteError(message, messageFormat);
    }
}
