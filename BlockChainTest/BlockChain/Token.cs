using System;
using System.Collections.Generic;
using System.Text;

namespace BlockChainTest
{
    [Serializable]
    public class Token
    {
        public Token()
        {
            tokenDictionary[ToString()] = this;
        }

        public static Dictionary<string, Token> tokenDictionary = new Dictionary<string, Token>();

        private string dataSource;

        public string DataSource { get => dataSource; set => dataSource = value; }

        public static Token Create(string data)
        {
            if (tokenDictionary.ContainsKey(data))
                return tokenDictionary[data];
            else
                return SerializationUtility.StringToObject<Token>(data);
        }

        public override string ToString()
        {
            return SerializationUtility.ObjectToString(this);
        }
    }
}
