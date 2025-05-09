// -----------------------------------------------------------------------------
// DeltaTcpLib.cs
// A reusable .NET Standard library for delta-based TCP messaging
// -----------------------------------------------------------------------------
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using RavenNest.Models;
using RavenNest.Models.TcpApi;
using RavenNest.SDK.Endpoints;
using UnityEngine.UIElements;
using Shinobytes.Linq;

namespace Shinobytes.DeltaTcpLib
{
    // -------------------------------------------------------------------------
    // Game state models
    // -------------------------------------------------------------------------

    public class GameStateRequest
    {
        public int PlayerCount;
        public DungeonState Dungeon;
        public RaidState Raid;
    }

    // -------------------------------------------------------------------------
    // Delta data structures
    // -------------------------------------------------------------------------
    public struct SkillDelta
    {
        public byte Index;
        public long Experience;
        public short Level;
    }

    public struct DeltaExperienceUpdate
    {
        public Guid CharacterId;
        public uint DirtyMask;
        public SkillDelta[] Changes;
    }

    public struct CharacterStateDelta
    {
        public Guid CharacterId;
        public uint DirtyMask;

        public short Health;
        public Island Island;
        public Island Destination;
        public CharacterFlags State;
        public int TrainingSkillIndex;
        public string TaskArgument;
        public long ExpPerHour;
        public DateTime EstimatedTimeForLevelUp;
        public short X, Y, Z;
        public int AutoJoinRaidCounter;
        public int AutoJoinDungeonCounter;
        public long AutoJoinRaidCount;
        public long AutoJoinDungeonCount;
        public bool IsAutoResting;
        public int AutoTrainTargetLevel;
        public double? AutoRestTarget;
        public double? AutoRestStart;
        public int? DungeonCombatStyle;
        public int? RaidCombatStyle;
    }

    [Flags]
    public enum CharacterStateFields : uint
    {
        Health = 1 << 0,
        Island = 1 << 1,
        Destination = 1 << 2,
        State = 1 << 3,
        TrainingSkill = 1 << 4,
        TaskArgument = 1 << 5,
        ExpPerHour = 1 << 6,
        LevelUpETA = 1 << 7,
        Position = 1 << 8,    // X, Y, Z grouped together
        AutoJoinRaid = 1 << 9,   // Counter and Count grouped
        AutoJoinDungeon = 1 << 10, // Counter and Count grouped
        IsAutoResting = 1 << 11,
        AutoTrainLevel = 1 << 12,
        AutoRestTarget = 1 << 13,
        AutoRestStart = 1 << 14,
        DungeonStyle = 1 << 15,
        RaidStyle = 1 << 16,
    }
    // -------------------------------------------------------------------------
    // VarInt and Span reader/writer
    // -------------------------------------------------------------------------
    public static class VarInt
    {
        public static int WriteVarUInt(Span<byte> buf, ulong v)
        {
            int i = 0;
            while (v >= 0x80)
            {
                buf[i++] = (byte)(v | 0x80);
                v >>= 7;
            }
            buf[i++] = (byte)v;
            return i;
        }
        public static (ulong, int) ReadVarUInt(ReadOnlySpan<byte> buf)
        {
            ulong r = 0; int s = 0, i = 0; byte b;
            do { b = buf[i]; r |= (ulong)(b & 0x7F) << s; s += 7; i++; } while ((b & 0x80) != 0);
            return (r, i);
        }
    }

    public static class SpanReader
    {
        public static ulong ReadVarUInt(this ReadOnlySpan<byte> span, ref int pos)
        {
            var (v, i) = VarInt.ReadVarUInt(span.Slice(pos));
            pos += i;
            return v;
        }

        public static byte ReadByte(this ReadOnlySpan<byte> span, ref int pos) => span[pos++];

        public static Guid ReadGuid(this ReadOnlySpan<byte> span, ref int pos)
        {
            var g = new Guid(span.Slice(pos, 16));
            pos += 16;
            return g;
        }
        public static uint ReadUInt32BE(this ReadOnlySpan<byte> span, ref int pos)
        {
            var v = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(pos, 4));
            pos += 4;
            return v;
        }
        public static short ReadInt16BE(this ReadOnlySpan<byte> span, ref int pos)
        {
            var v = BinaryPrimitives.ReadInt16BigEndian(span.Slice(pos, 2));
            pos += 2;
            return v;
        }

