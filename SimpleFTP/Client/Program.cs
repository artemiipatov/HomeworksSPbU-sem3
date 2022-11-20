using var client = new Client.Client(8888, "localhost");

var elementsInDirectory = await client.ListAsync("../../../");

Console.WriteLine(elementsInDirectory.Item1);
foreach (var element in elementsInDirectory.Item2)
{
    Console.WriteLine(element);
}