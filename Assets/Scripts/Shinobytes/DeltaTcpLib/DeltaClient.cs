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
using Cysharp.Threading.Tasks;
using System.Runtime.CompilerServices;

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

        public string PlatformUserId;
        public string PlatformUserName;
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

        Platform = 1 << 17,
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
            var b = BitConverter.SingleToInt32Bits(v);
            BinaryPrimitives.WriteInt32BigEndian(span, b);
            return 4;
        }

        public static int Write(this Span<byte> span, bool v)
        {
            span[0] = (byte)(v ? 1 : 0);
            return 1;
        }
        public static int WriteShortString(this Span<byte> span, string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                span[0] = 0;
                return 1;
            }

            var b = Encoding.UTF8.GetBytes(s);
            if (b.Length > 255)
            {
                throw new ArgumentException("String is too long for short string format.");
            }
            span[0] = (byte)b.Length;
            b.CopyTo(span.Slice(1));
            return 1 + b.Length;
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
        //private readonly object socketLock = new();
        private bool _isConnecting = false;
        private CancellationTokenSource _connectCts;
        private Socket socket;
        private readonly SocketType socketType;
        private readonly ProtocolType protocolType;

#if UNITY_EDITOR
        private long _totalBytesSent;
        private long _totalBytesReceived;
        private DateTime _trackingStartTime;
        private readonly Dictionary<string, long> _messageTypeBytesSent;
        public long TotalBytesSent => _totalBytesSent;
        public long TotalBytesReceived => _totalBytesReceived;
        public TimeSpan TrackingDuration => DateTime.UtcNow - _trackingStartTime;
        public float BytesSentPerSecond => (float)_totalBytesSent / (float)Math.Max(1, TrackingDuration.TotalSeconds);
        public Dictionary<string, long> MessageTypeBytesSent => new Dictionary<string, long>(_messageTypeBytesSent);
        private DateTime _lastLogTime = DateTime.MinValue;
#endif

        // --- Buffer size safety (conservative estimate, tweak as needed) ---
        public const int MaxPlayerStateSize = 512;
        public const int MaxExpUpdateSize = 128;
        public const int MaxGameStateSize = 512;

        public event Action<SessionToken> OnConnected;
        public event Action OnDisconnected;
        public event Action<Exception> OnConnectionError;

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
        public void ResetStatistics()
        {
            _totalBytesSent = 0;
            _totalBytesReceived = 0;
            _trackingStartTime = DateTime.UtcNow;

            foreach (var key in _messageTypeBytesSent.Keys.ToList())
                _messageTypeBytesSent[key] = 0;
        }
