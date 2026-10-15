using System.Collections.Generic;

namespace JewelBlitzArena.Net
{
    /// <summary>
    /// In-process bidirectional pipe for two (or more) local sessions.
    /// Optional <see cref="DeliveryDelayFrames"/> simulates one-way latency in sim-frame units.
    /// </summary>
    public sealed class LoopbackHub
    {
        private readonly Dictionary<int, Queue<ScheduledPacket>> _inbox = new Dictionary<int, Queue<ScheduledPacket>>();
        private int _clock;

        public int DeliveryDelayFrames { get; set; }

        public void ResetClock()
        {
            _clock = 0;
            foreach (KeyValuePair<int, Queue<ScheduledPacket>> kv in _inbox)
            {
                kv.Value.Clear();
            }
        }

        public void TickClock() => _clock++;

        public INetTransport CreateEndpoint(int localPlayerIndex, int[] remotePlayerIndices)
        {
            if (!_inbox.ContainsKey(localPlayerIndex))
            {
                _inbox[localPlayerIndex] = new Queue<ScheduledPacket>();
            }

            foreach (int remote in remotePlayerIndices)
            {
                if (!_inbox.ContainsKey(remote))
                {
                    _inbox[remote] = new Queue<ScheduledPacket>();
                }
            }

            return new Endpoint(this, localPlayerIndex, remotePlayerIndices);
        }

        private void EnqueueToRemotes(int sender, int[] remotes, InputFramePacket packet)
        {
            int deliverAt = _clock + DeliveryDelayFrames;
            for (int i = 0; i < remotes.Length; i++)
            {
                int dest = remotes[i];
                if (dest == sender)
                {
                    continue;
                }

                _inbox[dest].Enqueue(new ScheduledPacket(deliverAt, packet));
            }
        }

        private int Poll(int localPlayerIndex, InputFramePacket[] destination)
        {
            Queue<ScheduledPacket> q = _inbox[localPlayerIndex];
            int written = 0;
            int remaining = q.Count;
            for (int n = 0; n < remaining && written < destination.Length; n++)
            {
                ScheduledPacket sp = q.Dequeue();
                if (sp.DeliverAt > _clock)
                {
                    q.Enqueue(sp);
                    continue;
                }

                destination[written++] = sp.Packet;
            }

            return written;
        }

        private readonly struct ScheduledPacket
        {
            public readonly int DeliverAt;
            public readonly InputFramePacket Packet;

            public ScheduledPacket(int deliverAt, InputFramePacket packet)
            {
                DeliverAt = deliverAt;
                Packet = packet;
            }
        }

        private sealed class Endpoint : INetTransport
        {
            private readonly LoopbackHub _hub;
            private readonly int _local;
            private readonly int[] _remotes;

            public Endpoint(LoopbackHub hub, int local, int[] remotes)
            {
                _hub = hub;
                _local = local;
                _remotes = remotes;
            }

            public void Send(InputFramePacket packet) =>
                _hub.EnqueueToRemotes(_local, _remotes, packet);

            public int Poll(InputFramePacket[] destination) =>
                _hub.Poll(_local, destination);
        }
    }
}
