#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace GitCredentialManager
{
    public enum GitDistributionType
    {
        /// <summary>
        /// Represents a base/core distribution of Git.
        /// </summary>
        Core,

        /// <summary>
        /// Represents the Git for Windows fork of Git. Inline version identifier "windows".
        /// </summary>
        GitForWindows,

        /// <summary>
        /// Represents the Microsoft fork of Git with VFS support. Inline version identifier "vfs".
        /// </summary>
        Microsoft,

        /// <summary>
        /// Represents the Apple distribution of Git. Custom version string.
        /// </summary>
        Apple,

        /// <summary>
        /// Represents an unknown distribution of Git that has an unknown inline version identifier.
        /// </summary>
        Unknown,
    }

    public partial class GitVersion : IComparable<GitVersion>, IEquatable<GitVersion>
    {
        /// <summary>
        /// Represents the lowest possible Git version. All other versions will be compared greater than this instance.
        /// </summary>
        public static readonly GitVersion Zero =  new GitVersion(0, 0, 0);

        private static readonly Regex VersionRegex = CreateRegex();
        private static readonly Regex AppleVersionRegex = CreateAppleRegex();

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public int? ReleaseCandidate { get; set; }
        public GitDistributionType Distribution { get; }
        public string? DistributionIdentifier { get; set; }
        public int? Build { get; }
        public int? Revision { get; }
        public string? CommitId { get; set; }
        public string? OriginalString { get; private set; }

        private GitVersion()
        {
        }

        public GitVersion(int major, int minor, int patch, GitDistributionType distribution = GitDistributionType.Core,
            int? build = null, int? revision = null)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Distribution = distribution;
            switch (distribution)
            {
                case GitDistributionType.GitForWindows:
                    DistributionIdentifier = "windows";
                    break;

                case GitDistributionType.Microsoft:
                    DistributionIdentifier = "vfs";
                    break;

                case GitDistributionType.Apple:
                    // Only the build component is allowed in Apple Git - the revision component is not permitted
                    if (revision is not null)
                        throw new ArgumentException("Revision is not supported for the Apple distribution.", nameof(revision));
                    // Apple's version format is special - we don't use an inline identifier
                    DistributionIdentifier = null;
                    break;

                case GitDistributionType.Unknown:
                    // No special handling
                    break;

                case GitDistributionType.Core:
                    DistributionIdentifier = null;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(distribution), distribution, null);
            }

            // If the revision component is specified then so must build component
            if (revision is not null && build is null)
                throw new ArgumentNullException(nameof(build), "Build component cannot be null if revision is specified.");

            Build = build;
            Revision = revision;
        }

        public static GitVersion Parse(string str) =>
            TryParse(str, out GitVersion? version)
#if NETFRAMEWORK
                ? version!
#else
                ? version
#endif
                : throw new FormatException($"Invalid Git version format: {str}");

#if NETFRAMEWORK
        public static bool TryParse(string str, out GitVersion? version)
#else
        public static bool TryParse(string str, [NotNullWhen(true)] out GitVersion? version)
