using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

public class GameClient : IDisposable
{
    private readonly GameServer server;
    private readonly TcpClient client;
    private readonly StreamReader reader;
    private readonly StreamWriter writer;

    private readonly ConcurrentQueue<string> toWrite
        = new ConcurrentQueue<string>();

    public GameClient(GameServer server, TcpClient client)
    {
        this.server = server;
        this.client = client;

        reader = new StreamReader(this.client.GetStream());
        writer = new StreamWriter(this.client.GetStream());

        BeginReceive();
        BeginWrite();
    }

    private async void BeginReceive()
    {
        while (client.Connected)
        {
            var msg = "";
            try
            {
                msg = await reader.ReadLineAsync();
                //server.Log(msg);
                HandlePacket(msg);
            }
            catch (Exception exc)
            {
                server.LogError(exc.ToString());
                Disconnect();
                return;
            }
        }

        Disconnect();
    }

    private void Disconnect()
    {
        try
        {
            Dispose();
        }
        catch
        {
        }

        server.OnClientDisconnected(this);
    }

    public bool Connected => client.Connected;

    private async void BeginWrite()
    {
        while (client.Connected)
        {
            try
            {
                if (toWrite.TryDequeue(out var cmd))
                {
                    //server.Log(cmd);
                    await writer.WriteLineAsync(cmd);
                    await writer.FlushAsync();
                }
                else
                {
                    await Task.Delay(10);
                }
            }
            catch (Exception exc)
            {
                server.LogError(exc.ToString());
            }
        }
    }

    public void SendMessage(string playerName, string message) 
    {
        SendCommand(playerName, "message", message);
    }
    public void SendMessage(PlayerController player, string message) 
    {
        SendCommand(player.PlayerName, "message", message);
    }
    public void SendMessage(Player player, string message)
    {
        SendCommand(player.Username, "message", message);
    }

    public void SendCommand(string playerName, string command, params string[] args)
    {
        var arguments = "";
        if (args.Length > 0)
        {
            arguments = "|" + string.Join("|", args);
        }
        Write(playerName + ":" + command + arguments);
    }

    public void Send(string playerName, string rawCommand)
    {
        Write(playerName + ":" + rawCommand);
    }

    public void Send(string correlationId, string playerName, string rawCommand)
    {
        Write(correlationId + "|" + playerName + ":" + rawCommand);
    }

    public void Write(string cmd)
    {
        toWrite.Enqueue(cmd);
    }

    private void HandlePacket(string cmd)
    {
        server.DataReceived(this, cmd);
    }

    public void Dispose()
    {
        try
        {
            client?.Dispose();
            reader?.Dispose();
            writer?.Dispose();
        }
        catch { }
    }
}