// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.Git.CredentialManager.Tests
{
    public class IniSerializerTests
    {
        [Fact]
        public void IniSerializer_Deserialize()
        {
            const string iniText = "[foo \"this:\\ is-contoso/].com\"]\n" +
                                   "\tuser = john.doe\n" +
                                   "\n" +
                                   "[bar]\n" +
                                   "\tuser = jane.doe\n" +
                                   "\n" +
                                   "[misc]\n" +
                                   "\tnull =\n" +
                                   "\tempty =\n" +
                                   "\tvalue = foo\n" +
                                   "\n";

            var serializer = new IniSerializer();
            using (var reader = new StringReader(iniText))
            {
                IniFile iniFile = serializer.Deserialize(reader);

                Assert.Equal(3, iniFile.Sections.Count);

                Assert.Equal("foo", iniFile.Sections[0].Name);
                Assert.Equal("this:\\ is-contoso/].com", iniFile.Sections[0].Scope);
                Assert.Equal(1, iniFile.Sections[0].Properties.Count);
                Assert.True(iniFile.Sections[0].Properties.ContainsKey("user"));
                Assert.Equal("john.doe", iniFile.Sections[0].Properties["user"]);

                Assert.Equal("bar", iniFile.Sections[1].Name);
                Assert.Null(iniFile.Sections[1].Scope);
                Assert.Equal(1, iniFile.Sections[1].Properties.Count);
                Assert.True(iniFile.Sections[1].Properties.ContainsKey("user"));
                Assert.Equal("jane.doe", iniFile.Sections[1].Properties["user"]);

                Assert.Equal("misc", iniFile.Sections[2].Name);
                Assert.Null(iniFile.Sections[2].Scope);
                Assert.Equal(3, iniFile.Sections[2].Properties.Count);
                Assert.True(iniFile.Sections[2].Properties.ContainsKey("null"));
                Assert.True(iniFile.Sections[2].Properties.ContainsKey("empty"));
                Assert.True(iniFile.Sections[2].Properties.ContainsKey("value"));
                Assert.Null(iniFile.Sections[2].Properties["null"]);
                Assert.Null(iniFile.Sections[2].Properties["empty"]);
                Assert.Equal("foo", iniFile.Sections[2].Properties["value"]);
            }
        }

        [Fact]
        public void IniSerializer_Serialize()
        {
            const string expectedIniText = "[foo \"this:\\ is-contoso/].com\"]\n" +
                                           "\tuser = john.doe\n" +
                                           "\n" +
                                           "[bar]\n" +
                                           "\tuser = jane.doe\n" +
                                           "\n" +
                                           "[misc]\n" +
                                           "\tnull =\n" +
                                           "\tempty =\n" +
                                           "\tvalue = foo\n" +
                                           "\n";

            var iniFile = new IniFile
            {
                Sections =
                {
                    new IniSection("foo", "this:\\ is-contoso/].com")
                    {
                        Properties = {["user"] = "john.doe"}
                    },
                    new IniSection("bar", null)
                    {
                        Properties = {["user"] = "jane.doe"}
                    },
                    new IniSection("misc", null)
                    {
                        Properties =
                        {
                            ["null"] = null,
                            ["empty"] = "",
                            ["value"] = "foo",
                        },
                    },
                }
            };

            var serializer = new IniSerializer();

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb) {NewLine = "\n"})
            {
                serializer.Serialize(iniFile, writer);
            }

            string actualIniText = sb.ToString();

            Assert.Equal(expectedIniText, actualIniText);
        }
    }
}
