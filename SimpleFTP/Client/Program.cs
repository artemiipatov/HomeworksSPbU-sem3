string host = args[0];
int port = int.Parse(args[1]);

using var client = new Client.Client();

while (true)
{
    var query = Console.ReadLine();
    if (query == null)
    {
        break;
    }

    var queryArray = query.Split(" ");
    if (queryArray.Length != 2)
    {
        Console.WriteLine("Invalid query.");
    }

    switch (queryArray[0])
    {
        case "-list":
        {
            var response = await client.ListAsync(host, port, queryArray[1]);
            foreach (var element in response)
            {
                Console.WriteLine(element);
            }

            break;
        }

        case "get":
        {
            var path = string.Empty;

            while (true)
            {
                Console.WriteLine("Path to save:");
                path = Console.ReadLine();

                if (File.Exists(path))
                {
                    Console.WriteLine("File already exists.");
                    continue;
                }

                break;
            }

            var file = new FileStream(path ?? throw new ArgumentNullException(), FileMode.Open);
            var response = await client.GetAsync(host, port, queryArray[1], file);
            Console.WriteLine(response);

            break;
        }
    }
}