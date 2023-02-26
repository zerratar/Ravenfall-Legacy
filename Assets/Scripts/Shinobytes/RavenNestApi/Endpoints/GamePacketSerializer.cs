﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RavenNest.SDK.Endpoints
{
    public class GamePacketSerializer : IGamePacketSerializer
    {
        private readonly IBinarySerializer binarySerializer;
        private readonly Dictionary<string, Type> loadedTypes;

        public GamePacketSerializer(IBinarySerializer binarySerializer)
        {
            this.binarySerializer = binarySerializer;

            loadedTypes = new Dictionary<string, Type>();

            // load assemblies from this project and all shinobytes projects.

            LoadPacketTypes(typeof(GamePacketSerializer).Assembly.GetTypes());  // this assembly
            LoadPacketTypes(typeof(RavenNest.Models.Item).Assembly.GetTypes()); // RavenNest Models
        }

        private void LoadPacketTypes(Type[] types)
        {
            foreach (var type in types)
            {
                if (!type.IsPublic)
                {
                    continue;
                }

                loadedTypes[type.Name] = type;
            }
        }

        public GamePacket Deserialize(byte[] data)
        {
            return Deserialize(data, data.Length);
        }

        public GamePacket Deserialize(byte[] data, int length)
        {
            var packet = new GamePacket();
            using (var ms = new MemoryStream(data, 0, length))
            using (var br = new BinaryReader(ms))
            {
                packet.Id = br.ReadString();
                packet.Type = br.ReadString();
                packet.CorrelationId = new Guid(br.ReadBytes(br.ReadInt32()));

                var dataSize = br.ReadInt32();
                var payload = br.ReadBytes(dataSize);

                packet.Data = loadedTypes.TryGetValue(packet.Type, out var targetType)
                    ? binarySerializer.Deserialize(payload, targetType)
                    : payload;
            }
            return packet;
        }

        public void Serialize(BinaryWriter bw, GamePacket packet)
        {
            bw.Write(packet.Id);
            bw.Write(packet.Type);

            var correlationBytes = packet.CorrelationId.ToByteArray();
            bw.Write(correlationBytes.Length);
            bw.Write(correlationBytes);

            var body = binarySerializer.Serialize(packet.Data);
            bw.Write(body.Length);
            bw.Write(body);
            bw.Flush();
        }

        public byte[] SerializeMany(IReadOnlyList<GamePacket> packetsToSend)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write("collection");
                bw.Write(packetsToSend.Count);
                foreach (var packet in packetsToSend)
                {
                    Serialize(bw, packet);
                }

                return ms.ToArray();
            }
        }

        public byte[] Serialize(GamePacket packet)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(packet.Id);
                bw.Write(packet.Type);

                var correlationBytes = packet.CorrelationId.ToByteArray();
                bw.Write(correlationBytes.Length);
                bw.Write(correlationBytes);

                var body = binarySerializer.Serialize(packet.Data);
                bw.Write(body.Length);
                bw.Write(body);

                return ms.ToArray();
            }
        }
    }
}