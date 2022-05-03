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

    const int MAX_MESSAGE_LENGTH = 512;
    const int READ_TIMEOUT_SECONDS = 180;

    // Bad example: Do not use in real networking code because this is prone to DOS attacks
    /*
    static async Task ProcessClientBad(TcpClient client)
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
    */

    // Better example: Checks for maximum message length and implements a timeout
    static async Task ProcessClient(TcpClient client)
    {
        StreamWriter? writer = null;

        try {
            NetworkStream stream = client.GetStream();
            writer = new StreamWriter(stream, Encoding.UTF8)
            {
                AutoFlush = true,
                NewLine = "\r\n"
            };

            await writer.WriteLineAsync($"Simple echo server: Maximum message length is {MAX_MESSAGE_LENGTH} " +
                                        "characters, exit with 'quit'...");
            var reader = new StreamReader(stream, Encoding.UTF8);
            var sb = new StringBuilder();

            var buffer = new char[64];
            int charsRead;
            while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length, READ_TIMEOUT_SECONDS * 1000)) > 0) {
                for (int i = 0; i < charsRead; i++) {
                    char c = buffer[i];
                    if (c == '\n') {
                        string message = sb.ToString();
                        sb.Clear();

                        if (!await ProcessMessage(message, writer)) {
                            return;
                        }
                    } else if (sb.Length > MAX_MESSAGE_LENGTH) {
                        await writer.WriteLineAsync("Maximum message length exceeded: Closing connection.");
                        return;
                    } else if (!char.IsControl(c)) {
                        sb.Append(c);
                    }
                }
            }
        } catch (TimeoutException) {
            if (writer != null) {
                await writer.WriteLineAsync("Session timed out: Closing connection.");
            }
            return;
        } catch (Exception e) {
            Console.WriteLine($"Unhandled exception {e.GetType().FullName}: {e.Message}");
            Console.WriteLine(e.StackTrace);
            throw;
        } finally {
            client.Close();
        }
    }

    static async Task<bool> ProcessMessage(string message, StreamWriter writer)
    {
        if (string.IsNullOrEmpty(message)) {
           return true;
        }

        if (message == "quit") {
            await writer.WriteLineAsync("Closing connection.");
            return false;
        }

        await writer.WriteLineAsync($"Your message was: {message}");
        return true;
    }

    public static void Main(string[] args)
    {
        var ip = IPAddress.Parse(LISTEN_ADDRESS);
        var server = new TcpListener(ip, LISTEN_PORT);
        server.Start();

        Console.WriteLine($"EchoServer: Listening for incoming connections on {LISTEN_ADDRESS}:{LISTEN_PORT}");
        while (true) {
            TcpClient client = server.AcceptTcpClient();
            // No await here!
            ProcessClient(client);
        }
    }
}
