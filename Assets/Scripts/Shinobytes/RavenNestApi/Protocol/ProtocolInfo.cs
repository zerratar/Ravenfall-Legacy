// Copied from RavenNest/src/RavenNest.Protocol by tools/sync-protocol.ps1. Change it there, not here.
namespace RavenNest.Protocol
{
    /// <summary>Constants both sides agree on.</summary>
    public static class ProtocolInfo
    {
        /// <summary>
        /// Goes up only for a change an older peer cannot ignore. Adding a key to a message, a message type or a
        /// command does not need it: unknown keys and unknown message types are skipped.
        /// </summary>
        public const int Version = 1;

        /// <summary>The WebSocket endpoint, on the same host and port as the REST API.</summary>
        public const string Path = "/api/v2/game";

        /// <summary>Each side pings this often when it has sent nothing else.</summary>
        public const int PingIntervalMs = 5_000;

        /// <summary>Nothing received for this long means the connection is dead: close it and reconnect.</summary>
        public const int DeadAfterMs = 20_000;

        /// <summary>The client sends its saves this often.</summary>
        public const int SaveIntervalMs = 2_000;

        /// <summary>
        /// The largest message either side accepts. Big enough for a stream with a thousand players restored at once;
        /// the limit is only there so a broken message cannot take all the memory.
        /// </summary>
        public const int MaxMessageBytes = 256 * 1024 * 1024;

        /// <summary>
        /// What games before <see cref="LargeMessagesFrom"/> read: they close the connection on anything bigger, so
        /// the server keeps to this for them. A restore of 600 players went over it and dropped those games' connection
        /// again on every reconnect (0.9.5.1a).
        /// </summary>
        public const int LegacyMaxMessageBytes = 4 * 1024 * 1024;

        /// <summary>The first game version that reads messages up to <see cref="MaxMessageBytes"/>.</summary>
        public static readonly System.Version LargeMessagesFrom = new System.Version(0, 9, 5, 2);

        /// <summary>A command's result is kept this long, so repeating its request id returns it instead of running it again.</summary>
        public const int CommandResultKeepSeconds = 15 * 60;
    }
}
