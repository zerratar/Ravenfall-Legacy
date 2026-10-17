using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace RavenNest.SDK.V2
{
    /// <summary>
    /// The chat message a bot command came from, for the server commands its handler sends. Each of those gets a
    /// request id made from the chat message id, the command and how many it is in the handler, so handling the same
    /// chat message again (a bot resending after a reconnect, or the game restarted mid-command) asks the server with
    /// the same ids, and the server answers with what it did the first time instead of doing it twice.
    /// </summary>
    /// <remarks>
    /// Kept in an AsyncLocal, so it follows the handler through its awaits (most handlers are async void) and
    /// nothing else sees it.
    /// </remarks>
    public sealed class ChatCommandContext
    {
        private static readonly AsyncLocal<ChatCommandContext> current = new AsyncLocal<ChatCommandContext>();

        private readonly string chatMessageId;
        private int calls;

        private ChatCommandContext(string chatMessageId)
        {
            this.chatMessageId = chatMessageId;
        }

        public static ChatCommandContext Current => current.Value;

        /// <summary>
        /// Marks the bot command about to be handled; null or empty ids (events, pings) get no context. Call
        /// <see cref="End"/> once the handler has returned (its awaits keep the context).
        /// </summary>
        public static void Begin(string identifier, string chatMessageId)
        {
            current.Value = string.IsNullOrEmpty(chatMessageId) ? null : new ChatCommandContext(identifier + "|" + chatMessageId);
        }

        public static void End()
        {
            current.Value = null;
        }

        /// <summary>The request id for the next server command of this chat message.</summary>
        public Guid NextRequestId(string command)
        {
            var n = Interlocked.Increment(ref calls);
            using (var md5 = MD5.Create())
            {
                return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(chatMessageId + "|" + command + "|" + n)));
            }
        }
    }
}
