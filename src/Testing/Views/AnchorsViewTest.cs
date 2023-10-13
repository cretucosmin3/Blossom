using System.Threading;
using System;
using System.Numerics;
using Blossom.Core;
using Blossom.Core.Input;
using Blossom.Core.Visual;
using Blossom.Testing.CustomElements;
using SkiaSharp;

namespace Blossom.Testing
{
    public class AnchorsTest : VisualTest<AnchorsView>
    {
        [Test("Element changing text")]
        public void ChangingText()
        {
            TestView.Draggable.Text = "Hello";
        }

        [Test("Changing element's left anchor")]
        [ViewSize(400, 300)]
        public void ChangingAncorLeft()
        {
            TestView.Draggable.Transform.Anchor = Anchor.Left;
        }
    }
}

public class VisualTest<T> where T : View, new()
{
    public T TestView;

    public VisualTest()
    {
        TestView = new T();
    }
}

public class Test: Attribute
{
    public Test(string name){}
}

public class ViewSize: Attribute
{
    public ViewSize(int x, int y){}
}