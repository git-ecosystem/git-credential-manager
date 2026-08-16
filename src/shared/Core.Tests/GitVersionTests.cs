#nullable enable
using System;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class GitVersionTests
    {
        [Fact]
        public void GitVersion_Parse_ValidVersionString_ReturnsCorrectVersion()
        {
            GitVersion version = GitVersion.Parse("2.30.1");

            Assert.Equal(2, version.Major);
            Assert.Equal(30, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Null(version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.Core, version.Distribution);
            Assert.Null(version.DistributionIdentifier);
            Assert.Null(version.Build);
            Assert.Null(version.Revision);
        }

        [Fact]
        public void GitVersion_Parse_VersionWithBuild_ReturnsCorrectVersion()
        {
            var version = GitVersion.Parse("2.30.1.windows.2");

            Assert.Equal(2, version.Major);
            Assert.Equal(30, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Null(version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.GitForWindows, version.Distribution);
            Assert.Equal("windows", version.DistributionIdentifier);
            Assert.Equal(2, version.Build);
            Assert.Null(version.Revision);
        }

        [Fact]
        public void GitVersion_Parse_VersionWithBuildRevision_ReturnsCorrectVersion()
        {
            var version = GitVersion.Parse("2.30.1.vfs.2.3");

            Assert.Equal(2, version.Major);
            Assert.Equal(30, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Null(version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.Microsoft, version.Distribution);
            Assert.Equal("vfs", version.DistributionIdentifier);
            Assert.Equal(2, version.Build);
            Assert.Equal(3, version.Revision);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid")]
        [InlineData("a.b.c")]
        [InlineData("2.invalid.1")]
        [InlineData("2.30.invalid")]
        [InlineData("2.30")]
        [InlineData("2")]
        public void GitVersion_Parse_InvalidVersionString_ThrowsFormatException(string invalidVersion)
        {
            Assert.Throws<FormatException>(() => GitVersion.Parse(invalidVersion));
        }

        [Theory]
        [InlineData("2.30.1.vfs.1.0.extra")]
        [InlineData("2.30.1.vfs.1.0-extra")]
        [InlineData("2.30.1.vfs.1.0.extra.1")]
        [InlineData("2.30.1.vfs.1.0-extra.1")]
        public void GitVersion_Parse_ExtraInformationAtEnd_IgnoresExtraInfo(string versionString)
        {
            var version = GitVersion.Parse(versionString);

            Assert.Equal("2.30.1.vfs.1.0", version.ToString());
            Assert.Equal(versionString, version.OriginalString);
        }

        [Fact]
        public void GitVersion_TryParse_ValidVersionString_ReturnsTrueAndCorrectVersion()
        {
            bool result = GitVersion.TryParse("2.30.1", out var version);

            Assert.True(result);
            Assert.NotNull(version);
            Assert.Equal(2, version.Major);
            Assert.Equal(30, version.Minor);
            Assert.Equal(1, version.Patch);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid")]
        [InlineData("a.b.c")]
        public void GitVersion_TryParse_InvalidVersionString_ReturnsFalseAndNullVersion(string invalidVersion)
        {
            bool result = GitVersion.TryParse(invalidVersion, out var version);

            Assert.False(result);
            Assert.Null(version);
        }

        [Fact]
        public void GitVersion_ToString_StandardVersion_ReturnsCorrectFormat()
        {
            var version = new GitVersion(2, 30, 1);

            Assert.Equal("2.30.1", version.ToString());
        }

        [Fact]
        public void GitVersion_ToString_VersionWithDistributionOnly_ReturnsCorrectFormat()
        {
            var version = new GitVersion(2, 30, 1, GitDistributionType.Unknown)
            {
                DistributionIdentifier = "custom"
            };

            Assert.Equal("2.30.1.custom", version.ToString());
        }

        [Fact]
        public void GitVersion_ToString_VersionWithBuild_ReturnsCorrectFormat()
        {
            var version = new GitVersion(2, 30, 1, GitDistributionType.GitForWindows, 1, 0);

            Assert.Equal("2.30.1.windows.1.0", version.ToString());
        }

        [Fact]
        public void GitVersion_ToString_VersionWithBuildRevision_ReturnsCorrectFormat()
        {
            var version = new GitVersion(2, 30, 1, GitDistributionType.Microsoft, 1, 2);

            Assert.Equal("2.30.1.vfs.1.2", version.ToString());
        }

        [Fact]
        public void GitVersion_ToString_VersionWithZeroBuildRevision_ReturnsCorrectFormat()
        {
            var version = new GitVersion(2, 30, 1, GitDistributionType.Microsoft, 0, 0);

            Assert.Equal("2.30.1.vfs.0.0", version.ToString());
        }

        [Fact]
        public void GitVersion_ToString_ParsedStandardVersion_RoundTripWorks()
        {
            var originalString = "2.30.1";
            var version = GitVersion.Parse(originalString);

            Assert.Equal(originalString, version.ToString());
        }

        [Fact]
        public void GitVersion_ToString_ParsedDistributionVersion_RoundTripWorks()
        {
            var originalString = "2.30.1.vfs.1.2";
            var version = GitVersion.Parse(originalString);

            Assert.Equal(originalString, version.ToString());
        }

        [Fact]
        public void GitVersion_ToString_ZeroVersionNumbers_ReturnsCorrectFormat()
        {
            var version = GitVersion.Zero;

            Assert.Equal("0.0.0", version.ToString());
        }

        [Fact]
        public void GitVersion_ToString_LargeVersionNumbers_ReturnsCorrectFormat()
        {
            var version = new GitVersion(999,888, 777 );

            Assert.Equal("999.888.777", version.ToString());
        }

        [Fact]
        public void GitVersion_ToString_AppleGit_ReturnsCorrectFormat()
        {
            var version = new GitVersion(2, 50, 1, GitDistributionType.Apple, 155);

            Assert.Equal("2.50.1 (Apple Git-155)", version.ToString());
        }

        [Fact]
        public void GitVersion_CompareTo_SameVersion_ReturnsZero()
        {
            var version1 = GitVersion.Parse("2.30.1");
            var version2 = GitVersion.Parse("2.30.1");

            Assert.Equal(0, version1.CompareTo(version2));
        }

        [Fact]
        public void GitVersion_CompareTo_SameReference_ReturnsZero()
        {
            var version = GitVersion.Parse("2.30.1");

            Assert.Equal(0, version.CompareTo(version));
        }

        [Fact]
        public void GitVersion_CompareTo_WithNull_ReturnsPositive()
        {
            var version = GitVersion.Parse("2.30.1");

            Assert.True(version.CompareTo(null) > 0);
        }

        [Theory]
        [InlineData("2.30.1", "2.30.2", -1)]
        [InlineData("2.30.2", "2.30.1", 1)]
        [InlineData("2.29.1", "2.30.1", -1)]
        [InlineData("2.31.1", "2.30.1", 1)]
        [InlineData("1.30.1", "2.30.1", -1)]
        [InlineData("3.30.1", "2.30.1", 1)]
        public void GitVersion_CompareTo_DifferentVersions_ReturnsCorrectComparison(string str1, string str2, int expectedSign)
        {
            var version1 = GitVersion.Parse(str1);
            var version2 = GitVersion.Parse(str2);

            var result = version1.CompareTo(version2);
            Assert.Equal(expectedSign, Math.Sign(result));
        }

        [Theory]
        [InlineData("2.30.1.windows.1.0", "2.30.1.windows.2.0", -1)]
        [InlineData("2.30.1.windows.1.1", "2.30.1.windows.1.0", 1)]
        public void GitVersion_CompareTo_VersionsWithDistribution_ReturnsCorrectComparison(string str1, string str2, int expectedSign)
        {
            var version1 = GitVersion.Parse(str1);
            var version2 = GitVersion.Parse(str2);

            var result = version1.CompareTo(version2);
            Assert.Equal(expectedSign, Math.Sign(result));
        }

        [Fact]
        public void GitVersion_LessThanOperator_WithValidVersions_ReturnsCorrectResult()
        {
            var version1 = GitVersion.Parse("2.30.1");
            var version2 = GitVersion.Parse("2.30.2");

            Assert.True(version1 < version2);
            Assert.False(version2 < version1);
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.False(version1 < version1);
#pragma warning restore CS1718
        }

        [Fact]
        public void GitVersion_LessThanOperator_WithNull_ReturnsCorrectResult()
        {
            var version = GitVersion.Parse("2.30.1");

            Assert.True(null < version);
            Assert.False(version < null);
        }

        [Fact]
        public void GitVersion_LessThanOrEqualOperator_WithValidVersions_ReturnsCorrectResult()
        {
            var version1 = GitVersion.Parse("2.30.1");
            var version2 = GitVersion.Parse("2.30.2");
            var version3 = GitVersion.Parse("2.30.1");

            Assert.True(version1 <= version2);
            Assert.True(version1 <= version3);
            Assert.False(version2 <= version1);
        }

        [Fact]
        public void GitVersion_LessThanOrEqualOperator_WithNull_ReturnsCorrectResult()
        {
            var version = GitVersion.Parse("2.30.1");
            GitVersion? nullVersion = null;

            Assert.True(null <= version);
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.True(nullVersion <= nullVersion);
#pragma warning restore CS1718
            Assert.False(version <= null);
        }

        [Fact]
        public void GitVersion_GreaterThanOperator_WithValidVersions_ReturnsCorrectResult()
        {
            var version1 = GitVersion.Parse("2.30.1");
            var version2 = GitVersion.Parse("2.30.2");

            Assert.True(version2 > version1);
            Assert.False(version1 > version2);
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.False(version1 > version1);
#pragma warning restore CS1718
        }

        [Fact]
        public void GitVersion_GreaterThanOperator_WithNull_ReturnsCorrectResult()
        {
            var version = GitVersion.Parse("2.30.1");

            Assert.True(version > null);
            Assert.False(null > version);
        }

        [Fact]
        public void GitVersion_GreaterThanOrEqualOperator_WithValidVersions_ReturnsCorrectResult()
        {
            var version1 = GitVersion.Parse("2.30.1");
            var version2 = GitVersion.Parse("2.30.2");
            var version3 = GitVersion.Parse("2.30.1");

            Assert.True(version2 >= version1);
            Assert.True(version1 >= version3);
            Assert.False(version1 >= version2);
        }

        [Fact]
        public void GitVersion_GreaterThanOrEqualOperator_WithNull_ReturnsCorrectResult()
        {
            var version = GitVersion.Parse("2.30.1");
            GitVersion? nullVersion = null;

            Assert.True(version >= null);
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.True(nullVersion >= nullVersion);
#pragma warning restore CS1718
            Assert.False(null >= version);
        }

        [Fact]
        public void GitVersion_EqualityOperator_WithSameVersions_ReturnsTrue()
        {
            var version1 = GitVersion.Parse("2.30.1");
            var version2 = GitVersion.Parse("2.30.1");

            Assert.True(version1 == version2);
            Assert.False(version1 != version2);
        }

        [Fact]
        public void GitVersion_EqualityOperator_WithDifferentVersions_ReturnsFalse()
        {
            var version1 = GitVersion.Parse("2.30.1");
            var version2 = GitVersion.Parse("2.30.2");

            Assert.False(version1 == version2);
            Assert.True(version1 != version2);
        }

        [Fact]
        public void GitVersion_EqualityOperator_WithNull_ReturnsCorrectResult()
        {
            var version = GitVersion.Parse("2.30.1");
            GitVersion? nullVersion = null;

            Assert.False(version == null);
            Assert.False(null == version);
#pragma warning disable CS1718 // Comparison made to same variable
            Assert.True(nullVersion == nullVersion);
            Assert.True(version != null);
            Assert.True(null != version);
            Assert.False(nullVersion != nullVersion);
#pragma warning restore CS1718
        }

        [Fact]
        public void GitVersion_Equality_WorksCorrectly()
        {
            var version1 = new GitVersion (2, 30, 1);
            var version2 = new GitVersion (2, 30, 1);
            var version3 = new GitVersion (2, 30, 2);

            Assert.Equal(version1, version2);
            Assert.NotEqual(version1, version3);
            Assert.Equal(version1.GetHashCode(), version2.GetHashCode());
        }

        [Fact]
        public void GitVersion_IsComparable_WithNull_ReturnsFalse()
        {
            var version = GitVersion.Parse("2.30.1");

            Assert.False(version.IsComparableTo(null));
        }

        [Fact]
        public void GitVersion_IsComparable_ReleaseCandidateVersions_ReturnsTrue()
        {
            var rc1 = GitVersion.Parse("2.30.1.rc1");
            var rc2 = GitVersion.Parse("2.30.1-rc2");
            var stable = GitVersion.Parse("2.30.1");

            Assert.True(rc1.IsComparableTo(rc2));
            Assert.True(rc1.IsComparableTo(stable));
            Assert.True(stable.IsComparableTo(rc1));
        }

        [Fact]
        public void GitVersion_IsComparable_WithSameDistribution_ReturnsTrue()
        {
            var version1 = GitVersion.Parse("2.30.1.windows.1.0");
            var version2 = GitVersion.Parse("2.30.2.windows.2.0");

            Assert.True(version1.IsComparableTo(version2));
        }

        [Fact]
        public void GitVersion_IsComparable_WithDifferentDistribution_ReturnsFalse()
        {
            var version1 = GitVersion.Parse("2.30.1.windows.1.0");
            var version2 = GitVersion.Parse("2.30.1.vfs.1.0");

            Assert.False(version1.IsComparableTo(version2));
        }

        [Fact]
        public void GitVersion_IsComparable_BothWithoutDistribution_ReturnsTrue()
        {
            var version1 = GitVersion.Parse("2.30.1");
            var version2 = GitVersion.Parse("2.31.0");

            Assert.True(version1.IsComparableTo(version2));
        }

        [Fact]
        public void GitVersion_IsComparable_OneWithDistributionOneWithout_ReturnsFalse()
        {
            var version1 = GitVersion.Parse("2.30.1");
            var version2 = GitVersion.Parse("2.30.1.windows.1.0");

            Assert.False(version1.IsComparableTo(version2));
        }

        [Fact]
        public void GitVersion_CompareTo_ReleaseCandidateVsStable_ReleaseCandidateIsLess()
        {
            var rcVersion = GitVersion.Parse("2.30.1.rc1");
            var stableVersion = GitVersion.Parse("2.30.1");

            Assert.True(rcVersion.CompareTo(stableVersion) < 0);
            Assert.True(stableVersion.CompareTo(rcVersion) > 0);
        }

        [Fact]
        public void GitVersion_CompareTo_DifferentReleaseCandidate_ReturnsCorrectComparison()
        {
            var rc1 = GitVersion.Parse("2.30.1.rc1");
            var rc2 = GitVersion.Parse("2.30.1-rc2");

            Assert.True(rc1.CompareTo(rc2) < 0);
            Assert.True(rc2.CompareTo(rc1) > 0);
        }

        [Fact]
        public void GitVersion_CompareTo_SameReleaseCandidate_ReturnsZero()
        {
            var rc1 = GitVersion.Parse("2.30.1-rc5");
            var rc2 = GitVersion.Parse("2.30.1.rc5");

            Assert.Equal(0, rc1.CompareTo(rc2));
        }

        [Theory]
        [InlineData("2.30.1.rc1", "2.30.1", -1)]
        [InlineData("2.30.1", "2.30.1.rc1", 1)]
        [InlineData("2.30.1.rc1", "2.30.1.rc2", -1)]
        [InlineData("2.30.1.rc10", "2.30.1.rc2", 1)]
        [InlineData("2.30.0.rc1", "2.30.1.rc1", -1)]
        public void GitVersion_CompareTo_ReleaseCandidateVersions_ReturnsCorrectComparison(string str1, string str2,
            int expectedSign)
        {
            var version1 = GitVersion.Parse(str1);
            var version2 = GitVersion.Parse(str2);

            var result = version1.CompareTo(version2);
            Assert.Equal(expectedSign, Math.Sign(result));
        }

        [Fact]
        public void GitVersion_CompareTo_IncompatibleVersions_ThrowsGitVersionMismatchException()
        {
            var version1 = GitVersion.Parse("2.30.1.windows.1.0");
            var version2 = GitVersion.Parse("2.30.1.vfs.1.0");

            var exception = Assert.Throws<GitVersionMismatchException>(() => version1.CompareTo(version2));
            Assert.NotNull(exception.Version1);
            Assert.NotNull(exception.Version2);
            Assert.Equal(version1, exception.Version1);
            Assert.Equal(version2, exception.Version2);
        }

        [Fact]
        public void GitVersion_CompareTo_ReleaseCandidateWithDistributionVersions_ReturnsCorrectComparison()
        {
            var rc1Windows = GitVersion.Parse("2.30.1.rc1.windows.1.0");
            var rc2Windows = GitVersion.Parse("2.30.1-rc2.windows.1.0");
            var stableWindows = GitVersion.Parse("2.30.1.windows.1.0");

            Assert.True(rc1Windows.CompareTo(rc2Windows) < 0);
            Assert.True(rc1Windows.CompareTo(stableWindows) < 0);
            Assert.True(stableWindows.CompareTo(rc1Windows) > 0);
        }

        [Fact]
        public void GitVersion_CompareTo_StandardVersionWithDistributionVersion_ThrowsGitVersionMismatchException()
        {
            var version1 = GitVersion.Parse("2.30.1");
            var version2 = GitVersion.Parse("2.30.1.windows.1.0");

            var exception = Assert.Throws<GitVersionMismatchException>(() => version1.CompareTo(version2));
            Assert.Equal(version1, exception.Version1);
            Assert.Equal(version2, exception.Version2);
        }

        [Fact]
        public void GitVersion_LessThanOperator_ReleaseCandidateVersions_ReturnsCorrectResult()
        {
            var rc1 = GitVersion.Parse("2.30.1.rc1");
            var rc2 = GitVersion.Parse("2.30.1-rc2");
            var stable = GitVersion.Parse("2.30.1");

            Assert.True(rc1 < rc2);
            Assert.True(rc1 < stable);
            Assert.False(rc2 < rc1);
            Assert.False(stable < rc1);
        }

        [Fact]
        public void GitVersion_GreaterThanOperator_ReleaseCandidateVersions_ReturnsCorrectResult()
        {
            var rc1 = GitVersion.Parse("2.30.1.rc1");
            var rc2 = GitVersion.Parse("2.30.1-rc2");
            var stable = GitVersion.Parse("2.30.1");

            Assert.True(rc2 > rc1);
            Assert.True(stable > rc1);
            Assert.False(rc1 > rc2);
            Assert.False(rc1 > stable);
        }

        [Fact]
        public void GitVersion_LessThanOrEqualOperator_ReleaseCandidateVersions_ReturnsCorrectResult()
        {
            var rc1 = GitVersion.Parse("2.30.1.rc1");
            var rc2 = GitVersion.Parse("2.30.1.rc1");
            var rc3 = GitVersion.Parse("2.30.1.rc2");

            Assert.True(rc1 <= rc2);
            Assert.True(rc1 <= rc3);
            Assert.False(rc3 <= rc1);
        }

        [Fact]
        public void GitVersion_GreaterThanOrEqualOperator_ReleaseCandidateVersions_ReturnsCorrectResult()
        {
            var rc1 = GitVersion.Parse("2.30.1.rc1");
            var rc2 = GitVersion.Parse("2.30.1.rc1");
            var rc3 = GitVersion.Parse("2.30.1.rc2");

            Assert.True(rc2 >= rc1);
            Assert.True(rc3 >= rc1);
            Assert.False(rc1 >= rc3);
        }

        [Fact]
        public void GitVersion_LessThanOperator_IncompatibleVersions_ThrowsGitVersionMismatchException()
        {
            var version1 = GitVersion.Parse("2.30.1.windows.1.0");
            var version2 = GitVersion.Parse("2.30.1.vfs.1.0");

            Assert.Throws<GitVersionMismatchException>(() => version1 < version2);
        }

        [Fact]
        public void GitVersion_GreaterThanOperator_IncompatibleVersions_ThrowsGitVersionMismatchException()
        {
            var version1 = GitVersion.Parse("2.30.1.windows.1.0");
            var version2 = GitVersion.Parse("2.30.1.vfs.1.0");

            Assert.Throws<GitVersionMismatchException>(() => version1 > version2);
        }

        [Fact]
        public void GitVersion_LessThanOrEqualOperator_IncompatibleVersions_ThrowsGitVersionMismatchException()
        {
            var version1 = GitVersion.Parse("2.30.1.windows.1.0");
            var version2 = GitVersion.Parse("2.30.1.vfs.1.0");

            Assert.Throws<GitVersionMismatchException>(() => version1 <= version2);
        }

        [Fact]
        public void GitVersion_GreaterThanOrEqualOperator_IncompatibleVersions_ThrowsGitVersionMismatchException()
        {
            var version1 = GitVersion.Parse("2.30.1.windows.1.0");
            var version2 = GitVersion.Parse("2.30.1.vfs.1.0");

            Assert.Throws<GitVersionMismatchException>(() => version1 >= version2);
        }

        [Fact]
        public void GitVersion_ToCoreVersion_StandardVersion_ReturnsSameVersion()
        {
            var version = GitVersion.Parse("2.30.1");
            var coreVersion = version.ToCoreVersion();

            Assert.Equal(2, coreVersion.Major);
            Assert.Equal(30, coreVersion.Minor);
            Assert.Equal(1, coreVersion.Patch);
            Assert.Equal(GitDistributionType.Core, coreVersion.Distribution);
            Assert.Null(coreVersion.DistributionIdentifier);
            Assert.Null(coreVersion.Build);
            Assert.Null(coreVersion.Revision);
            Assert.Equal(version, coreVersion);
        }

        [Fact]
        public void GitVersion_ToCoreVersion_ReleaseCandidateDotVersion_KeepsReleaseCandidate()
        {
            var rcVersion = GitVersion.Parse("2.30.1.rc3");
            var coreVersion = rcVersion.ToCoreVersion();

            Assert.Equal(2, coreVersion.Major);
            Assert.Equal(30, coreVersion.Minor);
            Assert.Equal(1, coreVersion.Patch);
            Assert.Equal(3, coreVersion.ReleaseCandidate);
            Assert.Equal("2.30.1.rc3", coreVersion.ToString());
        }

        [Fact]
        public void GitVersion_ToCoreVersion_ReleaseCandidateDashVersion_KeepsReleaseCandidate()
        {
            var rcVersion = GitVersion.Parse("2.30.1-rc3");
            var coreVersion = rcVersion.ToCoreVersion();

            Assert.Equal(2, coreVersion.Major);
            Assert.Equal(30, coreVersion.Minor);
            Assert.Equal(1, coreVersion.Patch);
            Assert.Equal(3, coreVersion.ReleaseCandidate);
            Assert.Equal("2.30.1.rc3", coreVersion.ToString());
        }

        [Fact]
        public void GitVersion_ToCoreVersion_ReleaseCandidateDotWithDistribution_RemovesAllExtraInfo()
        {
            var rcVersion = GitVersion.Parse("2.30.1.rc2.windows.1");
            var coreVersion = rcVersion.ToCoreVersion();

            Assert.Equal(2, coreVersion.Major);
            Assert.Equal(30, coreVersion.Minor);
            Assert.Equal(1, coreVersion.Patch);
            Assert.Equal(2, coreVersion.ReleaseCandidate);
            Assert.Equal(GitDistributionType.Core, coreVersion.Distribution);
            Assert.Null(coreVersion.DistributionIdentifier);
            Assert.Null(coreVersion.Build);
            Assert.Equal("2.30.1.rc2", coreVersion.ToString());
        }

        [Fact]
        public void GitVersion_ToCoreVersion_ReleaseCandidateDashWithDistribution_RemovesAllExtraInfo()
        {
            var rcVersion = GitVersion.Parse("2.30.1-rc2.windows.1");
            var coreVersion = rcVersion.ToCoreVersion();

            Assert.Equal(2, coreVersion.Major);
            Assert.Equal(30, coreVersion.Minor);
            Assert.Equal(1, coreVersion.Patch);
            Assert.Equal(2, coreVersion.ReleaseCandidate);
            Assert.Equal(GitDistributionType.Core, coreVersion.Distribution);
            Assert.Null(coreVersion.DistributionIdentifier);
            Assert.Null(coreVersion.Build);
            Assert.Equal("2.30.1.rc2", coreVersion.ToString());
        }

        [Fact]
        public void GitVersion_ToCoreVersion_VersionWithDistributionOnly_RemovesDistribution()
        {
            var version = GitVersion.Parse("2.30.1.custom");
            var coreVersion = version.ToCoreVersion();

            Assert.Equal(2, coreVersion.Major);
            Assert.Equal(30, coreVersion.Minor);
            Assert.Equal(1, coreVersion.Patch);
            Assert.Equal(GitDistributionType.Core, coreVersion.Distribution);
            Assert.Null(coreVersion.DistributionIdentifier);
            Assert.Null(coreVersion.Build);
            Assert.Null(coreVersion.Revision);
            Assert.Equal("2.30.1", coreVersion.ToString());
        }

        [Fact]
        public void GitVersion_ToCoreVersion_VersionWithBuild_RemovesDistributionInfo()
        {
            var version = GitVersion.Parse("2.30.1.windows.2");
            var coreVersion = version.ToCoreVersion();

            Assert.Equal(2, coreVersion.Major);
            Assert.Equal(30, coreVersion.Minor);
            Assert.Equal(1, coreVersion.Patch);
            Assert.Equal(GitDistributionType.Core, coreVersion.Distribution);
            Assert.Null(coreVersion.DistributionIdentifier);
            Assert.Null(coreVersion.Build);
            Assert.Null(coreVersion.Revision);
            Assert.Equal("2.30.1", coreVersion.ToString());
        }

        [Fact]
        public void GitVersion_ToCoreVersion_VersionWithBuildRevision_RemovesAllDistributionInfo()
        {
            var version = GitVersion.Parse("2.30.1.vfs.2.3");
            var coreVersion = version.ToCoreVersion();

            Assert.Equal(2, coreVersion.Major);
            Assert.Equal(30, coreVersion.Minor);
            Assert.Equal(1, coreVersion.Patch);
            Assert.Equal(GitDistributionType.Core, coreVersion.Distribution);
            Assert.Null(coreVersion.DistributionIdentifier);
            Assert.Null(coreVersion.Build);
            Assert.Null(coreVersion.Revision);
            Assert.Equal("2.30.1", coreVersion.ToString());
        }

        [Fact]
        public void GitVersion_ToCoreVersion_DifferentDistributionsSameCore_ProduceSameCoreVersion()
        {
            var windowsVersion = GitVersion.Parse("2.30.1.windows.1.0");
            var vfsVersion = GitVersion.Parse("2.30.1.vfs.2.3");

            var windowsCore = windowsVersion.ToCoreVersion();
            var vfsCore = vfsVersion.ToCoreVersion();

            Assert.Equal(windowsCore, vfsCore);
            Assert.Equal("2.30.1", windowsCore.ToString());
            Assert.Equal("2.30.1", vfsCore.ToString());
        }

        [Fact]
        public void GitVersion_ToCoreVersion_CoreVersionsAreComparable()
        {
            var windowsVersion = GitVersion.Parse("2.30.1.windows.1.0");
            var vfsVersion = GitVersion.Parse("2.30.2.vfs.1.0");

            var windowsCore = windowsVersion.ToCoreVersion();
            var vfsCore = vfsVersion.ToCoreVersion();

            // These should be comparable since they're both core versions
            Assert.True(windowsCore.IsComparableTo(vfsCore));
            Assert.True(windowsCore < vfsCore);
        }

        [Fact]
        public void GitVersion_ToCoreVersion_PreservesOriginalVersionIntegrity()
        {
            var originalVersion = GitVersion.Parse("2.30.1.windows.2.3");
            var coreVersion = originalVersion.ToCoreVersion();

            // Original version should be unchanged
            Assert.Equal(2, originalVersion.Major);
            Assert.Equal(30, originalVersion.Minor);
            Assert.Equal(1, originalVersion.Patch);
            Assert.Equal(GitDistributionType.GitForWindows, originalVersion.Distribution);
            Assert.Equal("windows", originalVersion.DistributionIdentifier);
            Assert.Equal(2, originalVersion.Build);
            Assert.Equal(3, originalVersion.Revision);
            Assert.Equal("2.30.1.windows.2.3", originalVersion.ToString());

            // Core version should have no distribution info
            Assert.Equal("2.30.1", coreVersion.ToString());
        }

        [Fact]
        public void GitVersion_Parse_VersionWithReleaseCandidateDot_ReturnsCorrectVersion()
        {
            var version = GitVersion.Parse("2.50.1.rc1");

            Assert.Equal(2, version.Major);
            Assert.Equal(50, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Equal(1, version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.Core, version.Distribution);
            Assert.Null(version.DistributionIdentifier);
            Assert.Null(version.Build);
            Assert.Null(version.Revision);
        }

        [Fact]
        public void GitVersion_Parse_VersionWithReleaseCandidateDash_ReturnsCorrectVersion()
        {
            var version = GitVersion.Parse("2.50.1-rc1");

            Assert.Equal(2, version.Major);
            Assert.Equal(50, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Equal(1, version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.Core, version.Distribution);
            Assert.Null(version.DistributionIdentifier);
            Assert.Null(version.Build);
            Assert.Null(version.Revision);
        }

        [Fact]
        public void GitVersion_Parse_VersionWithReleaseCandidateDotAndDistribution_ReturnsCorrectVersion()
        {
            var version = GitVersion.Parse("2.50.1.rc1.windows.1");

            Assert.Equal(2, version.Major);
            Assert.Equal(50, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Equal(1, version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.GitForWindows, version.Distribution);
            Assert.Equal("windows", version.DistributionIdentifier);
            Assert.Equal(1, version.Build);
            Assert.Null(version.Revision);
        }

        [Fact]
        public void GitVersion_Parse_VersionWithReleaseCandidateDashAndDistribution_ReturnsCorrectVersion()
        {
            var version = GitVersion.Parse("2.50.1-rc1.windows.1");

            Assert.Equal(2, version.Major);
            Assert.Equal(50, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Equal(1, version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.GitForWindows, version.Distribution);
            Assert.Equal("windows", version.DistributionIdentifier);
            Assert.Equal(1, version.Build);
            Assert.Null(version.Revision);
        }

        [Fact]
        public void GitVersion_Parse_VersionWithReleaseCandidateAndBuildRevision_ReturnsCorrectVersion()
        {
            var version = GitVersion.Parse("2.50.1.rc1.vfs.0.1");

            Assert.Equal(2, version.Major);
            Assert.Equal(50, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Equal(1, version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.Microsoft, version.Distribution);
            Assert.Equal("vfs", version.DistributionIdentifier);
            Assert.Equal(0, version.Build);
            Assert.Equal(1, version.Revision);
        }

        [Fact]
        public void GitVersion_Parse_VersionWithHigherReleaseCandidate_ReturnsCorrectVersion()
        {
            var version = GitVersion.Parse("2.30.1.rc15");

            Assert.Equal(2, version.Major);
            Assert.Equal(30, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Equal(15, version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.Core, version.Distribution);
            Assert.Null(version.DistributionIdentifier);
        }

        [Fact]
        public void GitVersion_Parse_VersionWithReleaseCandidateDotCaseInsensitive_ReturnsCorrectVersion()
        {
            var version = GitVersion.Parse("2.30.1.RC2");

            Assert.Equal(2, version.Major);
            Assert.Equal(30, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Equal(2, version.ReleaseCandidate);
        }

        [Fact]
        public void GitVersion_Parse_VersionWithReleaseCandidateDashCaseInsensitive_ReturnsCorrectVersion()
        {
            var version = GitVersion.Parse("2.30.1-RC2");

            Assert.Equal(2, version.Major);
            Assert.Equal(30, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Equal(2, version.ReleaseCandidate);
        }

        [Theory]
        [InlineData("2.30.1.rc", "rc")]
        [InlineData("2.30.1.rc.1", "rc")]
        [InlineData("2.30.1.rcabc", "rcabc")]
        [InlineData("2.30.1.rc-1", "rc-1")]
        public void GitVersion_Parse_InvalidReleaseCandidateDotFormats_ParsedAsDistributionId(string ver, string rcComponent)
        {
            bool result = GitVersion.TryParse(ver, out GitVersion? version);

            Assert.True(result);
            Assert.NotNull(version);
            Assert.Equal(2, version.Major);
            Assert.Equal(30, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Null(version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.Unknown, version.Distribution);
            Assert.Equal(rcComponent, version.DistributionIdentifier);
        }

        [Fact]
        public void GitVersion_TryParse_ValidReleaseCandidateDotVersion_ReturnsTrueAndCorrectVersion()
        {
            bool result = GitVersion.TryParse("2.30.1.rc3", out var version);

            Assert.True(result);
            Assert.NotNull(version);
            Assert.Equal(2, version.Major);
            Assert.Equal(30, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Equal(3, version.ReleaseCandidate);
        }

        [Fact]
        public void GitVersion_TryParse_ValidReleaseCandidateDashVersion_ReturnsTrueAndCorrectVersion()
        {
            bool result = GitVersion.TryParse("2.30.1-rc3", out var version);

            Assert.True(result);
            Assert.NotNull(version);
            Assert.Equal(2, version.Major);
            Assert.Equal(30, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Equal(3, version.ReleaseCandidate);
        }

        [Fact]
        public void GitVersion_TryParse_ValidReleaseCandidateDotAndDashVersion_ReturnsTrueAndCorrectVersion()
        {
            bool result = GitVersion.TryParse("v2.30.1-rc3.rc0", out var version);

            Assert.True(result);
            Assert.NotNull(version);
            Assert.Equal(2, version.Major);
            Assert.Equal(30, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Equal(3, version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.Unknown, version.Distribution);
            Assert.Equal("rc0", version.DistributionIdentifier);
        }

        [Fact]
        public void GitVersion_TryParse_AppleGitVersion_ReturnsTrueAndCorrectVersion()
        {
            bool result = GitVersion.TryParse("2.50.1 (Apple Git-155)", out var version);

            Assert.True(result);
            Assert.NotNull(version);
            Assert.Equal(2, version.Major);
            Assert.Equal(50, version.Minor);
            Assert.Equal(1, version.Patch);
            Assert.Null(version.ReleaseCandidate);
            Assert.Equal(GitDistributionType.Apple, version.Distribution);
            Assert.Null(version.DistributionIdentifier);
            Assert.Equal(155, version.Build);
            Assert.Null(version.Revision);
        }

        [Theory]
        [InlineData("2.30.1", "2.30.1", "v2.30.1")]
        [InlineData("v2.30.1-rc2", "2.30.1.rc2", "v2.30.1-rc2")]
        [InlineData("2.30.1.windows.1", "2.30.1.windows.1", "v2.30.1.windows.1")]
        [InlineData("v2.30.1-rc2.windows.1", "2.30.1.rc2.windows.1", "v2.30.1-rc2.windows.1")]
        public void GitVersion_ToString_FormatsCorrectlyForAllStyles(string input, string expectedBuildNumber, string expectedTag)
        {
            var version = GitVersion.Parse(input);
            Assert.Equal(expectedBuildNumber, version.ToString(GitVersionFormat.BuildNumber));
            Assert.Equal(expectedTag, version.ToString(GitVersionFormat.Tag));
        }

        [Theory]
        [InlineData("2.30.1", "2.30.1")]
        [InlineData("v2.30.1-rc2", "2.30.1.rc2")]
        [InlineData("2.30.1.windows.1", "2.30.1.windows.1")]
        [InlineData("v2.30.1-rc2.windows.1", "2.30.1.rc2.windows.1")]
        public void GitVersion_ToString_DefaultOverload_UsesBuildNumberFormat(string input, string expected)
        {
            var version = GitVersion.Parse(input);
            Assert.Equal(expected, version.ToString());
        }
    }
}
