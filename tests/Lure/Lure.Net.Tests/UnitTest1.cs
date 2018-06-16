using Lure.Net.Data;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Diagnostics;
using Xunit;

namespace Lure.Net.Tests
{
    public class UnitTest1
    {
        private const int AckBufferSize = 4;

        private SeqNo _receivePacketAck = SeqNo.Zero - 1;
        private BitVector _receivePacketAckBuffer = new BitVector(AckBufferSize);

        [Fact]
        public void Test1()
        {
            AssertAck(-1, false, false, false, false);

            AssertAck(2, 2, false, false, true, false);

            AssertAck(3, 3, true, false, false, true);

            AssertAck(5, 5, false, true, true, false);

            AssertAck(6, 6, true, false, true, true);
            AssertAck(6, 6, true, false, true, true);
            AssertAck(6, 6, true, false, true, true);

            AssertAck(9, 9, false, false, true, true);

            // 4 - last
            AssertAck(13, 13, false, false, false, true);

            // 5 - full clear
            AssertAck(18, 18, false, false, false, false);

            AssertAck(16, 18, false, true, false, false);
            AssertAck(14, 18, false, true, false, true);
            AssertAck(10, 18, false, true, false, true);

            AssertAck(50, 50, false, false, false, false);
        }

        private void AssertAck(int receivedAck, int ack, params bool[] ackBuffer)
        {
            AckReceive((SeqNo)receivedAck);
            AssertAck(ack, ackBuffer);
        }

        private void AssertAck(int ack, params bool[] ackBuffer)
        {
            Assert.Equal((SeqNo)ack, _receivePacketAck);
            Assert.Equal(ackBuffer, _receivePacketAckBuffer.AsBits());
        }

        internal void AckReceive(SeqNo seq)
        {
            var diff = seq.GetDifference(_receivePacketAck);
            if (diff == 0)
            {
                return;
            }
            else if (diff > 0)
            {
                _receivePacketAck = seq;

                if (diff > _receivePacketAckBuffer.Capacity)
                {
                    _receivePacketAckBuffer.ClearAll();
                }
                else
                {
                    _receivePacketAckBuffer.LeftShift(diff);
                    _receivePacketAckBuffer.Set(diff - 1);
                }
            }
            else
            {
                diff *= -1;
                if (diff <= _receivePacketAckBuffer.Capacity)
                {
                    _receivePacketAckBuffer.Set(diff - 1);
                }
            }

            Log.Verbose("  {Acks} <- {Ack}", _receivePacketAckBuffer, _receivePacketAck.Value);
        }
    }
}
