using Avalonia.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitCredentialManager.Tests.Objects
{
    public class TestCredentialStore : ICredentialStore
    {
        private readonly IDictionary<(string service, string account), TestCredential> _store;

        public TestCredentialStore()
        {
            _store = new Dictionary<(string,string), TestCredential>();
        }

        #region ICredentialStore

        public IList<string> GetAccounts(string service)
        {
            return Query(service, null).Select(x => x.Account).Distinct().ToList();
        }

        ICredential ICredentialStore.Get(string service, string account)
        {
            return TryGet(service, account, out TestCredential credential) ? credential : null;
        }

        void ICredentialStore.AddOrUpdate(string service, string account, string secret)
        {
            Add(service, account, secret);
        }

        bool ICredentialStore.Remove(string service, string account)
        {
            foreach (var key in _store.Keys)
            {
                if ((service == null || key.service == service) &&
                    (account == null || key.account == account))
                {
                    _store.Remove(key);
                    return true;
                }
            }

            return false;
        }

        void ICredentialStore.AddOrUpdate(string service, ICredential credential) => Add(service, new TestCredential(service, credential));
        bool ICredentialStore.Remove(string service, ICredential credential) => (this as ICredentialStore).Remove(service, credential.Account);

        #endregion

        public int Count => _store.Count;

        public bool CanStorePasswordExpiry => true;

        public bool CanStoreOAuthRefreshToken => true;

        public bool TryGet(string service, string account, out TestCredential credential)
        {
            credential = Query(service, account).FirstOrDefault();
            return credential != null;
        }

        public void Add(string service, TestCredential credential)
        {
            _store[(service, credential.Account)] = credential;
        }

        public TestCredential Add(string service, string account, string secret)
        {
            var credential = new TestCredential(service, account, secret);
            _store[(service, account)] = credential;
            return credential;
        }

        public bool Contains(string service, string account)
        {
            return TryGet(service, account, out _);
        }

        private IEnumerable<TestCredential> Query(string service, string account)
        {
            if (string.IsNullOrWhiteSpace(account))
            {
                // Find the all credentials matching service
                foreach (var kvp in _store)
                {
                    if (kvp.Key.service == service)
                    {
                        yield return kvp.Value;
                    }
                }
            }

            // Find the specific credential matching both service and credential
            if (_store.TryGetValue((service, account), out var credential))
            {
                yield return credential;
            }
        }
    }

    public class TestCredential : ICredential
    {
        public TestCredential(string service, string account, string password)
        {
            Service = service;
            Account = account;
            Password = password;
        }

        public TestCredential(string service, ICredential credential)
        {
            Service = service;
            Account = credential.Account;
            Password = credential.Password;
            OAuthRefreshToken = credential.OAuthRefreshToken;
            PasswordExpiry = credential.PasswordExpiry;
        }

        public string Service { get; }

        public string Account { get; }

        public string Password { get; }

        public DateTimeOffset? PasswordExpiry { get; set; }

        public string OAuthRefreshToken { get; set; }
    }
}
