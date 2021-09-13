using System;
using System.Collections.Generic;
using System.Text;

namespace BlockChainTest
{
    public class SummaryBlock
    {
        private byte[] hash;
        // The list should be ordered by 
        private Dictionary<Token, string> ownership;
        private long blockNumber;

        public byte[] Hash { get => hash; set => hash = value; }
        public Dictionary<Token, string> Ownership { get => ownership; set => ownership = value; }
        public long BlockNumber { get => blockNumber; set => blockNumber = value; }

        public byte[] ToBytes()
        {
            return SerializationUtility.ObjectToByteArray(this);
        }

        public static SummaryBlock Create(byte[] data)
        {
            return SerializationUtility.ByteArrayToObject<SummaryBlock>(data);
        }

        public SummaryBlock Clone()
        {
            // TODO: optimize
            return Create(ToBytes());
        }
    }
}
