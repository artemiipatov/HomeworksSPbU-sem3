int port = int.Parse(args[0]);

var server = new Server.Server(port);
Task.Run(async () => await server.RunAsync());

if (Console.ReadKey().Key == ConsoleKey.Enter)
{
    server.Stop();
    server.Dispose();
}
