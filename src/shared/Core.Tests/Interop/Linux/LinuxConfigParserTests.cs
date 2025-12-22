using System.Collections.Generic;
using GitCredentialManager.Interop.Linux;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests.Interop.Linux;

public class LinuxConfigParserTests
{
    [Fact]
    public void LinuxConfigParser_Parse()
    {
        const string contents =
            """
            #
            # This is a config file complete with comments
            # and empty..

            # lines, as well as lines with..
            #                              
            # only whitespace (like above ^), and..
            invalid lines like this one, not a comment
            # Here's the first real properties:
            core.overrideMe=This is the first config value
            baz.specialChars=I contain special chars like = in my value # this is a comment
            # and let's have with a comment that also contains a = in side
            #
            core.overrideMe=This is the second config value
            bar.scope.foo=123456
            core.overrideMe=This is the correct value
                  ###### comments that start ## with whitespace and extra ## inside
            strings.one="here we have a dq string"
            strings.two='here we have a sq string'
            strings.three=    'here we have another sq string'   # have another sq string
            strings.four="this has 'nested quotes' inside"
            strings.five='mixed "quotes" the other way around'
            strings.six='this has an \'escaped\' set of quotes'
            """;

        var expected = new Dictionary<string, string>
        {
            ["core.overrideMe"] = "This is the correct value",
            ["bar.scope.foo"] = "123456",
            ["baz.specialChars"] = "I contain special chars like = in my value",
            ["strings.one"] = "here we have a dq string",
            ["strings.two"] = "here we have a sq string",
            ["strings.three"] = "here we have another sq string",
            ["strings.four"] = "this has 'nested quotes' inside",
            ["strings.five"] = "mixed \"quotes\" the other way around",
            ["strings.six"] = "this has an \\'escaped\\' set of quotes",
        };

        var parser = new LinuxConfigParser(new NullTrace());

        Assert.Equal(expected, parser.Parse(contents));
    }
}