#endif
        {
            Match match = VersionRegex.Match(str);

            if (match.Success)
            {
                int major, minor, patch;
                string? distId = null;
                GitDistributionType dist = GitDistributionType.Core;
                int? rc = null, build = null, revision = null;

                // Major, minor, and patch components are required and must be valid integers.
                if (!int.TryParse(match.Groups["major"].Value, out major) ||
                    !int.TryParse(match.Groups["minor"].Value, out minor) ||
                    !int.TryParse(match.Groups["patch"].Value, out patch))
                {
                    version = null;
                    return false;
                }

                // Release candidate is optional, but if present, it must be a valid integer.
                if (match.Groups["rc"].Success && int.TryParse(match.Groups["rc"].Value, out int rcValue))
                {
                    rc = rcValue;
                }

                // Distribution is optional, but if present, it must be a valid string.
                // Build and revision are also optional, but if present, they must be valid integers
                // and are relative to the distribution identifier.
                if (match.Groups["dist"].Success)
                {
                    distId = match.Groups["dist"].Value;
                    dist = distId.ToLowerInvariant() switch
                    {
                        "windows" => GitDistributionType.GitForWindows,
                        "vfs" => GitDistributionType.Microsoft,
                        _ => GitDistributionType.Unknown
                    };
                    build = match.Groups["build"].Success ? int.Parse(match.Groups["build"].Value) : null;
                    revision = match.Groups["rev"].Success ? int.Parse(match.Groups["rev"].Value) : null;
                }

                // Try to make sense of the remaining parts of the input string.
                string rest = match.Groups["rest"].Value.Trim();

                string? commitId = null;
                if (!string.IsNullOrWhiteSpace(rest))
                {
                    // We have to handle Apple Git specially since their version format string looks like this:
                    //   <major>.<minor>.<patch> (Apple Git-<build>)
                    // There is no revision version component for Apple Git.
                    var appleMatch = AppleVersionRegex.Match(rest);
                    if (appleMatch.Success)
                    {
                        build = int.Parse(appleMatch.Groups["build"].Value);
                        revision = null;
                        dist = GitDistributionType.Apple;
                        distId = null;
                    }
                    // We also check for a 'dirty-build' of Git; that is one that was built from a commit:
                    //   <major>.<minor>.<patch>.g<sha>
                    // where <sha> is the commit ID.
                    else if (rest.StartsWith(".g", StringComparison.OrdinalIgnoreCase))
                    {
                        commitId = rest.Substring(2).ToLowerInvariant();
                    }
                }

                version = new GitVersion(major, minor, patch, dist, build, revision)
                {
                    ReleaseCandidate = rc,
                    CommitId = commitId,
                    DistributionIdentifier = distId,
                    OriginalString = str
                };
                return true;
            }

            version = null;
            return false;
        }

        public override string ToString() => ToString(GitVersionFormat.BuildNumber);

        public string ToString(GitVersionFormat format)
        {
            var sb = new StringBuilder();

            if (format == GitVersionFormat.Tag)
            {
                sb.Append('v');
            }

            sb.Append($"{Major}.{Minor}.{Patch}");

            if (ReleaseCandidate is not null)
            {
                switch (format)
                {
                    case GitVersionFormat.BuildNumber:
                        sb.Append(".rc");
                        break;

                    case GitVersionFormat.Tag:
                        sb.Append("-rc");
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported GitVersionFormat");
                }

                sb.Append(ReleaseCandidate);
            }

            switch (Distribution)
            {
                case GitDistributionType.Core:
                    return sb.ToString();

                case GitDistributionType.Apple:
                    sb.Append(" (Apple Git");
                    if (Build is not null)
                        sb.Append($"-{Build}");
                    sb.Append(')');
                    return sb.ToString();

                default:
                    sb.Append($".{DistributionIdentifier}");
                    break;
            }

            if (Build is not null)
            {
                sb.Append($".{Build}");
            }
            else if (Revision is not null)
            {
                Debug.Fail("Build should not be null if Revision is set.");
                return sb.ToString(); // Don't append Revision if Build is null as this would be misleading
            }

            if (Revision is not null)
            {
                sb.Append($".{Revision}");
            }

            if (CommitId is not null)
            {
                sb.Append($".g{CommitId}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines whether this <see cref="GitVersion"/> can be compared with another <see cref="GitVersion"/>.
        /// Two versions are comparable if they have the same distribution.
        /// </summary>
        /// <param name="other">The other GitVersion to check compatibility with.</param>
        /// <returns>True if the versions can be compared; otherwise, false.</returns>
        public bool IsComparableTo(GitVersion? other)
        {
            if (other is null)
            {
                return false;
            }

            if (Distribution != other.Distribution)
            {
                return false;
            }

            return string.Equals(DistributionIdentifier, other.DistributionIdentifier, StringComparison.Ordinal);
        }

        public int CompareTo(GitVersion? other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (other is null)
            {
                return 1;
            }

            // Check for distribution mismatch and throw exception if they don't match
            if (!IsComparableTo(other))
            {
                throw new GitVersionMismatchException(this, other);
            }

            var majorCmp = Major.CompareTo(other.Major);
            if (majorCmp != 0) return majorCmp;

            var minorCmp = Minor.CompareTo(other.Minor);
            if (minorCmp != 0) return minorCmp;

            var patchCmp = Patch.CompareTo(other.Patch);
            if (patchCmp != 0) return patchCmp;

            // Compare release candidates: stable versions (null) are greater than release candidate versions
            var rcCmp = (ReleaseCandidate, other.ReleaseCandidate) switch
            {
                (null, null)   =>  0,                            // Both are stable releases, equal
                (null,    _)   =>  1,                            // This is stable, other is RC -> this is greater
                (_   , null)   => -1,                            // This is RC, other is stable -> this is less
                var (rc1, rc2) => rc1.Value.CompareTo(rc2.Value) // Both are RC, compare values
            };
            if (rcCmp != 0) return rcCmp;

            // Since we've already verified distributions are equal, we can skip the dist comparison
            var buildCmp = Build?.CompareTo(other.Build) ?? 0;
            if (buildCmp != 0) return buildCmp;

            var revCmp = Revision?.CompareTo(other.Revision) ?? 0;
            return revCmp;

            // Ignore the CommitID
        }

        /// <summary>
        /// Converts this GitVersion to a core version by ignoring distribution information.
        /// This is useful for comparing versions without considering distribution-specific details.
        /// </summary>
        public GitVersion ToCoreVersion()
        {
            // Convert to a core version by ignoring distribution information
            return new GitVersion(Major, Minor, Patch, GitDistributionType.Core)
            {
                ReleaseCandidate = ReleaseCandidate
            };
        }

#if NETFRAMEWORK
        public override int GetHashCode()
        {
            return Major.GetHashCode() ^
                   Minor.GetHashCode() ^
                   Patch.GetHashCode() ^
                   Distribution.GetHashCode() ^
                   (Build?.GetHashCode() ?? 0) ^
                   (Revision?.GetHashCode() ?? 0);
        }
#else
        public override int GetHashCode()
            => HashCode.Combine(Major, Minor, Patch, Distribution, Build, Revision);
#endif

        /// <summary>
        /// Check if this version is equal to another version.
        /// </summary>
        public bool Equals(GitVersion? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return CompareTo(other) == 0;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return ((IEquatable<GitVersion>)this).Equals((GitVersion)obj);
        }

        public static bool operator ==(GitVersion? left, GitVersion? right)
        {
            return left is null && right is null ||
                   left is not null && left.CompareTo(right) == 0;
        }

        public static bool operator !=(GitVersion? left, GitVersion? right)
        {
            return !(left == right);
        }

        public static bool operator <(GitVersion? left, GitVersion? right)
        {
            if (left is null)
            {
                return right is not null;
            }

            return left.CompareTo(right) < 0;
        }

        public static bool operator >(GitVersion? left, GitVersion? right)
        {
            if (left is null)
            {
                return false;
            }

            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(GitVersion? left, GitVersion? right)
        {
            if (left is null)
            {
                return true;
            }

            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(GitVersion? left, GitVersion? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.CompareTo(right) >= 0;
        }

        private const string RegexPattern =
            @"^v?(?'major'\d+)(?:\.(?'minor'\d+))(?:\.(?'patch'\d+))(?:[-.]rc(?'rc'\d+))?(?:\.(?'dist'[^\.]+)(?:\.(?'build'\d+)(?:\.(?'rev'\d+))?)?)?(?'rest'.+)?";

#if NETFRAMEWORK
        private static Regex CreateRegex()
            => new Regex(RegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
#else
        [GeneratedRegex(RegexPattern, RegexOptions.IgnoreCase)]
        private static partial Regex CreateRegex();
#endif

        private const string AppleRegexPattern =
            @"\(Apple Git-(?'build'\d+)\)";

#if NETFRAMEWORK
        private static Regex CreateAppleRegex()
            => new Regex(AppleRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
#else
        [GeneratedRegex(AppleRegexPattern, RegexOptions.IgnoreCase)]
        private static partial Regex CreateAppleRegex();
#endif
    }

    public enum GitVersionFormat
    {
        /// <summary>
        /// Format the version as Git build number. Example: "1.2.3.rc4".
        /// </summary>
        BuildNumber,

        /// <summary>
        /// Format the version as a Git tag. Example: "v1.2.3-rc4".
        /// </summary>
        Tag,
    }

    /// <summary>
    /// Exception thrown when comparing GitVersion instances with different platform.
    /// </summary>
    public class GitVersionMismatchException : InvalidOperationException
    {
        public GitVersion Version1 { get; }
        public GitVersion Version2 { get; }

        public GitVersionMismatchException(GitVersion version1, GitVersion version2)
            : base(GetErrorMessage(version1, version2))
        {
            Version1 = version1;
            Version2 = version2;
        }

        private static string GetErrorMessage(GitVersion version1, GitVersion version2)
        {
            var sb = new StringBuilder("Cannot compare Git versions with different distribution: ");

            sb.Append($"'{version1.Distribution}'");
            if (version1.DistributionIdentifier is not null)
            {
                sb.Append($" (\"{version1.DistributionIdentifier}\")");
            }

            sb.Append($" and '{version2.Distribution}'");
            if (version2.DistributionIdentifier is not null)
            {
                sb.Append($" (\"{version2.DistributionIdentifier}\")");
            }

            return sb.ToString();
        }

        public GitVersionMismatchException(GitVersion version1, GitVersion version2, string message)
            : base(message)
        {
            Version1 = version1;
            Version2 = version2;
        }

        public GitVersionMismatchException(GitVersion version1, GitVersion version2, string message, Exception innerException)
            : base(message, innerException)
        {
            Version1 = version1;
            Version2 = version2;
        }
    }
}
