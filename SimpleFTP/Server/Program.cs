int port = int.Parse(args[0]);

using var server = new Server.Server(port);
await server.RunAsync();