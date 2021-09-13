using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BlockChainTest
{
    static class CryptoUtility
    {
        static public RSA CreateRSA()
        {
            return RSA.Create();
        }
        public static byte[] CalculateHash(byte[] input)
        {
            HashAlgorithm _EncrptStrat = HashAlgorithm.Create(BlockChain.HashAlgorithm.Name);
            return _EncrptStrat.ComputeHash(input);
        }
    }
}
