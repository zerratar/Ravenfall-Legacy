// Copied from RavenNest/src/RavenNest.Protocol by tools/sync-protocol.ps1. Change it there, not here.
using System;
using MessagePack;
using MessagePack.Resolvers;

namespace RavenNest.Protocol
{
    /// <summary>
    /// Reads and writes the messages. Messages use explicit keys and LZ4 for anything large; command arguments and
    /// results, which are the existing REST models without keys, use the contractless resolver.
    /// </summary>
    public static class ProtocolSerializer
    {
        /// <summary>Messages: keyed contracts, LZ4 compressed above MessagePack's small-message threshold.</summary>
        public static readonly MessagePackSerializerOptions Messages = MessagePackSerializerOptions.Standard
            .WithResolver(StandardResolver.Instance)
            .WithCompression(MessagePackCompression.Lz4BlockArray)
            .WithSecurity(MessagePackSecurity.UntrustedData);

        /// <summary>Command arguments and results: the REST models as they are, matched by member name.</summary>
        public static readonly MessagePackSerializerOptions Values = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithSecurity(MessagePackSecurity.UntrustedData);

        public static byte[] Serialize(IMessage message)
        {
            return MessagePackSerializer.Serialize(message, Messages);
        }

        /// <summary>
        /// Reads a message. Returns false for a message type this side does not know (a newer peer), which the
        /// caller skips; throws <see cref="MessagePackSerializationException"/> for a broken message.
        /// </summary>
        public static bool TryDeserialize(ReadOnlyMemory<byte> data, out IMessage message)
        {
            message = MessagePackSerializer.Deserialize<IMessage>(data, Messages);
            return message != null;
        }

        public static byte[] SerializeValue<T>(T value)
        {
            return MessagePackSerializer.Serialize(value, Values);
        }

        public static T DeserializeValue<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return default;
            }
            return MessagePackSerializer.Deserialize<T>(data, Values);
        }
    }
}
