using System;
using GitCredentialManager.Interop.Windows;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests.Interop.Windows
{
    public class WindowsSystemPromptsTests
    {
        [Fact]
        public void WindowsSystemPrompts_ShowCredentialPrompt_NullResource_ThrowsException()
        {
            var sysPrompts = new WindowsSystemPrompts();
            Assert.Throws<ArgumentNullException>(() => sysPrompts.ShowCredentialPrompt(null, null, out _));
        }

        [Fact]
        public void WindowsSystemPrompts_ShowCredentialPrompt_EmptyResource_ThrowsException()
        {
            var sysPrompts = new WindowsSystemPrompts();
            Assert.Throws<ArgumentException>(() => sysPrompts.ShowCredentialPrompt(string.Empty, null, out _));
        }

        [Fact]
        public void WindowsSystemPrompts_ShowCredentialPrompt_WhiteSpaceResource_ThrowsException()
        {
            var sysPrompts = new WindowsSystemPrompts();
            Assert.Throws<ArgumentException>(() => sysPrompts.ShowCredentialPrompt("   ", null, out _));
        }
    }
}
