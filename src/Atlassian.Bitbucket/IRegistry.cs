using System;
using GitCredentialManager;

namespace Atlassian.Bitbucket
{
    public interface IRegistry<T> : IDisposable
    {
        T Get(InputArguments input);
    }
}