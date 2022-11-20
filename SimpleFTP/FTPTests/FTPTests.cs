namespace Tests;

using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using Server;
using Client;

public class Tests
{
    private const int SizeOfFile = 1024 * 64;
    private const int CountOfNumbersInFile = SizeOfFile / 4;

    private const string DirectoryPath = "testDirectory";
    private readonly string _subdirectoryPath1 = Path.Combine(DirectoryPath, "testSubdirectory1");
    private readonly string _subdirectoryPath2 = Path.Combine(DirectoryPath, "testSubdirectory2");
    private readonly string _fileName1 = Path.Combine(DirectoryPath, "file1.txt");
    private readonly string _fileName2 = Path.Combine(DirectoryPath, "file2.txt");

    private const int Port = 8888;
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
        var size = await client.GetAsync(sourceFileName, destinationStream);

        await using var sourceStream = File.Open(sourceFileName, FileMode.Open);
        destinationStream.Seek(0, SeekOrigin.Begin);
        while (true)
        {
            var expectedByte = sourceStream.ReadByte();
            var actualByte = destinationStream.ReadByte();
            Assert.AreEqual(expectedByte, actualByte);
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
        Assert.AreEqual(count, 4);
        Assert.AreEqual(elements[0], ("file1.txt", false));
        Assert.AreEqual(elements[1], ("file2.txt", false));
        Assert.AreEqual(elements[2], ("testSubdirectory1", true));
        Assert.AreEqual(elements[3], ("testSubdirectory2", true));
    }

    [Test]
    public async Task GetShouldReturnNegativeSizeInCaseOfIncorrectInput()
    {
        using var client = new Client(Port, "localhost");

        await using var destinationStream = new MemoryStream();
        var size = await client.GetAsync(Path.Combine(DirectoryPath, "notFile"), destinationStream);
        Assert.AreEqual(-1L, size);
    }

    [Test]
    public async Task ListShouldReturnNegativeValueInCaseOfIncorrectInput()
    {
        using var client = new Client(Port, "localhost");

        var size = (await client.ListAsync(Path.Combine(DirectoryPath, @"nonExistentPath"))).Item1;
        Assert.AreEqual(-1, size);
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
}