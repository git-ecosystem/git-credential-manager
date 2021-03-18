// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.IO;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Value store that uses an INI file as the persistent storage.
    /// </summary>
    public class IniFileValueStore : ITransactionalValueStore<string, string>
    {
        private readonly IFileSystem _fileSystem;
        private readonly IniSerializer _serializer;
        private readonly string _filePath;
        private readonly string _parentPath;
        private readonly object _fileLock = new object();
        private IniFile _iniFile;

        public IniFileValueStore(IFileSystem fileSystem, IniSerializer serializer, string filePath)
        {
            _fileSystem = fileSystem;
            _serializer = serializer;
            _filePath = filePath;
            _parentPath = Path.GetDirectoryName(_filePath);

            Reload();
        }

        public void Reload()
        {
            lock (_fileLock)
            {
                if (_fileSystem.FileExists(_filePath))
                {
                    string text = _fileSystem.ReadAllText(_filePath);
                    using (var reader = new StringReader(text))
                    {
                        _iniFile = _serializer.Deserialize(reader);
                    }
                }
                else
                {
                    // Create a new empty INI file object
                    _iniFile = new IniFile();
                }
            }
        }

        public void Commit()
        {
            lock (_fileLock)
            {
                // Sure parent directory exists
                if (!_fileSystem.DirectoryExists(_parentPath))
                {
                    _fileSystem.CreateDirectory(_parentPath);
                }

                using (var writer = new StringWriter())
                {
                    _serializer.Serialize(_iniFile, writer);
                    _fileSystem.WriteAllText(_filePath, writer.ToString());
                }
            }
        }

        public bool TryGetValue(string key, out string value)
        {
            lock (_fileLock)
            {
                value = null;

                if (!TrySplitKey(key, out string section, out string scope, out string property))
                {
                    throw new ArgumentException($"Invalid key '{key}'.", nameof(key));
                }

                return _iniFile.TryGetValue(section, scope, property, out value);
            }
        }

        public void SetValue(string key, string value)
        {
            lock (_fileLock)
            {
                if (!TrySplitKey(key, out string section, out string scope, out string property))
                {
                    throw new ArgumentException($"Invalid key '{key}'.", nameof(key));
                }

                _iniFile.SetValue(section, scope, property, value);
            }
        }

        public void Remove(string key)
        {
            lock (_fileLock)
            {
                if (!TrySplitKey(key, out string section, out string scope, out string property))
                {
                    throw new ArgumentException($"Invalid key '{key}'.", nameof(key));
                }

                _iniFile.UnsetValue(section, scope, property);
            }
        }

        private static bool TrySplitKey(string key, out string section, out string scope, out string property)
        {
            section = null;
            scope = null;
            property = null;

            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            int first = key.IndexOf('.');
            int last = key.LastIndexOf('.');

            if (first < 0 || last < 0)
            {
                return false;
            }

            // section.property
            if (first == last)
            {
                section = key.Substring(0, first);
                property = key.Substring(last + 1);

                return true;
            }

            // section.scope.maybe.with.periods.property
            section = key.Substring(0, first);
            scope = key.Substring(first + 1, last - first - 1);
            property = key.Substring(last + 1);

            return true;
        }
    }
}
