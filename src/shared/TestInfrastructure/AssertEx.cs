using Xunit;

namespace GitCredentialManager.Tests
{
    public static class AssertEx
    {
        /// <summary>
        /// Requires the fact or theory be marked with the <see cref="SkippableFactAttribute"/>
        /// or <see cref="SkippableTheoryAttribute"/>.
        /// </summary>
        /// <param name="reason">Reason the test has been skipped.</param>
        public static void Skip(string reason)
        {
            Xunit.Skip.If(true, reason);
        }
    }
}
