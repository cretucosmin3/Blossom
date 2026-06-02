using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Blossom.Core.Visual
{
    public static class SKSLShaderManager
    {
        private static readonly Dictionary<BackgroundShaderType, SKRuntimeEffect> _effects = new();
        private static readonly Dictionary<BorderEffectType, SKRuntimeEffect> _borderEffects = new();
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

        public const string GlassRefractionShaderSource = @"
            uniform shader u_backdrop;
            uniform float u_time;
            uniform float2 u_resolution;
            uniform float4 u_color;
            uniform float u_hover;
            uniform float2 u_elementPos;
            uniform float2 u_elementScale;

            float my_smoothstep(float edge0, float edge1, float x) {
                float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
                return t * t * (3.0 - 2.0 * t);
            }

            half4 main(float2 fragCoord) {
                float2 uv = fragCoord / u_resolution;
                
                // static organic liquid distortion wave
                float2 dist = float2(
                    sin(fragCoord.y * 0.04) * 5.0 + cos(fragCoord.x * 0.04) * 3.0,
                    cos(fragCoord.x * 0.04) * 5.0 + sin(fragCoord.y * 0.04) * 3.0
                );
                
                float2 screenCoord = fragCoord * u_elementScale + u_elementPos;
                float2 sampleCoord = screenCoord + dist * (1.0 + u_hover * 0.4);
                
                // 9-tap blur centered at sampleCoord
                half4 blurred = half4(0.0);
                float step = 2.0;
                
                blurred += sample(u_backdrop, sampleCoord + float2(-step, -step));
                blurred += sample(u_backdrop, sampleCoord + float2(0.0, -step));
                blurred += sample(u_backdrop, sampleCoord + float2(step, -step));
                blurred += sample(u_backdrop, sampleCoord + float2(-step, 0.0));
                blurred += sample(u_backdrop, sampleCoord);
                blurred += sample(u_backdrop, sampleCoord + float2(step, 0.0));
                blurred += sample(u_backdrop, sampleCoord + float2(-step, step));
                blurred += sample(u_backdrop, sampleCoord + float2(0.0, step));
                blurred += sample(u_backdrop, sampleCoord + float2(step, step));
                blurred /= 9.0;
                
                // static specular sheen highlight (diagonal sweep)
                float sheen = my_smoothstep(0.42, 0.58, sin(uv.x - uv.y) * 0.5 + 0.5);
                
                // frosted glass noise grain
                float noise = fract(sin(dot(fragCoord, float2(12.9898, 78.233))) * 43758.5453);
                
                half4 col = mix(blurred, u_color, u_color.w);
                col.rgb += (noise - 0.5) * 0.035;
                col.rgb += float3(sheen) * 0.12 * (1.0 + u_hover * 0.4);
                
                return col;
            }
        ";

        public const string GlassBorderShaderSource = @"
            uniform shader u_backdrop;
            uniform float u_time;
            uniform float2 u_resolution;
            uniform float4 u_color;
            uniform float u_hover;
            uniform float2 u_elementPos;
            uniform float2 u_elementScale;
            uniform float u_borderWidth;

            float my_smoothstep(float edge0, float edge1, float x) {
                float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
                return t * t * (3.0 - 2.0 * t);
            }

            half4 main(float2 fragCoord) {
                float2 uv = fragCoord / u_resolution;
                
                // sample backdrop slightly inwards to refract inner colors
                float2 toCenter = float2(0.5) - uv;
                float2 sampleOffset = normalize(toCenter) * u_borderWidth * 2.0;
                
                float2 screenCoord = fragCoord * u_elementScale + u_elementPos;
                float2 sampleCoord = screenCoord + sampleOffset;
                
                half4 backCol = sample(u_backdrop, sampleCoord);
                
                // amplify brightness/saturation of refracted background color
                float3 edgeColor = backCol.rgb * 1.6;
                
                // calculate distance to edge in pixels
                float borderDistX = min(fragCoord.x, u_resolution.x - fragCoord.x);
                float borderDistY = min(fragCoord.y, u_resolution.y - fragCoord.y);
                float distToEdge = min(borderDistX, borderDistY);
                
                // sharp glint centered along the border stroke
                float strokeMid = u_borderWidth * 0.5;
                float shine = my_smoothstep(u_borderWidth * 0.5, 0.0, abs(distToEdge - strokeMid));
                
                // subtle additive light reflection from top-left
                float topEdge = my_smoothstep(u_borderWidth, 0.0, fragCoord.y);
                float leftEdge = my_smoothstep(u_borderWidth, 0.0, fragCoord.x);
                float topLeftGlow = max(topEdge, leftEdge) * 0.35;
                
                // combine dynamic backdrop reflection with thin specular glint
                float3 col = edgeColor + float3(shine * 0.15 + topLeftGlow * 0.25);
                col = mix(col, u_color.rgb, 0.1);
                col = clamp(col, 0.0, 1.0);
                
                return half4(col, 1.0);
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
                    BackgroundShaderType.GlassRefraction => GlassRefractionShaderSource,
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

        private static SKRuntimeEffect GetOrCreateBorderEffect(BorderEffectType type)
        {
            lock (_lock)
            {
                if (_borderEffects.TryGetValue(type, out var effect))
                {
                    return effect;
                }

                string source = type switch
                {
                    BorderEffectType.GlassReflection => GlassBorderShaderSource,
                    _ => throw new ArgumentException("Unsupported border effect type", nameof(type))
                };

                effect = SKRuntimeEffect.Create(source, out string errors);
                if (effect == null)
                {
                    throw new InvalidOperationException($"Failed to compile SKSL border shader {type}: {errors}");
                }

                _borderEffects[type] = effect;
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

        public static SKShader CreateGlassShader(BackgroundShaderType type, float time, float width, float height, SKColor color, float hoverProgress, SKShader backdrop, SKRect elementBounds, float scaleX = 1f, float scaleY = 1f)
        {
            if (type == BackgroundShaderType.None) return null!;

            var effect = GetOrCreateEffect(type);
            var uniforms = new SKRuntimeEffectUniforms(effect);
            uniforms["u_time"] = time;
            uniforms["u_resolution"] = new float[] { width, height };
            uniforms["u_color"] = new float[] { color.Red / 255f, color.Green / 255f, color.Blue / 255f, color.Alpha / 255f };
            uniforms["u_hover"] = hoverProgress;
            uniforms["u_elementPos"] = new float[] { elementBounds.Left, elementBounds.Top };
            uniforms["u_elementScale"] = new float[] { scaleX, scaleY };

            var children = new SKRuntimeEffectChildren(effect);
            children.Add("u_backdrop", backdrop);

            return effect.ToShader(true, uniforms, children);
        }

        public static SKShader CreateGlassBorderShader(BorderEffectType type, float time, float width, float height, SKColor color, float hoverProgress, SKShader backdrop, SKRect elementBounds, float borderWidth, float scaleX = 1f, float scaleY = 1f)
        {
            if (type == BorderEffectType.None) return null!;

            var effect = GetOrCreateBorderEffect(type);
            var uniforms = new SKRuntimeEffectUniforms(effect);
            uniforms["u_time"] = time;
            uniforms["u_resolution"] = new float[] { width, height };
            uniforms["u_color"] = new float[] { color.Red / 255f, color.Green / 255f, color.Blue / 255f, color.Alpha / 255f };
            uniforms["u_hover"] = hoverProgress;
            uniforms["u_elementPos"] = new float[] { elementBounds.Left, elementBounds.Top };
            uniforms["u_elementScale"] = new float[] { scaleX, scaleY };
            uniforms["u_borderWidth"] = borderWidth;

            var children = new SKRuntimeEffectChildren(effect);
            children.Add("u_backdrop", backdrop);

            return effect.ToShader(true, uniforms, children);
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

                var glassRef = SKRuntimeEffect.Create(GlassRefractionShaderSource, out string glassRefErrors);
                Console.WriteLine("[SHADER TEST] Glass Refraction compilation: " + (glassRef != null ? "SUCCESS" : "FAILED - " + glassRefErrors));

                var glassBorder = SKRuntimeEffect.Create(GlassBorderShaderSource, out string glassBorderErrors);
                Console.WriteLine("[SHADER TEST] Glass Border compilation: " + (glassBorder != null ? "SUCCESS" : "FAILED - " + glassBorderErrors));
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

                foreach (var effect in _borderEffects.Values)
                {
                    effect.Dispose();
                }
                _borderEffects.Clear();
            }
        }
    }

    public static class SKSLShaderTimeTracker
    {
        public static float DeltaTime { get; set; } = 0.016f;
        public static float ElapsedSeconds { get; set; } = 0f;
    }
}
