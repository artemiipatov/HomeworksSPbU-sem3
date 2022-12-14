using WebChat;

switch (args.Length)
{
    case 1:
    {
        await StartServer(args);
        break;
    }
        
    case 2:
    {
        await StartClient(args);
        break;
    }

    default:
    {
        throw new ArgumentException();
    }
}

async Task StartClient(string[] args)
{
    if (!int.TryParse(args[1], out var port))
    {
        throw new ArgumentException();
    }

    using var client = new Client(args[0], port);
    await client.RunAsync();
}

async Task StartServer(string[] args)
{
    if (!int.TryParse(args[0], out var port))
    {
        throw new ArgumentException();
    }

    using var server = new Server(port);
    await server.RunAsync();
}