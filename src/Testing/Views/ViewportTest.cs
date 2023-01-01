using System.Globalization;
using System.Security.Cryptography;
using System.Numerics;
using System.Net.Mime;
using System;
using System.Diagnostics;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using System.Drawing;
using System.Threading;
using SkiaSharp;
using System.Linq;
using System.Collections.Generic;

namespace Blossom.Testing;

public class ViewportTest : View
{
    private readonly Rect MaximumRect = new Rect(0, 0, 0, 0);
    private readonly Rect ProvisionalRect = new Rect(0, 0, 0, 0);

    public ViewportTest() : base("Viewport Test") { }

    public override void Main()
    {
    }
}