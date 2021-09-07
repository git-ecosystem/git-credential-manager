using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Git.CredentialManager
{
    public class GitVersion : IComparable, IComparable<GitVersion>
    {
        private List<int> components;

        public GitVersion(string versionString)
        {
            if (versionString is null)
            {
                components = new List<int>();
                return;
            }

            string[] splitVersion = versionString.Split('.');
            components = new List<int>(splitVersion.Length);

            foreach (string part in splitVersion)
            {
                if (Int32.TryParse(part, out int component))
                {
                    components.Add(component);
                }
                else
                {
                    // Exit at the first non-integer component
                    break;
                }
            }
        }

        public GitVersion(params int[] components)
        {
            this.components = components.ToList();
        }

        public override string ToString()
        {
            return string.Join(".", this.components);
        }

        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                return 1;
            }

            GitVersion other = obj as GitVersion;
            if (other == null)
            {
                throw new ArgumentException("A GitVersion object is required for comparison.", "obj");
            }

            return CompareTo(other);
        }

        public int CompareTo(GitVersion other)
        {
            if (other is null)
            {
                return 1;
            }

            // Compare for as many components as the two versions have in common. If a
            // component does not exist in a components list, it is assumed to be 0.
            int thisCount = this.components.Count, otherCount = other.components.Count;
            for (int i = 0; i < Math.Max(thisCount, otherCount); i++)
            {
                int thisComponent = i < thisCount ? this.components[i] : 0;
                int otherComponent = i < otherCount ? other.components[i] : 0;
                if (thisComponent != otherComponent)
                {
                    return thisComponent.CompareTo(otherComponent);
                }
            }

            // No discrepencies found in versions
            return 0;
        }

        public static int Compare(GitVersion left, GitVersion right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            return left.CompareTo(right);
        }

        public override bool Equals(object obj)
        {
            GitVersion other = obj as GitVersion;
            if (other is null)
            {
                return false;
            }

            return this.CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public static bool operator ==(GitVersion left, GitVersion right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(GitVersion left, GitVersion right)
        {
            return !(left == right);
        }

        public static bool operator <(GitVersion left, GitVersion right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator >(GitVersion left, GitVersion right)
        {
            return Compare(left, right) > 0;
        }

        public static bool operator <=(GitVersion left, GitVersion right)
        {
            return Compare(left, right) <= 0;
        }

        public static bool operator >=(GitVersion left, GitVersion right)
        {
            return Compare(left, right) >= 0;
        }
    }
}