using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;

namespace BlockChainTest
{
    [Serializable]
    public class Entry
    {
        private byte[] sourcePK;
        private byte[] destinationPK;
        private Token token;
        private byte[] signature;
        private long blockNumber;

        public byte[] SourcePK { get => sourcePK; set => sourcePK = value; }
        public byte[] DestinationPK { get => destinationPK; set => destinationPK = value; }
        public string Token { get => token.ToString(); 
            set => token = BlockChainTest.Token.Create(value); }
        public byte[] Signature { get => signature; set => signature = value; }
        // Block number must not be too close.
        public long BlockNumber { get => blockNumber; set => blockNumber = value; }

        public Token GetToken()
        {
            return token;
        }

        public byte[] MakeSignature(byte[] privatekey)
        {
            using (RSA rsa = CryptoUtility.CreateRSA())
            {
                rsa.ImportRSAPrivateKey(privatekey, out _);

                if (!rsa.ExportRSAPublicKey().SequenceEqual(SourcePK))
                {
                    ("Warning at Entry::MakeSignature\nPrivate key and Public key don't match.").WriteMessage();
                    return null;
                }

                Signature = null;

                Signature = rsa.SignData(ToBytes(), BlockChain.HashAlgorithm, 
                    BlockChain.SignaturePadding);
            }
            return Signature;
        }

        // Also sets signature if verification is true
        public bool VerifySignature(byte[] signature = null)
        {
            if (signature == null)
                signature = this.Signature;
            if (signature == null)
            {
                ("Warning at Entry::VerifySignature(signature)\n" +
                    "No signature found for testing").WriteMessage();
                return false;
            }

            bool _result = false;
            using (RSA rsa = CryptoUtility.CreateRSA()) {
                rsa.ImportRSAPublicKey(sourcePK, out _);

                _result = rsa.VerifyData(ToBytesNoSignature(), signature, 
                    BlockChain.HashAlgorithm, BlockChain.SignaturePadding);
            }

            if (_result)
                this.Signature = signature;

            return _result;
        }

        public byte[] ToBytes()
        {
            return SerializationUtility.ObjectToByteArray(this);
        }

        public byte[] ToBytesNoSignature()
        {
            byte[] _memSignature = Signature;
            Signature = null;
            byte[] result = SerializationUtility.ObjectToByteArray(this);
            Signature = _memSignature;
            return result;
        }

        public static Entry Create(byte[] serializedData)
        {
            return SerializationUtility.ByteArrayToObject<Entry>(serializedData);
        }
    }
}
