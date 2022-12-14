namespace WebChat;

using System.Net.Sockets;

/// <summary>
/// Class for processing get and post queries.
/// </summary>
public static class Processing
{
    /// <summary>
    /// Processes get and post queries.
    /// </summary>
    /// <param name="stream">Working stream.</param>
    public static async Task ProcessQuery(NetworkStream stream)
    {
        await Task.Run(async () => await Post(stream));
        await Task.Run(async () => await Get(stream));
    }
    
    private static async Task Post(NetworkStream stream)
    {
        while (true)
        {
            var writer = new StreamWriter(stream);
            var message = Console.ReadLine();
            await writer.WriteAsync(message);
            await writer.FlushAsync();
            await stream.FlushAsync();

            writer = null;
        }
    }

    private static async Task Get(NetworkStream stream)
    {
        while (true)
        {
            var reader = new StreamReader(stream);
            var message = await reader.ReadLineAsync();
            Console.WriteLine(message);
            
            reader = null;
        }
    }
}
