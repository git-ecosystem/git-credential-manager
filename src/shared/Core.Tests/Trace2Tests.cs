using System.Text.RegularExpressions;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests;

public class Trace2Tests
{
    [Theory]
    [InlineData("20190408T191610.507018Z-H9b68c35f-P000059a8")]
    [InlineData("")]
    public void SetSid_Envar_Returns_Expected_Value(string parentSid)
    {
        Regex rx = new Regex(@$"{parentSid}\/[\d\w-]*");

        var environment = new TestEnvironment();
        environment.Variables.Add("GIT_TRACE2_PARENT_SID", parentSid);

        var trace2 = new Trace2(environment);
        var sid = trace2.SetSid();

        Assert.Matches(rx, sid);
    }
}
