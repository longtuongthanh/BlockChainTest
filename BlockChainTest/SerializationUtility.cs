using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;

namespace BlockChainTest
{
    public class SerializationUtility
    {
        public static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;

            return JsonSerializer.SerializeToUtf8Bytes(obj, obj.GetType());
        }

        public static T ByteArrayToObject<T>(byte[] arrBytes)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(arrBytes);
            }
            catch (Exception e)
            {
                e.WriteMessage();
                return default(T);
            }
        }

        public static string ObjectToString(object obj)
        {
            if (obj == null)
                return null;

            return JsonSerializer.Serialize(obj, obj.GetType());
        }

        public static T StringToObject<T>(string str)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(str);
            }
            catch (Exception e)
            {
                e.WriteMessage();
                return default(T);
            }
        }

        public static string ByteArrayToStringBase64(byte[] arrBytes)
        {
            return Convert.ToBase64String(arrBytes);
        }

        public static byte[] Base64StringToByteArray(string str)
        {
            return Convert.FromBase64String(str);
        }

        public static byte[] UTF8StringToByteArray(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
        public static string ByteArrayToUTF8String(byte[] arrBytes)
        {
            return Encoding.UTF8.GetString(arrBytes);
        }
    }
}

public static partial class Extensions
{
    public static string Idem<T>(this T obj)
    {
        if (obj == null)
            return null;

        return JsonSerializer.Serialize(obj);
    }
}