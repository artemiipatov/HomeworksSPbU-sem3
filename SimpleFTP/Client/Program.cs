using System.Net.Sockets;

const int port = 8888;
using var client = new TcpClient("localhost", port);

// Console.WriteLine($"Sending \"Hello!\" to port {port}...");
var stream = client.GetStream();
var writer = new StreamWriter(stream);
while (true)
{
    var query = Console.ReadLine();
    writer.WriteLine(query);
    writer.Flush();
    if (query == "3")
    {
        break;
    }

    var sizeInBytes = new byte[8];
    await stream.ReadAsync(sizeInBytes.AsMemory(0, 8));
    var size = BitConverter.ToInt64(sizeInBytes);
    Console.WriteLine(size);
    var fileInBytes = new byte[size];
    await stream.ReadAsync(fileInBytes.AsMemory(0, (int)size));
    await using var newFile = new BinaryWriter(File.Open("someData.txt", FileMode.Create));
    newFile.Write(fileInBytes);
}
// var reader = new BinaryReader(stream);
// var size = reader.ReadInt64();
// var newFile = new BinaryWriter(File.Open("newFile.txt", FileMode.Create));