#endif

        public async UniTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected || _isConnecting)
                return;

            try
            {
                _isConnecting = true;
                _connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                if (socket == null || !socket.Connected)
                {
                    try { socket?.Dispose(); } catch { }
                    socket = new Socket(socketType, protocolType);
                }

                var sessionToken = tokenProvider.GetSessionToken();
                if (sessionToken == null)
                    throw new InvalidOperationException("Session token is not set.");

                await UniTask.RunOnThreadPool(async () =>
                {
                    socket.BeginConnect(endpoint, OnSocketConnected, socket);
                    var connected = SpinWait.SpinUntil(() => socket.Connected || _connectCts.Token.IsCancellationRequested, TimeSpan.FromSeconds(5));

                    if (_connectCts.Token.IsCancellationRequested)
                        throw new OperationCanceledException();

                    if (!connected)
                        throw new TimeoutException("Connection attempt timed out");

                    if (socket.Connected)
                        await SendFrameAsync(AUTH, sessionToken.ToBytes());
                }, cancellationToken: _connectCts.Token);

                if (socket.Connected)
                    OnConnected?.Invoke(sessionToken);
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
            try { socket.EndConnect(ar); }
            catch { /* Ignore connect errors here, handled in ConnectAsync */ }
        }

        public void Disconnect()
        {
            _connectCts?.Cancel();

            //lock (socketLock)
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

        // --------- Async batch sending methods ----------

        public static int Serialize(DeltaExperienceUpdate[] batch, byte[] output, int count)
        {
            int pos = 0;
            pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)count);

            for (int i = 0; i < count; i++)
            {
                var d = batch[i];
                pos += output.AsSpan(pos).Write(d.CharacterId);
                pos += output.AsSpan(pos).Write(d.DirtyMask);
                pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)d.Changes.Length);

                for (int j = 0; j < d.Changes.Length; j++)
                {
                    var c = d.Changes[j];
                    output[pos++] = c.Index;
                    pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)c.Experience);
                    pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)c.Level);
                }
            }

            return pos;
        }


        public static int Serialize(CharacterStateDelta[] states, byte[] output, int count)
        {
            int pos = 0;
            pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)count);

            for (int i = 0; i < count; i++)
            {
                var d = states[i];
                pos += output.AsSpan(pos).Write(d.CharacterId);
                pos += output.AsSpan(pos).Write(d.DirtyMask);

                // Only write fields that are dirty (have changed)
                if ((d.DirtyMask & (uint)CharacterStateFields.Health) != 0)
                    pos += output.AsSpan(pos).Write(d.Health);
                if ((d.DirtyMask & (uint)CharacterStateFields.Island) != 0)
                    pos += output.AsSpan(pos).Write(d.Island);
                if ((d.DirtyMask & (uint)CharacterStateFields.Destination) != 0)
                    pos += output.AsSpan(pos).Write(d.Destination);
                if ((d.DirtyMask & (uint)CharacterStateFields.State) != 0)
                    pos += output.AsSpan(pos).Write(d.State);
                if ((d.DirtyMask & (uint)CharacterStateFields.TrainingSkill) != 0)
                    pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)d.TrainingSkillIndex);
                if ((d.DirtyMask & (uint)CharacterStateFields.TaskArgument) != 0)
                    pos += output.AsSpan(pos).WriteShortString(d.TaskArgument);
                if ((d.DirtyMask & (uint)CharacterStateFields.ExpPerHour) != 0)
                    pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)d.ExpPerHour);
                if ((d.DirtyMask & (uint)CharacterStateFields.LevelUpETA) != 0)
                    pos += output.AsSpan(pos).Write(d.EstimatedTimeForLevelUp);
                if ((d.DirtyMask & (uint)CharacterStateFields.Position) != 0)
                {
                    pos += output.AsSpan(pos).Write(d.X);
                    pos += output.AsSpan(pos).Write(d.Y);
                    pos += output.AsSpan(pos).Write(d.Z);
                }
                if ((d.DirtyMask & (uint)CharacterStateFields.AutoJoinRaid) != 0)
                {
                    pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)d.AutoJoinRaidCounter);
                    pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)d.AutoJoinRaidCount);
                }
                if ((d.DirtyMask & (uint)CharacterStateFields.AutoJoinDungeon) != 0)
                {
                    pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)d.AutoJoinDungeonCounter);
                    pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)d.AutoJoinDungeonCount);
                }
                if ((d.DirtyMask & (uint)CharacterStateFields.IsAutoResting) != 0)
                    pos += output.AsSpan(pos).Write(d.IsAutoResting);
                if ((d.DirtyMask & (uint)CharacterStateFields.AutoTrainLevel) != 0)
                    pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)d.AutoTrainTargetLevel);
                if ((d.DirtyMask & (uint)CharacterStateFields.AutoRestTarget) != 0)
                {
                    output[pos++] = d.AutoRestTarget.HasValue ? (byte)1 : (byte)0;
                    if (d.AutoRestTarget.HasValue)
                    {
                        BinaryPrimitives.WriteInt64BigEndian(output.AsSpan(pos), BitConverter.DoubleToInt64Bits(d.AutoRestTarget.Value));
                        pos += 8;
                    }
                }
                if ((d.DirtyMask & (uint)CharacterStateFields.AutoRestStart) != 0)
                {
                    output[pos++] = d.AutoRestStart.HasValue ? (byte)1 : (byte)0;
                    if (d.AutoRestStart.HasValue)
                    {
                        BinaryPrimitives.WriteInt64BigEndian(output.AsSpan(pos), BitConverter.DoubleToInt64Bits(d.AutoRestStart.Value));
                        pos += 8;
                    }
                }
                if ((d.DirtyMask & (uint)CharacterStateFields.DungeonStyle) != 0)
                {
                    output[pos++] = d.DungeonCombatStyle.HasValue ? (byte)1 : (byte)0;
                    if (d.DungeonCombatStyle.HasValue)
                        pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)d.DungeonCombatStyle.Value);
                }
                if ((d.DirtyMask & (uint)CharacterStateFields.RaidStyle) != 0)
                {
                    output[pos++] = d.RaidCombatStyle.HasValue ? (byte)1 : (byte)0;
                    if (d.RaidCombatStyle.HasValue)
                        pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)d.RaidCombatStyle.Value);
                }
                if ((d.DirtyMask & (uint)CharacterStateFields.Platform) != 0)
                {
                    pos += output.AsSpan(pos).WriteShortString(d.PlatformUserId);
                    pos += output.AsSpan(pos).WriteShortString(d.PlatformUserName);
                }
            }

            return pos;
        }

        public static int Serialize(GameStateRequest gs, byte[] output)
        {
            int pos = 0;
            pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)gs.PlayerCount);

            output[pos++] = gs.Raid.IsActive ? (byte)1 : (byte)0;
            if (gs.Raid.IsActive)
            {
                pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)gs.Raid.BossCombatLevel);
                pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)gs.Raid.CurrentBossHealth);
                pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)gs.Raid.MaxBossHealth);
                pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)gs.Raid.PlayersJoined);
                pos += SpanWriter.Write(output.AsSpan(pos), gs.Raid.EndTime);
            }
            pos += SpanWriter.Write(output.AsSpan(pos), gs.Raid.NextRaid);

            output[pos++] = gs.Dungeon.IsActive ? (byte)1 : (byte)0;
            if (gs.Dungeon.IsActive)
            {
                var hasDungeonName = !string.IsNullOrEmpty(gs.Dungeon.Name);
                if (hasDungeonName)
                {
                    var dungeonName = Encoding.UTF8.GetBytes(gs.Dungeon.Name);
                    pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)dungeonName.Length);
                    dungeonName.CopyTo(output.AsSpan(pos));
                    pos += dungeonName.Length;
                }
                else
                {
                    pos += VarInt.WriteVarUInt(output.AsSpan(pos), 0);
                }
                output[pos++] = gs.Dungeon.HasStarted ? (byte)1 : (byte)0;
                pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)gs.Dungeon.BossCombatLevel);
                pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)gs.Dungeon.CurrentBossHealth);
                pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)gs.Dungeon.MaxBossHealth);
                pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)gs.Dungeon.PlayersAlive);
                pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)gs.Dungeon.PlayersJoined);
                pos += VarInt.WriteVarUInt(output.AsSpan(pos), (ulong)gs.Dungeon.EnemiesLeft);
                pos += SpanWriter.Write(output.AsSpan(pos), gs.Dungeon.StartTime);
            }
            pos += SpanWriter.Write(output.AsSpan(pos), gs.Dungeon.NextDungeon);
            return pos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPlayerStateBufferSize(int playerCount)
        {
            return playerCount * MaxPlayerStateSize + 32;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetExpBufferSize(int playerCount)
        {
            return playerCount * MaxExpUpdateSize + 16;
        }

        public async UniTask SendExperienceDeltasAsync(DeltaExperienceUpdate[] batch, int count)
        {
            if (batch == null || count == 0) return;
            int estimatedBufSize = GetExpBufferSize(count);
            byte[] buf = ArrayPool<byte>.Shared.Rent(estimatedBufSize);

            try
            {
                int pos = Serialize(batch, buf, count);
                if (pos > estimatedBufSize)
                {
                    Shinobytes.Debug.LogError($"[DeltaClient] Experience batch buffer overrun: used {pos} of {estimatedBufSize} bytes.");
                }

                await SendFrameAsync(EXPERIENCE_UPDATE, buf, pos);
            }
            catch (Exception ex)
            {
                Shinobytes.Debug.LogError("[DeltaClient] Error in SendExperienceDeltasAsync: " + ex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf, clearArray: true);
            }
        }

        public async UniTask SendPlayerStateAsync(CharacterStateDelta[] states, int count)
        {
            if (states == null || count == 0) return;
            int estimatedBufSize = GetPlayerStateBufferSize(count);
            byte[] buf = ArrayPool<byte>.Shared.Rent(estimatedBufSize);

            try
            {
                int pos = Serialize(states, buf, count);

                if (pos > estimatedBufSize)
                {
                    Shinobytes.Debug.LogError($"[DeltaClient] Player state batch buffer overrun: used {pos} of {estimatedBufSize} bytes.");
                }

                await SendFrameAsync(PLAYER_STATE, buf, pos);
            }
            catch (Exception ex)
            {
                Shinobytes.Debug.LogError("[DeltaClient] Error in SendPlayerStateAsync: " + ex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf, clearArray: true);
            }
        }


        public async UniTask SendGameStateAsync(GameStateRequest gs)
        {
            if (gs == null) return;
            byte[] buf = ArrayPool<byte>.Shared.Rent(MaxGameStateSize);
            try
            {
                int pos = Serialize(gs, buf);

                await SendFrameAsync(GAME_STATE, buf, pos);
            }
            catch (Exception exc)
            {
                Shinobytes.Debug.LogError("[DeltaClient] Error in SendGameStateAsync: " + exc);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf, clearArray: true);
            }
        }

        // --------- Thread-safe async frame send ---------
        private async UniTask SendFrameAsync(byte type, byte[] payload, int length)
        {
            if (length == 0 || payload == null) return;

            byte[] frame = ArrayPool<byte>.Shared.Rent(length + 5);
            try
            {
                int msgLen = 1 + length;
                BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, 4), msgLen);
                frame[4] = type;
                Array.Copy(payload, 0, frame, 5, length);

                await UniTask.Yield(); // Allow Unity main thread to yield

                //lock (socketLock)
                {
                    if (socket == null || !socket.Connected)
                    {
                        Shinobytes.Debug.LogWarning("[DeltaClient] Attempted to send while socket disconnected.");
                        return;
                    }
                    try
                    {
                        //socket.Send(frame, 0, 5 + length, SocketFlags.None).;
                        await socket.SendAsync(frame.AsMemory(0, 5 + length), SocketFlags.None, CancellationToken.None);
#if UNITY_EDITOR
                        _totalBytesSent += 5 + length;
                        string messageType = GetMessageTypeName(type);
                        if (_messageTypeBytesSent.ContainsKey(messageType))
                            _messageTypeBytesSent[messageType] += 5 + length;
                        LogPeriodicStatistics();
#endif
                    }
                    catch (SocketException se)
                    {
                        Shinobytes.Debug.LogError("[DeltaClient] SocketException: " + se);
                        Disconnect();
                    }
                    catch (Exception ex)
                    {
                        Shinobytes.Debug.LogError("[DeltaClient] SendFrameAsync error: " + ex);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(frame, clearArray: true);
            }
        }

        private UniTask SendFrameAsync(byte type, Span<byte> buf, int length)
        {
            byte[] tempBuf = ArrayPool<byte>.Shared.Rent(length);
            buf.Slice(0, length).CopyTo(tempBuf);
            var task = SendFrameAsync(type, tempBuf, length);
            ArrayPool<byte>.Shared.Return(tempBuf, clearArray: true);
            return task;
        }

        private async UniTask SendFrameAsync(byte type, byte[] payload)
        {
            await SendFrameAsync(type, payload, payload.Length);
        }

        //        private void SendFrame(byte type, byte[] buf, int length)
        //        {
        //            if (length == 0)
        //                return;

        //            byte[] frame = ArrayPool<byte>.Shared.Rent(length + 5);
        //            try
        //            {
        //                int msgLen = 1 + length;
        //                BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, 4), msgLen);
        //                frame[4] = type;
        //                Array.Copy(buf, 0, frame, 5, length);

        //                //lock (socketLock)
        //                {
        //                    if (socket == null || !socket.Connected)
        //                        return;
        //                    //socket.Send(frame, 0, 5 + length, SocketFlags.None);
        //                    await socket.SendAsync(frame.AsMemory(0, 5 + length), SocketFlags.None, CancellationToken.None);
        //#if UNITY_EDITOR
        //                    _totalBytesSent += 5 + length;
        //                    string messageType = GetMessageTypeName(type);
        //                    if (_messageTypeBytesSent.ContainsKey(messageType))
        //                        _messageTypeBytesSent[messageType] += 5 + length;
        //                    LogPeriodicStatistics();
        //#endif
        //                }
        //            }
        //            catch (SocketException)
        //            {
        //                Disconnect();
        //            }
        //            catch (Exception exc)
        //            {
        //                Shinobytes.Debug.LogError("[DeltaClient] SendFrame error: " + exc);
        //            }
        //            finally
        //            {
        //                ArrayPool<byte>.Shared.Return(frame, clearArray: true);
        //            }
        //        }

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
        private void LogPeriodicStatistics()
        {
            if ((DateTime.UtcNow - _lastLogTime).TotalSeconds >= 10)
            {
                _lastLogTime = DateTime.UtcNow;
                UnityEngine.Debug.Log(GetStatisticsReport());
            }
        }
#endif

        // Utility: Zero out array between sends if you want to ensure no stale data leaks
        public static void ClearBuffer<T>(T[] arr, int usedCount)
        {
            if (arr == null) return;
            Array.Clear(arr, 0, usedCount);
        }
    }
}