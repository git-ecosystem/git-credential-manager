using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GitCredentialManager.Tests;

public class GitStreamReaderTests
{
    #region ReadLineAsync

    [Fact]
    public async Task GitStreamReader_ReadLineAsync_LF()
    {
        // hello\n
        // world\n

        byte[] buffer = Encoding.UTF8.GetBytes("hello\nworld\n");
        using var stream = new MemoryStream(buffer);
        var reader = new GitStreamReader(stream, Encoding.UTF8);

        string actual1 = await reader.ReadLineAsync();
        string actual2 = await reader.ReadLineAsync();
        string actual3 = await reader.ReadLineAsync();

        Assert.Equal("hello", actual1);
        Assert.Equal("world", actual2);
        Assert.Null(actual3);
    }

    [Fact]
    public async Task GitStreamReader_ReadLineAsync_CR()
    {
        // hello\rworld\r

        byte[] buffer = Encoding.UTF8.GetBytes("hello\rworld\r");
        using var stream = new MemoryStream(buffer);
        var reader = new GitStreamReader(stream, Encoding.UTF8);

        string actual1 = await reader.ReadLineAsync();
        string actual2 = await reader.ReadLineAsync();

        Assert.Equal("hello\rworld\r", actual1);
        Assert.Null(actual2);
    }

    [Fact]
    public async Task GitStreamReader_ReadLineAsync_CRLF()
    {
        // hello\r\n
        // world\r\n

        byte[] buffer = Encoding.UTF8.GetBytes("hello\r\nworld\r\n");
        using var stream = new MemoryStream(buffer);
        var reader = new GitStreamReader(stream, Encoding.UTF8);

        string actual1 = await reader.ReadLineAsync();
        string actual2 = await reader.ReadLineAsync();
        string actual3 = await reader.ReadLineAsync();

        Assert.Equal("hello", actual1);
        Assert.Equal("world", actual2);
        Assert.Null(actual3);
    }

    [Fact]
    public async Task GitStreamReader_ReadLineAsync_Mixed()
    {
        // hello\r\n
        // world\rthis\n
        // is\n
        // a\n
        // \rmixed\rnewline\r\n
        // \n
        // string\n

        byte[] buffer = Encoding.UTF8.GetBytes("hello\r\nworld\rthis\nis\na\n\rmixed\rnewline\r\n\nstring\n");
        using var stream = new MemoryStream(buffer);
        var reader = new GitStreamReader(stream, Encoding.UTF8);

        string actual1 = await reader.ReadLineAsync();
        string actual2 = await reader.ReadLineAsync();
        string actual3 = await reader.ReadLineAsync();
        string actual4 = await reader.ReadLineAsync();
        string actual5 = await reader.ReadLineAsync();
        string actual6 = await reader.ReadLineAsync();
        string actual7 = await reader.ReadLineAsync();
        string actual8 = await reader.ReadLineAsync();

        Assert.Equal("hello", actual1);
        Assert.Equal("world\rthis", actual2);
        Assert.Equal("is", actual3);
        Assert.Equal("a", actual4);
        Assert.Equal("\rmixed\rnewline", actual5);
        Assert.Equal("", actual6);
        Assert.Equal("string", actual7);
        Assert.Null(actual8);
    }

    #endregion

    #region ReadLine

    [Fact]
    public void GitStreamReader_ReadLine_LF()
    {
        // hello\n
        // world\n

        byte[] buffer = Encoding.UTF8.GetBytes("hello\nworld\n");
        using var stream = new MemoryStream(buffer);
        var reader = new GitStreamReader(stream, Encoding.UTF8);

        string actual1 = reader.ReadLine();
        string actual2 = reader.ReadLine();
        string actual3 = reader.ReadLine();

        Assert.Equal("hello", actual1);
        Assert.Equal("world", actual2);
        Assert.Null(actual3);
    }

    [Fact]
    public void GitStreamReader_ReadLine_CR()
    {
        // hello\rworld\r

        byte[] buffer = Encoding.UTF8.GetBytes("hello\rworld\r");
        using var stream = new MemoryStream(buffer);
        var reader = new GitStreamReader(stream, Encoding.UTF8);

        string actual1 = reader.ReadLine();
        string actual2 = reader.ReadLine();

        Assert.Equal("hello\rworld\r", actual1);
        Assert.Null(actual2);
    }

    [Fact]
    public void GitStreamReader_ReadLine_CRLF()
    {
        // hello\r\n
        // world\r\n

        byte[] buffer = Encoding.UTF8.GetBytes("hello\r\nworld\r\n");
        using var stream = new MemoryStream(buffer);
        var reader = new GitStreamReader(stream, Encoding.UTF8);

        string actual1 = reader.ReadLine();
        string actual2 = reader.ReadLine();
        string actual3 = reader.ReadLine();

        Assert.Equal("hello", actual1);
        Assert.Equal("world", actual2);
        Assert.Null(actual3);
    }

    [Fact]
    public void GitStreamReader_ReadLine_Mixed()
    {
        // hello\r\n
        // world\rthis\n
        // is\n
        // a\n
        // \rmixed\rnewline\r\n
        // \n
        // string\n

        byte[] buffer = Encoding.UTF8.GetBytes("hello\r\nworld\rthis\nis\na\n\rmixed\rnewline\r\n\nstring\n");
        using var stream = new MemoryStream(buffer);
        var reader = new GitStreamReader(stream, Encoding.UTF8);

        string actual1 = reader.ReadLine();
        string actual2 = reader.ReadLine();
        string actual3 = reader.ReadLine();
        string actual4 = reader.ReadLine();
        string actual5 = reader.ReadLine();
        string actual6 = reader.ReadLine();
        string actual7 = reader.ReadLine();
        string actual8 = reader.ReadLine();

        Assert.Equal("hello", actual1);
        Assert.Equal("world\rthis", actual2);
        Assert.Equal("is", actual3);
        Assert.Equal("a", actual4);
        Assert.Equal("\rmixed\rnewline", actual5);
        Assert.Equal("", actual6);
        Assert.Equal("string", actual7);
        Assert.Null(actual8);
    }

    #endregion
}
