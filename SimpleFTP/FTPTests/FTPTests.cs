namespace Tests;

using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Server;
using Client;

public class Tests
{
    private const int Port = 8888;

    private const int SizeOfFile = 1024 * 64;
    private const int CountOfNumbersInFile = SizeOfFile / 4;

    private const string DirectoryPath = "testDirectory";
    private readonly string _fileName1 = Path.Combine(DirectoryPath, "file1.txt");
    private readonly string _fileName2 = Path.Combine(DirectoryPath, "file2.txt");

    private readonly string[] _subdirectoriesPaths =
    {
        Path.Combine(DirectoryPath, "testSubdirectory1"),
        Path.Combine(DirectoryPath, "testSubdirectory2"),
        Path.Combine(DirectoryPath, "testSubdirectory3"),
        Path.Combine(DirectoryPath, "testSubdirectory4"),
        Path.Combine(DirectoryPath, "testSubdirectory5"),
    };

    private readonly string[] _subdirectoriesFiles =
    {
        Path.Combine(Path.Combine(DirectoryPath, "testSubdirectory1"), "file1.txt"),
        Path.Combine(Path.Combine(DirectoryPath, "testSubdirectory2"), "file2.txt"),
        Path.Combine(Path.Combine(DirectoryPath, "testSubdirectory3"), "file3.txt"),
        Path.Combine(Path.Combine(DirectoryPath, "testSubdirectory4"), "file4.txt"),
        Path.Combine(Path.Combine(DirectoryPath, "testSubdirectory5"), "file5.txt"),
    };

    private readonly Server _server = new (Port);

    private Task serverTask = new (() => { });

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        serverTask = _server.RunAsync();
        Directory.CreateDirectory(DirectoryPath);

        foreach (var subdirectory in _subdirectoriesPaths)
        {
            Directory.CreateDirectory(subdirectory);
        }

        File.Create(_fileName1);
        File.Create(_fileName2);

        foreach (var file in _subdirectoriesFiles)
        {
            File.Create(file);
        }
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        foreach (var file in _subdirectoriesFiles)
        {
            File.Delete(file);
        }

        foreach (var subdirectory in _subdirectoriesPaths)
        {
            Directory.Delete(subdirectory);
        }

        File.Delete(_fileName1);
        File.Delete(_fileName2);
        Directory.Delete(DirectoryPath);

        _server.Stop();
        await serverTask;
        _server.Dispose();
    }

    [Test]
    public async Task GetShouldWorkProperlyWithCorrectQuery()
    {
        const string sourceFileName = "source.txt";
        CreateFileAndGenerateSomeData(sourceFileName);

        var client = new Client();

        await using var destinationStream = new MemoryStream();
        var size = client.GetAsync("localhost", Port, sourceFileName, destinationStream).Result;

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
        var client = new Client();

        var elements = await client.ListAsync("localhost", Port, DirectoryPath);
        Assert.That(elements.Count, Is.EqualTo(7));
        Assert.That(elements.Contains(("file1.txt", false)));
        Assert.That(elements.Contains(("file2.txt", false)));
        Assert.That(elements.Contains(("testSubdirectory1", true)));
        Assert.That(elements.Contains(("testSubdirectory2", true)));
    }

    [Test]
    public async Task GetShouldReturnNegativeSizeInCaseOfIncorrectInput()
    {
        var client = new Client();

        await using var destinationStream = new MemoryStream();
        var size = client.GetAsync("localhost", Port, Path.Combine(DirectoryPath, "notFile"), destinationStream).Result;
        Assert.That(size, Is.EqualTo(-1L));
    }

    [Test]
    public async Task ServerShouldWorkWithMultipleClients()
    {
        const int numberOfClients = 5;

        var clients = new Client[numberOfClients];

        for (var i = 0; i < numberOfClients; i++)
        {
            clients[i] = new Client();
        }

        var responses = new List<(string, bool)>[numberOfClients];

        for (var i = 0; i < numberOfClients; i++)
        {
            responses[i] = await clients[i].ListAsync("localhost", Port, _subdirectoriesPaths[i]);
        }

        for (var i = 0; i < numberOfClients; i++)
        {
            Assert.That(responses[i].Count, Is.EqualTo(1));
            Assert.That(responses[i].Contains(($"file{i + 1}.txt", false)));
        }
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