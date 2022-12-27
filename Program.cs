using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Blossom
{
    internal class BlossomEntry
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        const UInt32 WM_CLOSE = 0x0010;
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        private static readonly string[] _args = Environment.GetCommandLineArgs();
        private static readonly string _appName = Path.GetFileNameWithoutExtension(_args[0]);
        private static readonly string _appPath = Path.GetDirectoryName(_args[0]);
        private static readonly string _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _appName);
        private static readonly string _appDataPathConfig = Path.Combine(_appDataPath, "config.json");
        private static readonly string _appDataPathLog = Path.Combine(_appDataPath, "log.txt");
        private static readonly string _appDataPathScreenshot = Path.Combine(_appDataPath, "screenshot.png");

        public static int GetHash(int[] values)
        {
            // Create a hash code based on the values in the array
            int hash = 17;
            foreach (int value in values)
            {
                hash = hash * 23 + value.GetHashCode();
            }
            return hash;
        }

        public static float GetProcentageDifference(float a, float b)
        {
            return (a - b) / b;
        }

        public static int ExtractPatternKey(float[] values, float alpha)
        {
            float[] procentages = new float[values.Length];

            procentages[0] = GetProcentageDifference(values[0], values[1]);
            for (int i = 1; i < values.Length; i++)
            {
                procentages[i] = GetProcentageDifference(values[i], values[i - 1]);
            }

            int[] normalized = NormalizeArray(procentages, 0, alpha);

            return GetHash(normalized);
        }

        public static int[] NormalizeArray(float[] values, float minValue, float maxValue)
        {
            float arrayMin = values.Min();
            float arrayMax = values.Max();

            float arrayRange = arrayMax - arrayMin;
            float outputRange = maxValue - minValue;
            int[] normalizedValues = new int[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                var value = ((values[i] - arrayMin) / arrayRange) * outputRange + minValue;
                normalizedValues[i] = (int)Math.Round(value, MidpointRounding.ToZero); ;
            }

            return normalizedValues;
        }

        public static int BytesOf(object obj)
        {
            return Marshal.SizeOf(obj);
        }

        static void Main(string[] args)
        {
            float[] chartValues1 = { 104.5f, 107.2f, 114.93f };
            float[] chartValues2 = { 105.3f, 107.5f, 115.4f };

            int key1 = ExtractPatternKey(chartValues1, 3);
            Console.WriteLine(key1);

            int key2 = ExtractPatternKey(chartValues2, 3);
            Console.WriteLine(key2);

            int key3 = ExtractPatternKey(chartValues2, 50);
            Console.WriteLine(key3);

            Console.ReadKey();
            // DateTime startTime = new DateTime(2019, 7, 13);
            // DateTime endTime = DateTime.Now;

            // TimeSpan difference = endTime - startTime;
            // var years = difference.Days / 365d;
            // var restYears = years - Math.Truncate(years);
            // var months = restYears * 12;
            // var restMonths = months - Math.Truncate(months);
            // var days = restMonths * 30;
            // var restDays = days - Math.Truncate(days);
            // var hours = restDays * 24;
            // var restHours = hours - Math.Truncate(hours);
            // var minutes = restHours * 60;

            // Console.WriteLine($"{(int)years} years, {(int)months} months, {(int)days} days, {(int)hours} hours, {(int)minutes} minutes");

            // Console.ReadKey();
            // return;

            // Hide console
            var consoleHnd = GetConsoleWindow();
            // NativeMethods.FreeConsole();

            // SendMessage(consoleHnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            ShowWindow(consoleHnd, 0);

            Log.Info(_appName);
            Log.Info(_appPath);
            Log.Info(_appDataPath);
            Log.Info(_appDataPathConfig);
            Log.Info(_appDataPathLog);
            Log.Info(_appDataPathScreenshot);

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Log.Debug("Closing");
            };

            Browser.Initialize();

            Environment.Exit(0);
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

    public static class ObjectSizeCalculator
    {
        public static int CalculateSize(object obj)
        {
            if (obj == null)
                return 0;

            if (obj is string)
                return Encoding.UTF8.GetByteCount((string)obj);
            else if (obj is ValueType)
                return Marshal.SizeOf(obj);

            return CalculateSizeInternal(obj);
        }

        private static int CalculateSizeInternal(object obj)
        {
            int size = 0;
            var props = obj.GetType().GetProperties();
            foreach (var prop in props)
            {
                object value = prop.GetValue(obj);
                if (value == null)
                    continue;

                if (value is string)
                {
                    size += Encoding.UTF8.GetByteCount((string)value);
                }
                else if (value is ValueType)
                {
                    size += Marshal.SizeOf(value);
                }
                else
                {
                    size += CalculateSizeInternal(value);
                }
            }
            return size;
        }
    }
}