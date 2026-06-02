using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Blossom.Core.Visual
{
    public static class SKSLShaderManager
    {
        private static readonly Dictionary<BackgroundShaderType, SKRuntimeEffect> _effects = new();
        private static readonly object _lock = new();

        public const string CrtShaderSource = @"
            uniform float u_time;
            uniform float2 u_resolution;
            uniform float4 u_color;
            uniform float u_hover;

            half4 main(float2 fragCoord) {
                float2 uv = fragCoord / u_resolution;
                float4 col = u_color;
                
                // Add dynamic scanlines
                float scanline = sin(uv.y * 120.0 - u_time * 6.0) * 0.15;
                col.xyz -= scanline;
                
                // Add noise flicker
                float flicker = sin(u_time * 100.0) * cos(u_time * 50.0) * 0.015 * (1.0 - u_hover * 0.5);
                col.xyz += flicker;
                
                // Hover glow effect
                float distToCenter = length(uv - float2(0.5));
                col.xyz += (1.0 - distToCenter) * float3(0.0, 0.6, 1.0) * u_hover * 0.4;
                
                return half4(col);
            }
        ";

        public const string GridShaderSource = @"
            uniform float u_time;
            uniform float2 u_resolution;
            uniform float4 u_color;
            uniform float u_hover;

            float my_smoothstep(float edge0, float edge1, float x) {
                float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
                return t * t * (3.0 - 2.0 * t);
            }

            half4 main(float2 fragCoord) {
                float2 uv = fragCoord / u_resolution;
                float gridSpeed = u_time * 0.8;
                
                // Perspective warp
                float px = (uv.x - 0.5) / (uv.y + 0.1);
                float py = 1.0 / (uv.y + 0.1) + gridSpeed;
                
                float lineX = abs(sin(px * 12.0));
                float lineY = abs(sin(py * 8.0));
                
                float grid = my_smoothstep(0.95, 0.99, 1.0 - min(lineX, lineY));
                
                float3 baseCol = float3(0.03, 0.01, 0.06);
                float3 gridCol = u_color.xyz;
                
                float3 col = mix(baseCol, gridCol, grid * (0.6 + u_hover * 0.4));
                
                float horizon = my_smoothstep(0.12, 0.0, abs(uv.y - 0.1));
                col += gridCol * horizon * 0.6;
                
                return half4(col, u_color.w);
            }
        ";

        public const string PlasmaShaderSource = @"
            uniform float u_time;
            uniform float2 u_resolution;
            uniform float4 u_color;
            uniform float u_hover;

            half4 main(float2 fragCoord) {
                float2 uv = fragCoord / u_resolution;
                
                float t = u_time * 1.2;
                float x = uv.x;
                float y = uv.y;
                
                float w1 = sin(x * 8.0 + t);
                float w2 = sin(8.0 * (x * sin(t/2.0) + y * cos(t/3.0)) + t);
                float w3 = sin(sqrt(80.0 * (x*x + y*y)) + t);
                
                float plasma = (w1 + w2 + w3) / 3.0;
                plasma = (plasma + 1.0) / 2.0;
                
                float3 c1 = float3(0.08, 0.01, 0.18);
                float3 c2 = u_color.xyz;
                float3 c3 = float3(1.0, 0.0, 0.5); // hot pink accent
                
                float3 finalCol = mix(c1, c2, plasma);
                finalCol = mix(finalCol, c3, sin(plasma * 3.14159) * u_hover * 0.6);
                
                return half4(finalCol, u_color.w);
            }
        ";

        private static SKRuntimeEffect GetOrCreateEffect(BackgroundShaderType type)
        {
            lock (_lock)
            {
                if (_effects.TryGetValue(type, out var effect))
                {
                    return effect;
                }

                string source = type switch
                {
                    BackgroundShaderType.Scanlines => CrtShaderSource,
                    BackgroundShaderType.SynthwaveGrid => GridShaderSource,
                    BackgroundShaderType.LiquidPlasma => PlasmaShaderSource,
                    _ => throw new ArgumentException("Unsupported shader type", nameof(type))
                };

                effect = SKRuntimeEffect.Create(source, out string errors);
                if (effect == null)
                {
                    throw new InvalidOperationException($"Failed to compile SKSL shader {type}: {errors}");
                }

                _effects[type] = effect;
                return effect;
            }
        }

        public static SKShader CreateShader(BackgroundShaderType type, float time, float width, float height, SKColor color, float hoverProgress)
        {
            if (type == BackgroundShaderType.None) return null!;

            var effect = GetOrCreateEffect(type);
            var uniforms = new SKRuntimeEffectUniforms(effect);
            uniforms["u_time"] = time;
            uniforms["u_resolution"] = new float[] { width, height };
            uniforms["u_color"] = new float[] { color.Red / 255f, color.Green / 255f, color.Blue / 255f, color.Alpha / 255f };
            uniforms["u_hover"] = hoverProgress;

            return effect.ToShader(true, uniforms);
        }

        public static void TestCompilation()
        {
            try
            {
                var crt = SKRuntimeEffect.Create(CrtShaderSource, out string crtErrors);
                Console.WriteLine("[SHADER TEST] CRT Scanlines compilation: " + (crt != null ? "SUCCESS" : "FAILED - " + crtErrors));

                var grid = SKRuntimeEffect.Create(GridShaderSource, out string gridErrors);
                Console.WriteLine("[SHADER TEST] Synthwave Grid compilation: " + (grid != null ? "SUCCESS" : "FAILED - " + gridErrors));

                var plasma = SKRuntimeEffect.Create(PlasmaShaderSource, out string plasmaErrors);
                Console.WriteLine("[SHADER TEST] Liquid Plasma compilation: " + (plasma != null ? "SUCCESS" : "FAILED - " + plasmaErrors));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SHADER TEST] Check exception: " + ex.Message);
            }
        }

        public static void Shutdown()
        {
            lock (_lock)
            {
                foreach (var effect in _effects.Values)
                {
                    effect.Dispose();
                }
                _effects.Clear();
            }
        }
    }

    public static class SKSLShaderTimeTracker
    {
        public static float DeltaTime { get; set; } = 0.016f;
        public static float ElapsedSeconds { get; set; } = 0f;
    }
}
