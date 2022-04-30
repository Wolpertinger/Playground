using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// A simple echo server that uses async/await instead of threads to handle multiple concurrent connections
/// </summary>
class AsyncEchoServer
{
    const string LISTEN_ADDRESS = "127.0.0.1";
    const int LISTEN_PORT = 8000;

    static async Task ProcessRequestAsync(TcpClient client)
    {
        try {
            NetworkStream stream = client.GetStream();
            var writer = new StreamWriter(stream, Encoding.ASCII);
            writer.AutoFlush = true;
            await writer.WriteLineAsync("Simple echo server, exit with 'quit'...");
            
            var reader = new StreamReader(stream, Encoding.ASCII);
            while (true) {
                string? message = await reader.ReadLineAsync();
                if (message == "quit") {
                    await writer.WriteLineAsync("Closing connection");
                    break;
                }
                await writer.WriteLineAsync($"Your message was: {message}");
            }
        } finally {
            client.Close();
        }
    }

    public static void Main(string[] args)
    {
        var ip = IPAddress.Parse(LISTEN_ADDRESS);
        var server = new TcpListener(ip, LISTEN_PORT);
        server.Start();

        Console.WriteLine($"EchoServer: Listening for incoming connections on {LISTEN_ADDRESS}:{LISTEN_PORT}...");
        while (true) {
            TcpClient client = server.AcceptTcpClient();
            // No await here!
            ProcessRequestAsync(client);
        }
    }
}
