using System.Collections.Generic;
using System.Text;
using GitCredentialManager.Tests.Objects;
using Xunit;

namespace GitCredentialManager.Tests
{
    public class IniFileTests
    {
        [Fact]
        public void IniSectionName_Equality()
        {
            var a1 = new IniSectionName("foo");
            var b1 = new IniSectionName("foo");
            Assert.Equal(a1,b1);
            Assert.Equal(a1.GetHashCode(),b1.GetHashCode());

            var a2 = new IniSectionName("foo");
            var b2 = new IniSectionName("FOO");
            Assert.Equal(a2,b2);
            Assert.Equal(a2.GetHashCode(),b2.GetHashCode());

            var a3 = new IniSectionName("foo", "bar");
            var b3 = new IniSectionName("foo", "BAR");
            Assert.NotEqual(a3,b3);
            Assert.NotEqual(a3.GetHashCode(),b3.GetHashCode());

            var a4 = new IniSectionName("foo", "bar");
            var b4 = new IniSectionName("FOO", "bar");
            Assert.Equal(a4,b4);
            Assert.Equal(a4.GetHashCode(),b4.GetHashCode());
        }

        [Fact]
        public void IniSerializer_Deserialize()
        {
            const string path = "/tmp/test.ini";
            string iniText = @"
[one]
    foo = 123
  [two]
    foo   =   abc
# comment
[two ""subsection name""] # comment [section]
    foo = this is different # comment prop = val

#[notasection]

    [
[bad #section]
recovery tests]
[]
    ]

    [three]
    bar = a
    bar = b
    # comment
    bar = c
    empty =
[TWO]
    foo = hello
    widget = ""Hello, World!""
[four]
[five]
    prop1 = ""this hash # is inside quotes""
    prop2 = ""this hash # is inside quotes"" # this line has two hashes
    prop3 = ""   this dquoted string has three spaces around   ""
    #prop4 = this property has been commented-out
";

            var fs = new TestFileSystem
            {
                Files = { [path] = Encoding.UTF8.GetBytes(iniText) }
            };

            IniFile ini = IniSerializer.Deserialize(fs, path);

            Assert.Equal(6, ini.Sections.Count);

            AssertSection(ini, "one", out IniSection one);
            Assert.Equal(1, one.Properties.Count);
            AssertProperty(one, "foo", "123");

            AssertSection(ini, "two", out IniSection twoA);
            Assert.Equal(3, twoA.Properties.Count);
            AssertProperty(twoA, "foo", "hello");
            AssertProperty(twoA, "widget", "Hello, World!");

            AssertSection(ini, "two", "subsection name", out IniSection twoB);
            Assert.Equal(1, twoB.Properties.Count);
            AssertProperty(twoB, "foo", "this is different");

            AssertSection(ini, "three", out IniSection three);
            Assert.Equal(4, three.Properties.Count);
            AssertMultiProperty(three, "bar", "a", "b", "c");
            AssertProperty(three, "empty", "");

            AssertSection(ini, "four", out IniSection four);
            Assert.Equal(0, four.Properties.Count);

            AssertSection(ini, "five", out IniSection five);
            Assert.Equal(3, five.Properties.Count);
            AssertProperty(five, "prop1", "this hash # is inside quotes");
            AssertProperty(five, "prop2", "this hash # is inside quotes");
            AssertProperty(five, "prop3", "   this dquoted string has three spaces around   ");
        }

        private static void AssertSection(IniFile file, string name, out IniSection section)
        {
            Assert.True(file.TryGetSection(name, out section));
            Assert.Equal(name, section.Name.Name);
            Assert.Null(section.Name.SubName);
        }

        private static void AssertSection(IniFile file, string name, string subName, out IniSection section)
        {
            Assert.True(file.TryGetSection(name, subName, out section));
            Assert.Equal(name, section.Name.Name);
            Assert.Equal(subName, section.Name.SubName);
        }

        private static void AssertProperty(IniSection section, string name, string value)
        {
            Assert.True(section.TryGetProperty(name, out var actualValue));
            Assert.Equal(value, actualValue);
        }

        private static void AssertMultiProperty(IniSection section, string name, params string[] values)
        {
            Assert.True(section.TryGetMultiProperty(name, out IEnumerable<string> actualValues));
            Assert.Equal(values, actualValues);
        }
    }
}
