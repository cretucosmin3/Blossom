using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Kara.Utils
{
	public static class Imaging
	{
		private static WebClient client;

		public static System.Drawing.Bitmap LoadImageFromUrl(string url)
		{
			System.Drawing.Bitmap bmp = null;
			using (WebClient webClient = new WebClient())
			{
				byte[] data = webClient.DownloadData(url);

				using MemoryStream mem = new MemoryStream(data);
				bmp = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(mem);
			}

			return bmp;
		}

		public static byte[] BytesFromBitmap(Bitmap imgo)
		{
			var bitmapData = imgo.LockBits(new System.Drawing.Rectangle(0, 0, imgo.Width, imgo.Height), ImageLockMode.ReadOnly, imgo.PixelFormat);
			var length = bitmapData.Stride * bitmapData.Height;
			byte[] bytes = new byte[length];
			System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, bytes, 0, length);
			imgo.UnlockBits(bitmapData);
			return bytes;
		}

		public static byte[] LoadImageBytes(string url)
		{
			byte[] bytes = new byte[0];

			if (client == null)
				client = new WebClient();

			try
			{
				bytes = client.DownloadData(url);
			}
			catch (Exception x)
			{
				Console.WriteLine($"Error while reading from url : {x.Message}");
			}

			return bytes;
		}

		public static byte[] GetImageFromUrl(string url)
		{
			System.Net.HttpWebRequest request = null;
			System.Net.HttpWebResponse response = null;
			byte[] b = null;

			request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
			response = (System.Net.HttpWebResponse)request.GetResponse();

			if (request.HaveResponse)
			{
				if (response.StatusCode == System.Net.HttpStatusCode.OK)
				{
					Stream receiveStream = response.GetResponseStream();
					using (BinaryReader br = new BinaryReader(receiveStream))
					{
						b = br.ReadBytes(500000);
						br.Close();
					}
				}
			}

			return b;
		}
	}
}