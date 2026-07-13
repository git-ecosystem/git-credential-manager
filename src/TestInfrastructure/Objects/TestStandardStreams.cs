using System.IO;
using System.Text;

namespace GitCredentialManager.Tests.Objects
{
    public class TestStandardStreams : IStandardStreams
    {
        public string NewLine { get; set; } = "\n";
        public string In { get; set; } = string.Empty;
        public StringBuilder Out { get; set; } = new StringBuilder();
        public StringBuilder Error { get; set; } = new StringBuilder();
        public bool IsInputRedirected { get; set; } = true;
        public bool IsOutputRedirected { get; set; } = true;
        public bool IsErrorRedirected { get; set; } = true;

        #region IStandardStreams

        TextReader IStandardStreams.In => new StringReader(In);

        TextWriter IStandardStreams.Out => new StringWriter(Out){NewLine = NewLine};

        TextWriter IStandardStreams.Error => new StringWriter(Error){NewLine = NewLine};

        #endregion
    }
}
