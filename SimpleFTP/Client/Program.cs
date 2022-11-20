using var client = new Client.Client(8888, "localhost");

await using var fileStream = new FileStream("data1.pdf", FileMode.Create);
await client.GetAsync("Shurygin._.Analiticheskaya.geometriya.II.pdf", fileStream);

var elementsInDirectory = await client.ListAsync("../../../");

Console.WriteLine(elementsInDirectory.Item1);
foreach (var element in elementsInDirectory.Item2)
{
    Console.WriteLine(element);
}