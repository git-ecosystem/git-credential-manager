// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Git.CredentialManager;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Authentication.Helper
{
    /// <summary>
    /// Custom <see cref="TokenCache"/> which will find and use the latest
    /// installed version of Visual Studio's ADAL cache.
    /// </summary>
    /// <remarks>
    /// Sharing the VS ADAL cache will help reduce the number of sign-in prompts
    /// by enabling re-use of stored access tokens. This can be useful when Git
    /// is being used from both VS Team Explorer and the command line.
    /// </remarks>
    internal class VisualStudioTokenCache : TokenCache
    {
        private static readonly IReadOnlyList<string> KnownCachePaths = new[]
        {
            // VS2019 location and cache format has not been confirmed yet
            @".IdentityService\IdentityServiceAdalCache.cache",          // VS2017 ADAL v3 cache
            @"Microsoft\VSCommon\VSAccountManagement\AdalCache.cache",   // VS2015 ADAL v2 cache
            @"Microsoft\VSCommon\VSAccountManagement\AdalCacheV2.cache", // VS2017 ADAL v2 cache
        };

        private readonly ICommandContext _context;
        private readonly string _vsCachePath;
        private readonly object _lock = new object();

        public VisualStudioTokenCache(ICommandContext context)
        {
            _context = context;

            BeforeAccess = OnBeforeAccess;
            AfterAccess = OnAfterAccess;

            _vsCachePath = FindVisualStudioCachePath();
            if (_vsCachePath is null)
            {
                _context.Trace.WriteLine("No Visual Studio token cache was found.");
            }
            else
            {
                _context.Trace.WriteLine($"Using Visual Studio token cache at '{_vsCachePath}'.");
            }
        }

        private static string FindVisualStudioCachePath()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            foreach (string relativePath in KnownCachePaths)
            {
                string candidatePath = Path.Combine(localAppDataPath, relativePath);

                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            return null;
        }

        private void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            lock (_lock)
            {
                if (_vsCachePath != null && File.Exists(_vsCachePath))
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(_vsCachePath);

                        byte[] state = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);

                        Deserialize(state);
                    }
                    catch (Exception ex)
                    {
                        _context.Trace.WriteLine("Reading token cache failed!");
                        _context.Trace.WriteException(ex);
                    }
                }
            }
        }

        private void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            lock (_lock)
            {
                try
                {
                    if (HasStateChanged && File.Exists(_vsCachePath))
                    {
                        byte[] state = Serialize();

                        byte[] data = ProtectedData.Protect(state, null, DataProtectionScope.CurrentUser);

                        File.WriteAllBytes(_vsCachePath, data);

                        HasStateChanged = false;
                    }
                }
                catch (Exception ex)
                {
                    _context.Trace.WriteLine("Writing token cache failed!");
                    _context.Trace.WriteException(ex);
                }
            }
        }
    }
}
