using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RavenNest.Protocol;

namespace RavenNest.SDK.V2
{
    /// <summary>
    /// Commands on the v2 connection. Each has a request id that stays the same however often it is sent: after a
    /// reconnect every unanswered command goes again, and the server answers a repeat with the first result instead
    /// of running it twice. A slow answer is waited for, never taken as a failure.
    /// </summary>
    public sealed class CommandChannel
    {
        /// <summary>How long a command may stay unanswered, reconnects included, before the caller gets nothing back.</summary>
        public static readonly TimeSpan GiveUpAfter = TimeSpan.FromMinutes(2);

        private readonly GameConnectionV2 connection;
        private readonly ConcurrentDictionary<Guid, Pending> pending = new ConcurrentDictionary<Guid, Pending>();

        public CommandChannel(GameConnectionV2 connection)
        {
            this.connection = connection;
        }

        public int Waiting => pending.Count;

        public long Sent, Answered, Failed, TimedOut;

        private sealed class Pending
        {
            public CommandRequest Request;
            public DateTime Created;
            public Action<CommandResult> Complete;
        }

        /// <summary>
        /// Runs a command and returns its value, the same model the REST call returns. Default (null, false, 0) when
        /// the server said no or never answered; the reason is logged.
        /// </summary>
        public Task<T> CallAsync<T>(string command, byte[] args, Guid requestId = default)
        {
            var request = new CommandRequest
            {
                RequestId = requestId == Guid.Empty ? Guid.NewGuid() : requestId,
                Command = command,
                Args = args
            };

            var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            var entry = new Pending
            {
                Request = request,
                Created = DateTime.UtcNow,
                Complete = result =>
                {
                    try
                    {
                        if (result == null)
                        {
                            done.TrySetResult(default);
                            return;
                        }

                        if (result.Status != CommandStatus.Ok)
                        {
                            Shinobytes.Debug.LogWarning("Command " + command + " was not done: " + result.Status + " " + result.Error);
                            done.TrySetResult(default);
                            return;
                        }

                        done.TrySetResult(ProtocolSerializer.DeserializeValue<T>(result.Value));
                    }
                    catch (Exception exc)
                    {
                        Shinobytes.Debug.LogError("Could not read the result of " + command + ": " + exc);
                        done.TrySetResult(default);
                    }
                }
            };

            if (!pending.TryAdd(request.RequestId, entry))
            {
                // the same request id is already on its way; the caller gets nothing new
                done.TrySetResult(default);
                return done.Task;
            }

            Sent++;
            connection.Send(request);
            return done.Task;
        }

        public void OnResult(CommandResult result)
        {
            if (!pending.TryRemove(result.RequestId, out var entry))
            {
                return; // an answer to a repeat we already had
            }

            if (result.Status == CommandStatus.Ok) Answered++; else Failed++;
            entry.Complete(result);
        }

        /// <summary>After a reconnect: everything unanswered goes again, with its request id.</summary>
        public void ResendAll()
        {
            foreach (var entry in pending.Values)
            {
                connection.Send(entry.Request);
            }
        }

        /// <summary>Gives up on commands unanswered for too long.</summary>
        public void Tick(DateTime now)
        {
            foreach (var (id, entry) in pending)
            {
                if (now - entry.Created > GiveUpAfter && pending.TryRemove(id, out _))
                {
                    TimedOut++;
                    Shinobytes.Debug.LogWarning("Command " + entry.Request.Command + " got no answer in " + GiveUpAfter.TotalMinutes + " minutes.");
                    entry.Complete(null);
                }
            }
        }

        /// <summary>The connection ended for good (v2 switched off): everyone waiting gets nothing back.</summary>
        public void FailAll()
        {
            foreach (var (id, entry) in pending)
            {
                if (pending.TryRemove(id, out _))
                {
                    entry.Complete(null);
                }
            }
        }
    }
}
