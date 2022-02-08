using System.Collections.Concurrent;

namespace Assets.Scripts.Overlay
{
    public abstract class OverlayCommunication : IOverlayCommunication
    {
        public const int CommunicationPort = 5486;

        protected ConcurrentQueue<OverlayPacket> readPackets = new ConcurrentQueue<OverlayPacket>();
        protected ConcurrentQueue<OverlayPacket> writePackets = new ConcurrentQueue<OverlayPacket>();
        public bool TryRead(out OverlayPacket packet)
        {
            return readPackets.TryDequeue(out packet);
        }

        public void Send(OverlayPacket data, bool replaceExistingOfSameType = false)
        {
            if (replaceExistingOfSameType)
            {
                // Super expensive
                // I hope we dont do this too often.
                var array = writePackets.ToArray();

                for (int i = 0; i < array.Length; i++)
                {
                    OverlayPacket item = array[i];
                    if (item.Name == data.Name)
                    {
                        array[i] = data;
                        // Only replace first instance, so we don't replace everything.
                        // although, there should hopefully only be one instance.
                        break;
                    }
                }

                writePackets = new ConcurrentQueue<OverlayPacket>(array);
                return;
            }
            writePackets.Enqueue(data);
        }
    }
}
