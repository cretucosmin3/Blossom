using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blossom.Utils
{
    public static class Imaging
    {
        private static HttpClient client;

        public static async Task<Bitmap> LoadImageFromUrl(string url)
        {
            Bitmap bmp = null;
            using (HttpClient webClient = new())
            {
                byte[] data = await webClient.GetByteArrayAsync(url);

                using MemoryStream mem = new(data);
                bmp = (Bitmap)Image.FromStream(mem);
            }

            return bmp;
        }

        public static byte[] BytesFromBitmap(Bitmap imgo)
        {
            var bitmapData = imgo.LockBits(new Rectangle(0, 0, imgo.Width, imgo.Height), ImageLockMode.ReadOnly, imgo.PixelFormat);
            var length = bitmapData.Stride * bitmapData.Height;
            byte[] bytes = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, bytes, 0, length);
            imgo.UnlockBits(bitmapData);

            return bytes;
        }

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
                Console.WriteLine($"Error while reading from url : {x.Message}");
            }

            return bytes;
        }
    }
}