        public static float ReadFloatBE(this ReadOnlySpan<byte> span, ref int pos)
        {
            var bits = BinaryPrimitives.ReadInt32BigEndian(span.Slice(pos, 4));
            pos += 4;
            return BitConverter.Int32BitsToSingle(bits);
        }

        public static bool ReadBool(this ReadOnlySpan<byte> span, ref int pos) => span[pos++] != 0;

        public static string ReadString(this ReadOnlySpan<byte> span, ref int pos)
        {
            var len = (int)span.ReadVarUInt(ref pos);
            var s = Encoding.UTF8.GetString(span.Slice(pos, len)); pos += len;
            return s;
        }

        public static DateTime ReadDateTime(this ReadOnlySpan<byte> span, ref int pos)
        {
            var ticks = (long)span.ReadVarUInt(ref pos);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        public static Island ReadIsland(this ReadOnlySpan<byte> span, ref int pos) => (Island)span[pos++];
        public static CharacterFlags ReadFlags(this ReadOnlySpan<byte> span, ref int pos)
        {
            var v = BinaryPrimitives.ReadInt32BigEndian(span.Slice(pos, 4));
            pos += 4;
            return (CharacterFlags)v;
        }
    }
    public static class SpanWriter
    {
        public static int Write(this Span<byte> span, Guid g)
        {
            g.ToByteArray().CopyTo(span);
            return 16;
        }
        public static int Write(this Span<byte> span, ulong v) => VarInt.WriteVarUInt(span, v);
        public static int Write(this Span<byte> span, uint v)
        {
            BinaryPrimitives.WriteUInt32BigEndian(span, v);
            return 4;
        }
        public static int Write(this Span<byte> span, short v)
        {
            BinaryPrimitives.WriteInt16BigEndian(span, v);
            return 2;
        }
        public static int Write(this Span<byte> span, float v)
        {
            var b = BitConverter.SingleToInt32Bits(v); BinaryPrimitives.WriteInt32BigEndian(span, b);
            return 4;
        }
        public static int Write(this Span<byte> span, bool v)
        {
            span[0] = (byte)(v ? 1 : 0);
            return 1;
        }
        public static int Write(this Span<byte> span, string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return VarInt.WriteVarUInt(span, (ulong)0);
            }

