int port = int.Parse(args[0]);

var server = new Server.Server(port);
var serverTask = server.RunAsync();

if (Console.ReadKey().Key == ConsoleKey.Enter)
{
    server.Stop();
    await serverTask;
    server.Dispose();
}
