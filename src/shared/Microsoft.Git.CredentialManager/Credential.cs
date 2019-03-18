// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Globalization;
using System.Text;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents a simple credential; user name and password pair.
    /// </summary>
    public interface ICredential
    {
        /// <summary>
        /// User name.
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// Password.
        /// </summary>
        string Password { get; }
    }

    /// <summary>
    /// Represents a credential (username/password pair) that Git can use to authenticate to a remote repository.
    /// </summary>
    public class GitCredential : ICredential
    {
        public GitCredential(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public string UserName { get; }

        public string Password { get; }
    }

    public static class CredentialExtensions
    {
        /// <summary>
        /// Returns the base-64 encoded, {username}:{password} formatted string of this `<see cref="ICredential"/>`.
        /// </summary>
        public static string ToBase64String(this ICredential credential)
        {
            string basicAuthValue = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", credential.UserName, credential.Password);
            byte[] authBytes = Encoding.UTF8.GetBytes(basicAuthValue);
            return Convert.ToBase64String(authBytes);
        }
    }
}
