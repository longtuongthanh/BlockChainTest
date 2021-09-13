using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace BlockChainTest
{
    [Serializable]
    public class Block
    {
        private byte[] previousHash;
        private List<Entry> entries;
        private byte[] suffix;
        private byte[] hash;
        private long blockNumber;

        public long BlockNumber { get => blockNumber; set => blockNumber = value; }
        public byte[] PreviousHash { get => previousHash; set => previousHash = value; }
        public List<Entry> Entries { get => entries; set => entries = value; }
        public byte[] Suffix { get => suffix; set => suffix = value; }

        public byte[] GetHash()
        {
            if (hash == null)
                CalculateHash();
            return hash;
        }

        public void CalculateHash()
        {
            hash = CryptoUtility.CalculateHash(ToBytes());
        }

        private bool CheckValidPrivate(int zerolength)
        {
            CalculateHash();

            bool result = true;

            // the hash must have some amount of first bits as 0
            result &= CheckFirstZeroBits(hash, zerolength);

            return result;
        }

        public bool CheckValid(int zerolength)
        {
            // All fields must be filled.
            if (previousHash == null)
                return false;
            if (Entries == null)
                return false;
            if (suffix == null)
                return false;

            foreach (Entry item in Entries)
            {
                if (!item.VerifySignature())
                    return false;
                if (item.BlockNumber != BlockNumber)
                    return false;
            }

            CalculateHash();

            bool result = true;
            
            // the hash must have some amount of first bits as 0
            result &= CheckFirstZeroBits(hash, zerolength);

            return result;
        }

        private static bool CheckFirstZeroBits(byte[] array, int zerolength)
        {
            if (zerolength / 8 > array.Length)
            {
                ("Warning at BlockChain::CheckFirstZeroBits\n" +
                    "zerolength is higher than hash result size.").WriteMessage();
                return false;
            }

            int _ZeroBytes = zerolength / 8;
            int _LessCompare = 1 << (8 - zerolength % 8);

            for (int i = 0; i < _ZeroBytes; i++)
                if (array[i] != 0)
                    return false;

            if (zerolength % 8 != 0 && array[_ZeroBytes] < _LessCompare)
                return false;

            return true;
        }
        public byte[] ToBytes()
        {
            return SerializationUtility.ObjectToByteArray(this);
        }
        public static Block Create(byte[] data)
        {
            return SerializationUtility.ByteArrayToObject<Block>(data);
        }
    }
}
