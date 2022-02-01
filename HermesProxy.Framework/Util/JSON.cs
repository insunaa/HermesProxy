using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;

namespace HermesProxy.Framework.Util
{
    public class JSON
    {
        public static string CreateString<T>(T dataObject)
        {
            return JsonSerializer.Serialize(dataObject);
        }

        public static T CreateObject<T>(string jsonData, bool split = false)
        {
            var data = split ? jsonData.Split(':', 2)[1] : jsonData;
            data = data.Replace("\0", "");
            return JsonSerializer.Deserialize<T>(data);
        }

        // Used for protobuf json strings.
        public static byte[] Deflate<T>(string name, T data)
        {
            var jsonData = Encoding.UTF8.GetBytes(name + ":" + CreateString(data) + "\0");
            var compressedData = ZLib.Compress(jsonData);

            return BitConverter.GetBytes(jsonData.Length).Combine(compressedData);
        }
    }
}
