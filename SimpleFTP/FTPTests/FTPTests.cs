namespace Tests;

using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Server;
using Client;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "<Pending>")]
public class Tests
{
    private const int Port = 8888;

    private const int SizeOfFile = 1024 * 64;
    private const int CountOfNumbersInFile = SizeOfFile / 4;

    private const string DirectoryPath = "testDirectory";
    private readonly string _subdirectoryPath1 = Path.Combine(DirectoryPath, "testSubdirectory1");
    private readonly string _subdirectoryPath2 = Path.Combine(DirectoryPath, "testSubdirectory2");
    private readonly string _fileName1 = Path.Combine(DirectoryPath, "file1.txt");
    private readonly string _fileName2 = Path.Combine(DirectoryPath, "file2.txt");

    private readonly Server _server = new (Port);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Task.Run(async () => await _server.RunAsync());
        Directory.CreateDirectory(DirectoryPath);
        Directory.CreateDirectory(_subdirectoryPath1);
        Directory.CreateDirectory(_subdirectoryPath2);
        File.Create(_fileName1);
        File.Create(_fileName2);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        File.Delete(_fileName1);
        File.Delete(_fileName2);
        Directory.Delete(_subdirectoryPath1);
        Directory.Delete(_subdirectoryPath2);
        Directory.Delete(DirectoryPath);
        _server.Dispose();
    }

    [Test]
    public async Task GetShouldWorkProperlyWithCorrectQuery()
    {
        const string sourceFileName = "source.txt";
        CreateFileAndGenerateSomeData(sourceFileName);

        using var client = new Client(Port, "localhost");

        await using var destinationStream = new MemoryStream();
        var size = client.GetAsync(sourceFileName, destinationStream).Result;

        await using var sourceStream = OpenWithDelay(sourceFileName);
        destinationStream.Seek(0, SeekOrigin.Begin);
        while (true)
        {
            var expectedByte = sourceStream.ReadByte();
            var actualByte = destinationStream.ReadByte();
            Assert.That(expectedByte, Is.EqualTo(actualByte));
            if (expectedByte == -1)
            {
                break;
            }
        }
    }

    [Test]
    public async Task ListShouldWorkProperlyWithCorrectQuery()
    {
        using var client = new Client(Port, "localhost");

        var (count, elements) = await client.ListAsync(DirectoryPath);
        Assert.That(count, Is.EqualTo(4));
        Assert.That(elements.Contains(("file1.txt", false)));
        Assert.That(elements.Contains(("file2.txt", false)));
        Assert.That(elements.Contains(("testSubdirectory1", true)));
        Assert.That(elements.Contains(("testSubdirectory2", true)));
    }

    [Test]
    public async Task GetShouldReturnNegativeSizeInCaseOfIncorrectInput()
    {
        using var client = new Client(Port, "localhost");

        await using var destinationStream = new MemoryStream();
        var size = client.GetAsync(Path.Combine(DirectoryPath, "notFile"), destinationStream).Result;
        Assert.That(size, Is.EqualTo(-1L));
    }

    [Test]
    public async Task ListShouldReturnNegativeValueInCaseOfIncorrectInput()
    {
        using var client = new Client(Port, "localhost");

        var size = (await client.ListAsync(Path.Combine(DirectoryPath, @"nonExistentPath"))).Item1;
        Assert.That(size, Is.EqualTo(-1));
    }

    private void CreateFileAndGenerateSomeData(string fileName)
    {
        using var writer = new StreamWriter(File.Create(fileName));

        var rnd = new Random();
        for (var i = 0; i < CountOfNumbersInFile; i++)
        {
            writer.Write(rnd.Next());
        }
    }

    private FileStream OpenWithDelay(string sourceFileName)
    {
        while (true)
        {
            try
            {
                var stream = File.OpenRead(sourceFileName);
                return stream;
            }
            catch (IOException)
            {
            }
        }
    }
}