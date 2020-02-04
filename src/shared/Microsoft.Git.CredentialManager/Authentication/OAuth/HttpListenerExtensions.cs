// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Authentication.OAuth
{
    public static class HttpListenerExtensions
    {
        public static async Task WriteResponseAsync(this HttpListenerResponse response, string responseText)
        {
            byte[] responseData = Encoding.UTF8.GetBytes(responseText);
            response.ContentLength64 = responseData.Length;
            await response.OutputStream.WriteAsync(responseData, 0, responseData.Length);
        }
    }
}
