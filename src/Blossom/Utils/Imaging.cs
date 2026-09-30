using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blossom.Utils
{
    public static class Imaging
    {
        private static HttpClient client;

        public static async Task<byte[]> LoadImageBytes(string url)
        {
            byte[] bytes = Array.Empty<byte>();

            client ??= new HttpClient();

            try
            {
                bytes = await client.GetByteArrayAsync(url);
            }
            catch (Exception x)
            {
                Log.Error($"Error while reading from url: {x.Message}");
            }

            return bytes;
        }
    }
}