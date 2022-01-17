using SilkyNvg;
using SilkyNvg.Text;

namespace Kara.Core
{
    internal static class Renderer
    {
        private static Nvg _renderPipeline;
        private static readonly object _lock = new();
        private static int DefaultFont;

        internal static Nvg Pipe
        {
            get => _renderPipeline;
        }

        internal static void Initialize(Nvg renderPipeline)
        {
            _renderPipeline = renderPipeline;
            DefaultFont = Renderer.Pipe.CreateFont("sans", "./fonts/FiraSans-Medium.ttf");
        }
    }
}