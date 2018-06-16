using Lure.Net.Data;
using System;
using Xunit;

namespace Lure.Net.Tests
{
    public class UnitTest1
    {
        private SeqNo _receivePacketAck = SeqNo.Zero - 1;
        private BitVector _receivePacketAckBuffer = new BitVector(Packet.AcksLength);

        public UnitTest1()
        {
        }

        [Fact]
        public void Test1()
        {

        }
    }
}
