using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RavenNest.SDK.Endpoints
{
    public class CompressedJsonSerializer : IBinarySerializer
    {
        public object Deserialize(byte[] data, Type type)
        {
            var json = Decompress(data);
            if (type == null)
            {
                return JsonConvert.DeserializeObject(json);
            }

            return JsonConvert.DeserializeObject(json, type);
        }

        public byte[] Serialize(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return Compress(json);
        }


        public static byte[] Compress(string text)
        {
            var bytes = Encoding.Unicode.GetBytes(text);
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    gs.Write(bytes, 0, bytes.Length);
                }
                return mso.ToArray();
            }
        }

        public static string Decompress(byte[] data)
        {
            // Read the last 4 bytes to get the length
            byte[] lengthBuffer = new byte[4];
            Array.Copy(data, data.Length - 4, lengthBuffer, 0, 4);
            int uncompressedSize = BitConverter.ToInt32(lengthBuffer, 0);

            var buffer = new byte[uncompressedSize];
            using (var ms = new MemoryStream(data))
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    gzip.Read(buffer, 0, uncompressedSize);
                }
            }
            return Encoding.Unicode.GetString(buffer);
        }
    }
}