            var b = Encoding.UTF8.GetBytes(s);
            int p = VarInt.WriteVarUInt(span, (ulong)b.Length);
            b.CopyTo(span.Slice(p));
            return p + b.Length;
        }
        public static int Write(this Span<byte> span, DateTime dt) => VarInt.WriteVarUInt(span, (ulong)dt.Ticks);
        public static int Write(this Span<byte> span, Island i)
        {
            span[0] = (byte)i;
            return 1;
        }
        public static int Write(this Span<byte> span, CharacterFlags f)
        {
            BinaryPrimitives.WriteInt32BigEndian(span, (int)f);
            return 4;
        }
    }

    // -------------------------------------------------------------------------
    // TCP Client
    // -------------------------------------------------------------------------
    public class DeltaClient
    {
        const int AUTH = 1;
        const int EXPERIENCE_UPDATE = 2;
        const int PLAYER_STATE = 3;
        const int GAME_STATE = 4;


        private readonly ITokenProvider tokenProvider;
        private readonly IPEndPoint endpoint;
        private readonly object _sync = new();
        private bool _isConnecting = false;
        private CancellationTokenSource _connectCts;

        private Socket socket;
        private readonly SocketType socketType;
        private readonly ProtocolType protocolType;

        // Updated events
        public event Action<SessionToken> OnConnected;
        public event Action OnDisconnected;
        public event Action<Exception> OnConnectionError;

#if UNITY_EDITOR

        private long _totalBytesSent;
        private long _totalBytesReceived;
        private DateTime _trackingStartTime;
        private readonly Dictionary<string, long> _messageTypeBytesSent;

        public long TotalBytesSent => _totalBytesSent;
        public long TotalBytesReceived => _totalBytesReceived;
        public TimeSpan TrackingDuration => DateTime.UtcNow - _trackingStartTime;

        public float BytesSentPerSecond =>
            (float)_totalBytesSent / (float)Math.Max(1, TrackingDuration.TotalSeconds);

        public Dictionary<string, long> MessageTypeBytesSent =>
            new Dictionary<string, long>(_messageTypeBytesSent);
#endif

        public DeltaClient(string host, int port, ITokenProvider tokenProvider)
        {
            this.tokenProvider = tokenProvider;
            this.socketType = SocketType.Stream;
            this.protocolType = ProtocolType.Tcp;

            if (IPAddress.TryParse(host, out var ip))
            {
                this.endpoint = new IPEndPoint(ip, port);
            }
            else
            {
                var hostEntry = Dns.GetHostEntry(host);
                if (hostEntry.AddressList.Length == 0)
                {
                    throw new ArgumentException($"Unable to resolve host: {host}");
                }

                this.endpoint = new IPEndPoint(hostEntry.AddressList[0], port);
            }
            this.socket = new Socket(socketType, protocolType);

#if UNITY_EDITOR
            _totalBytesSent = 0;
            _totalBytesReceived = 0;
            _trackingStartTime = DateTime.UtcNow;
            _messageTypeBytesSent = new Dictionary<string, long>
            {
                { "Auth", 0 },
                { "Experience", 0 },
                { "PlayerState", 0 },
                { "GameState", 0 }
            };
#endif
        }

        public bool IsConnecting => _isConnecting;
        public bool IsConnected => socket != null && socket.Connected;

#if UNITY_EDITOR
        public string GetStatisticsReport()
        {
            var duration = TrackingDuration.TotalSeconds;
            var sb = new StringBuilder();

            sb.AppendLine("DeltaClient Network Statistics:");
            sb.AppendLine($"Duration: {duration:F1} seconds");
            sb.AppendLine($"Total sent: {FormatByteSize(_totalBytesSent)} ({FormatByteSize((long)BytesSentPerSecond)}/sec)");

            sb.AppendLine("\nBy message type:");
            foreach (var kvp in _messageTypeBytesSent)
            {
                var bytesPerSec = duration > 0 ? kvp.Value / duration : 0;
                sb.AppendLine($"  {kvp.Key}: {FormatByteSize(kvp.Value)} ({FormatByteSize((long)bytesPerSec)}/sec)");
            }

            return sb.ToString();
        }
        // Format byte size to human-readable format
        private string FormatByteSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
        // Reset statistics
        public void ResetStatistics()
        {
            _totalBytesSent = 0;
            _totalBytesReceived = 0;
            _trackingStartTime = DateTime.UtcNow;

            foreach (var key in _messageTypeBytesSent.Keys.ToList())
            {
                _messageTypeBytesSent[key] = 0;
            }
        }

#endif

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected || _isConnecting)
                return;

            try
            {
                _isConnecting = true;
                _connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                if (socket == null || !socket.Connected)
                {
                    // Dispose old socket if it exists
                    if (socket != null)
                    {
                        try
                        {
                            socket.Dispose();
                        }
                        catch { /* Ignore any errors during disposal */ }
                    }

                    // Create a new socket
                    socket = new Socket(socketType, protocolType);
                }

                var sessionToken = tokenProvider.GetSessionToken();
                if (sessionToken == null)
                    throw new InvalidOperationException("Session token is not set.");

                // Use async connection
                await Task.Run(() =>
                {
                    socket.BeginConnect(endpoint, OnSocketConnected, socket);

                    // Wait for connection with timeout
                    var connected = SpinWait.SpinUntil(() => socket.Connected || _connectCts.Token.IsCancellationRequested,
                        TimeSpan.FromSeconds(5));

                    if (_connectCts.Token.IsCancellationRequested)
                        throw new OperationCanceledException();

                    if (!connected)
                        throw new TimeoutException("Connection attempt timed out");

                    if (socket.Connected)
                    {
                        SendFrame(AUTH, sessionToken.ToBytes());
                    }

                }, _connectCts.Token);

                // If we got here without exception, we're connected
                if (socket.Connected)
                {
                    OnConnected?.Invoke(sessionToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Connection was canceled
            }
            catch (Exception ex)
            {
                OnConnectionError?.Invoke(ex);
                throw;
            }
            finally
            {
                _isConnecting = false;
                _connectCts?.Dispose();
                _connectCts = null;
            }
        }

        private void OnSocketConnected(IAsyncResult ar)
        {
            try
            {
                socket.EndConnect(ar);
            }
            catch //(Exception ex)
            {
                //OnConnectionError?.Invoke(ex);
                // ignored
            }
        }

        public void Disconnect()
        {
            // Cancel any pending connection
            _connectCts?.Cancel();

            lock (_sync)
            {
                try
                {
                    if (socket != null)
                    {
                        if (socket.Connected)
                        {
                            try { socket.Shutdown(SocketShutdown.Both); } catch { }
                            try { socket.Close(); } catch { }
                        }

                        try { socket.Dispose(); } catch { }
                        socket = null;
                    }
                }
                catch { }
            }

            OnDisconnected?.Invoke();
        }


        public void SendExperienceDeltas(DeltaExperienceUpdate[] batch, int count)
        {
            //var buf = new byte[count * 64];
            var buf = ArrayPool<byte>.Shared.Rent(count * 64);
            try
            {
                int pos = 0;
                pos += VarInt.WriteVarUInt(buf.AsSpan(pos), (ulong)count);

                for (int i = 0; i < count; i++)
                {
                    var d = batch[i];
                    pos += buf.AsSpan(pos).Write(d.CharacterId);
                    pos += buf.AsSpan(pos).Write(d.DirtyMask);
                    pos += VarInt.WriteVarUInt(buf.AsSpan(pos), (ulong)d.Changes.Length);

                    for (int j = 0; j < d.Changes.Length; j++)
                    {
                        var c = d.Changes[j];
                        buf[pos++] = c.Index;
                        pos += VarInt.WriteVarUInt(buf.AsSpan(pos), (ulong)c.Experience);
                        pos += VarInt.WriteVarUInt(buf.AsSpan(pos), (ulong)c.Level);
                    }
                }

                SendFrame(EXPERIENCE_UPDATE, buf, pos);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf);
            }
            //SendFrame(EXPERIENCE_UPDATE, PackExperience(batch, count));
        }
        public void SendPlayerState(CharacterStateDelta[] states, int count)
        {
            var buf = ArrayPool<byte>.Shared.Rent(count * 256);
            try
            {
                int pos = 0;
                pos += VarInt.WriteVarUInt(buf.AsSpan(pos), (ulong)count);

                for (int i = 0; i < count; i++)
                {
                    var d = states[i];
                    pos += buf.AsSpan(pos).Write(d.CharacterId);
                    pos += buf.AsSpan(pos).Write(d.DirtyMask);  // Write the dirty mask

                    // Only write fields that are dirty (have changed)
                    if ((d.DirtyMask & (uint)CharacterStateFields.Health) != 0)
                        pos += buf.AsSpan(pos).Write(d.Health);

                    if ((d.DirtyMask & (uint)CharacterStateFields.Island) != 0)
                        pos += buf.AsSpan(pos).Write(d.Island);

                    if ((d.DirtyMask & (uint)CharacterStateFields.Destination) != 0)
                        pos += buf.AsSpan(pos).Write(d.Destination);

                    if ((d.DirtyMask & (uint)CharacterStateFields.State) != 0)
                        pos += buf.AsSpan(pos).Write(d.State);

                    if ((d.DirtyMask & (uint)CharacterStateFields.TrainingSkill) != 0)
                        pos += VarInt.WriteVarUInt(buf.AsSpan(pos), (ulong)d.TrainingSkillIndex);

                    if ((d.DirtyMask & (uint)CharacterStateFields.TaskArgument) != 0)
                        pos += buf.AsSpan(pos).Write(d.TaskArgument);

                    if ((d.DirtyMask & (uint)CharacterStateFields.ExpPerHour) != 0)
                        pos += VarInt.WriteVarUInt(buf.AsSpan(pos), (ulong)d.ExpPerHour);

                    if ((d.DirtyMask & (uint)CharacterStateFields.LevelUpETA) != 0)
                        pos += buf.AsSpan(pos).Write(d.EstimatedTimeForLevelUp);

                    if ((d.DirtyMask & (uint)CharacterStateFields.Position) != 0)
                    {
                        pos += buf.AsSpan(pos).Write(d.X);
                        pos += buf.AsSpan(pos).Write(d.Y);
                        pos += buf.AsSpan(pos).Write(d.Z);
                    }

                    if ((d.DirtyMask & (uint)CharacterStateFields.AutoJoinRaid) != 0)
                    {
                        pos += buf.AsSpan(pos).Write(d.AutoJoinRaidCounter);
                        pos += VarInt.WriteVarUInt(buf.AsSpan(pos), (ulong)d.AutoJoinRaidCount);
                    }

                    if ((d.DirtyMask & (uint)CharacterStateFields.AutoJoinDungeon) != 0)
                    {
                        pos += buf.AsSpan(pos).Write(d.AutoJoinDungeonCounter);
                        pos += VarInt.WriteVarUInt(buf.AsSpan(pos), (ulong)d.AutoJoinDungeonCount);
                    }

                    if ((d.DirtyMask & (uint)CharacterStateFields.IsAutoResting) != 0)
                        pos += buf.AsSpan(pos).Write(d.IsAutoResting);

                    if ((d.DirtyMask & (uint)CharacterStateFields.AutoTrainLevel) != 0)
                        pos += buf.AsSpan(pos).Write(d.AutoTrainTargetLevel);

                    if ((d.DirtyMask & (uint)CharacterStateFields.AutoRestTarget) != 0)
                    {
                        buf[pos++] = d.AutoRestTarget.HasValue ? (byte)1 : (byte)0;
                        if (d.AutoRestTarget.HasValue)
                        {
                            BinaryPrimitives.WriteInt64BigEndian(buf.AsSpan(pos),
                                BitConverter.DoubleToInt64Bits(d.AutoRestTarget.Value));
                            pos += 8;
                        }
                    }

                    if ((d.DirtyMask & (uint)CharacterStateFields.AutoRestStart) != 0)
                    {
                        buf[pos++] = d.AutoRestStart.HasValue ? (byte)1 : (byte)0;
                        if (d.AutoRestStart.HasValue)
                        {
                            BinaryPrimitives.WriteInt64BigEndian(buf.AsSpan(pos),
                                BitConverter.DoubleToInt64Bits(d.AutoRestStart.Value));
                            pos += 8;
                        }
                    }

                    if ((d.DirtyMask & (uint)CharacterStateFields.DungeonStyle) != 0)
                    {
                        buf[pos++] = d.DungeonCombatStyle.HasValue ? (byte)1 : (byte)0;
                        if (d.DungeonCombatStyle.HasValue)
                            pos += VarInt.WriteVarUInt(buf.AsSpan(pos), (ulong)d.DungeonCombatStyle.Value);
                    }

                    if ((d.DirtyMask & (uint)CharacterStateFields.RaidStyle) != 0)
                    {
                        buf[pos++] = d.RaidCombatStyle.HasValue ? (byte)1 : (byte)0;
                        if (d.RaidCombatStyle.HasValue)
                            pos += VarInt.WriteVarUInt(buf.AsSpan(pos), (ulong)d.RaidCombatStyle.Value);
                    }
                }

                SendFrame(PLAYER_STATE, buf, pos);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        public void SendGameState(GameStateRequest gs)
        {
            // Use stackalloc for a temporary buffer - 512 bytes is a reasonable size
            Span<byte> buf = stackalloc byte[512];
            int pos = 0;

            pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)gs.PlayerCount);

            // raid
            buf[pos++] = gs.Raid.IsActive ? (byte)1 : (byte)0;
            if (gs.Raid.IsActive)
            {
                pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)gs.Raid.BossCombatLevel);
                pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)gs.Raid.CurrentBossHealth);
                pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)gs.Raid.MaxBossHealth);
                pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)gs.Raid.PlayersJoined);
                pos += SpanWriter.Write(buf.Slice(pos), gs.Raid.EndTime);
            }
            pos += SpanWriter.Write(buf.Slice(pos), gs.Raid.NextRaid);

            // dungeon
            buf[pos++] = gs.Dungeon.IsActive ? (byte)1 : (byte)0;
            if (gs.Dungeon.IsActive)
            {
                // Handle dungeon name (potentially variable length)
                var hasDungeonName = !string.IsNullOrEmpty(gs.Dungeon.Name);

                if (hasDungeonName)
                {
                    var dungeonName = Encoding.UTF8.GetBytes(gs.Dungeon.Name);
                    pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)dungeonName.Length);
                    dungeonName.CopyTo(buf.Slice(pos));
                    pos += dungeonName.Length;
                }
                else
                {
                    pos += VarInt.WriteVarUInt(buf.Slice(pos), 0);
                }

                buf[pos++] = gs.Dungeon.HasStarted ? (byte)1 : (byte)0;
                pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)gs.Dungeon.BossCombatLevel);
                pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)gs.Dungeon.CurrentBossHealth);
                pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)gs.Dungeon.MaxBossHealth);
                pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)gs.Dungeon.PlayersAlive);
                pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)gs.Dungeon.PlayersJoined);
                pos += VarInt.WriteVarUInt(buf.Slice(pos), (ulong)gs.Dungeon.EnemiesLeft);
                pos += SpanWriter.Write(buf.Slice(pos), gs.Dungeon.StartTime);
            }

            pos += SpanWriter.Write(buf.Slice(pos), gs.Dungeon.NextDungeon);

            SendFrame(GAME_STATE, buf, pos);
        }

        private void SendFrame(byte type, Span<byte> buf, int length)
        {
            if (length == 0)
            {
                return;
            }

            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                buf.Slice(0, length).CopyTo(rentedBuffer);

                SendFrame(type, rentedBuffer, length);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Error sending frame: " + exc.ToString());
            }
            finally
            {

                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

        private void SendFrame(byte type, byte[] payload)
        {
            // Call the other SendFrame method to avoid code duplication
            SendFrame(type, payload, payload.Length);
        }

        private void SendFrame(byte type, byte[] buf, int length)
        {
            if (length == 0)
            {
                return;
            }

            try
            {
                int len = 1 + length;
                Span<byte> hdr = stackalloc byte[5];
                BinaryPrimitives.WriteInt32BigEndian(hdr, len);
                hdr[4] = type;
                socket.Send(hdr);
                socket.Send(buf, 0, length, SocketFlags.None);

#if UNITY_EDITOR

            if (UnityEngine.Application.isEditor)
            {
                // Track bytes sent (header + payload)
                int totalBytes = 5 + length; // 5-byte header + payload length
                _totalBytesSent += totalBytes;

                // Track by message type
                string messageType = GetMessageTypeName(type);
                if (_messageTypeBytesSent.ContainsKey(messageType))
                {
                    _messageTypeBytesSent[messageType] += totalBytes;
                }

                // Optional: Log every X seconds for real-time monitoring
                LogPeriodicStatistics();
            }
#endif
            }
            catch (SocketException se)
            {
                Disconnect();
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("Error sending frame: " + exc.ToString());
            }
        }


        // Helper to convert type code to string name
        private string GetMessageTypeName(byte type)
        {
            switch (type)
            {
                case AUTH: return "Auth";
                case EXPERIENCE_UPDATE: return "Experience";
                case PLAYER_STATE: return "PlayerState";
                case GAME_STATE: return "GameState";
                default: return $"Unknown({type})";
            }
        }

#if UNITY_EDITOR
        // For periodic logging in Editor
        private DateTime _lastLogTime = DateTime.MinValue;
        private void LogPeriodicStatistics()
        {
            // Log statistics every 10 seconds
            if ((DateTime.UtcNow - _lastLogTime).TotalSeconds >= 10)
            {
                _lastLogTime = DateTime.UtcNow;
                UnityEngine.Debug.Log(GetStatisticsReport());
            }
        }
#endif
    }
}