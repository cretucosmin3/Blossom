﻿using System.Net.Mime;
using System.Diagnostics.Tracing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkyNvg;
using SilkyNvg.Rendering.OpenGL;
using StbImageWriteSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Kara
{
	internal class KaraEntry
	{
		private static readonly string[] _args = Environment.GetCommandLineArgs();
		private static readonly string _appName = Path.GetFileNameWithoutExtension(_args[0]);
		private static readonly string _appPath = Path.GetDirectoryName(_args[0]);
		private static readonly string _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _appName);
		private static readonly string _appDataPathConfig = Path.Combine(_appDataPath, "config.json");
		private static readonly string _appDataPathLog = Path.Combine(_appDataPath, "log.txt");
		private static readonly string _appDataPathScreenshot = Path.Combine(_appDataPath, "screenshot.png");

		static void Main(string[] args)
		{
			List<temp> temp = new List<temp>();

			temp x = new("Vasile", 15);

			temp.Add(x);
			temp.Add(new("Bogdan", 16));
			temp.Add(new("Alex", 17));
			temp.Add(new("Pepe", 18));

			Log.Debug(string.Join(", ", temp));

			x.Name = "Ionut";
			x.Age = 19;

			x = new temp("Razvan", 23);

			Log.Info(string.Join(", ", temp));

			// AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
			// {
			// 	Log.Debug("Closing");
			// };

			// Browser.Initialize();
		}

		// static void UnpremultiplyAlpha(Span<byte> image, int w, int h, int stride)
		// {
		// 	for (int y = 0; y < h; y++)
		// 	{
		// 		Span<byte> row = image[(y * stride)..];
		// 		for (int x = 0; x < w; x += 4)
		// 		{
		// 			byte r = row[x + 0];
		// 			byte g = row[x + 1];
		// 			byte b = row[x + 2];
		// 			byte a = row[x + 3];
		// 			if (a != 0)
		// 			{
		// 				row[x + 0] = (byte)Math.Min(r * 255 / a, 255);
		// 				row[x + 1] = (byte)Math.Min(g * 255 / a, 255);
		// 				row[x + 2] = (byte)Math.Min(b * 255 / a, 255);
		// 			}
		// 		}
		// 	}

		// 	for (int y = 0; y < h; y++)
		// 	{
		// 		int offset = y * stride;
		// 		Span<byte> row = image[offset..];
		// 		for (int x = 0; x < w; x++)
		// 		{
		// 			byte r = 0, g = 0, b = 0;
		// 			byte a = row[3];
		// 			byte n = 0;
		// 			if (a == 0)
		// 			{
		// 				if (x - 1 > 0 && image[offset - 1] != 0)
		// 				{
		// 					r += image[offset - 4];
		// 					g += image[offset - 3];
		// 					b += image[offset - 2];
		// 					n++;
		// 				}
		// 				if (x + 1 < w && row[7] != 0)
		// 				{
		// 					r += row[4];
		// 					g += row[5];
		// 					b += row[6];
		// 					n++;
		// 				}
		// 				if (y - 1 > 0 && image[offset - stride + 3] != 0)
		// 				{
		// 					r += image[offset - stride];
		// 					g += image[offset - stride + 1];
		// 					b += image[offset - stride + 2];
		// 					n++;
		// 				}
		// 				if (y + 1 < h && row[stride + 3] != 0)
		// 				{
		// 					r += row[stride];
		// 					g += row[stride + 1];
		// 					b += row[stride + 2];
		// 					n++;
		// 				}
		// 				if (n > 0)
		// 				{
		// 					row[0] = (byte)(r / n);
		// 					row[1] = (byte)(g / n);
		// 					row[2] = (byte)(b / n);
		// 				}
		// 			}
		// 			row = image[(offset + x * 4)..];
		// 		}
		// 	}
		// }

		// static void SetAlpha(Span<byte> image, int w, int h, int stride, byte a)
		// {
		// 	for (int y = 0; y < h; y++)
		// 	{
		// 		Span<byte> row = image[(y * stride)..];
		// 		for (int x = 0; x < w; x++)
		// 		{
		// 			row[x * 4 + 3] = a;
		// 		}
		// 	}
		// }

		// static void FlipHorizontally(Span<byte> image, int w, int h, int stride)
		// {
		// 	int i = 0;
		// 	int j = h - 1;
		// 	while (i < j)
		// 	{
		// 		Span<byte> ri = image[(i * stride)..];
		// 		Span<byte> rj = image[(j * stride)..];
		// 		for (int k = 0; k < w * 4; k++)
		// 		{
		// 			byte t = ri[k];
		// 			ri[k] = rj[k];
		// 			rj[k] = t;
		// 		}
		// 		i++;
		// 		j--;
		// 	}
		// }

		// static void SaveScreenShot(int w, int h, bool premult, string name)
		// {
		// 	Span<byte> image = new byte[w * h * 4];
		// 	gl.ReadPixels(0, 0, (uint)w, (uint)h, GLEnum.Rgba, GLEnum.UnsignedByte, image);

		// 	if (premult)
		// 	{
		// 		UnpremultiplyAlpha(image, w, h, w * 4);
		// 	}
		// 	else
		// 	{
		// 		SetAlpha(image, w, h, w * 4, 255);
		// 	}

		// 	FlipHorizontally(image, w, h, w * 4);
		// 	ImageWriter imageWriter = new();
		// 	imageWriter.WritePng(image.ToArray(), w, h, ColorComponents.RedGreenBlueAlpha, File.OpenWrite(name));
		// }
	}

	class temp : IDisposable
	{
		public string Name { get; set; }
		public int Age { get; set; }

		public temp(string name, int id) => (Name, Age) = (name, id);

		public void Dispose()
		{

		}

		public override string ToString() => $"{Name} {Age}";
	}
}
