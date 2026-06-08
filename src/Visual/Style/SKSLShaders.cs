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
        private static SKRuntimeEffect? _halftoneEffect;

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

        public const string HolographicLatticeShaderSource = @"
            uniform float u_time;
            uniform float2 u_resolution;
            uniform float4 u_color;
            uniform float u_hover;

            float my_smoothstep(float edge0, float edge1, float x) {
                float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
                return t * t * (3.0 - 2.0 * t);
            }

            float my_atan2(float y, float x) {
                if (x == 0.0) {
                    return y > 0.0 ? 1.57079632679 : (y < 0.0 ? -1.57079632679 : 0.0);
                }
                float a = atan(y / x);
                if (x < 0.0) {
                    if (y >= 0.0) {
                        a += 3.14159265359;
                    } else {
                        a -= 3.14159265359;
                    }
                }
                return a;
            }

            half4 main(float2 fragCoord) {
                float2 uv = fragCoord / u_resolution;
                float2 p = (fragCoord - u_resolution * 0.5) / u_resolution.y;

                // Dynamic coordinate warping for fluid/organic lattice distortion
                float2 warp = float2(
                    sin(p.y * 4.0 + u_time * 0.15) * 0.08,
                    cos(p.x * 4.0 + u_time * 0.15) * 0.08
                );
                float2 warpedP = p + warp;

                // Scale grid coordinates
                float scale = 7.0 + u_hover * 3.0;
                float2 ip = floor(warpedP * scale);
                float2 fp = fract(warpedP * scale) - 0.5;

                float d = length(fp);
                float angle = my_atan2(fp.y, fp.x);

                // Multi-fold kaleidoscope symmetry inside each cell
                float folds = 6.0;
                float radialSymmetry = cos(angle * folds + u_time * 0.5);

                // Complex interference pattern
                float w = sin(d * 30.0 - radialSymmetry * 5.0) * 0.5 + 0.5;
                float cellBorder = my_smoothstep(0.44, 0.48, d);

                // Holographic spectral separation (chromatic dispersion)
                float3 rCol = float3(1.0, 0.0, 0.3) * (sin(d * 25.0 + radialSymmetry * 2.0 + 0.0) * 0.5 + 0.5);
                float3 gCol = float3(0.0, 0.9, 0.6) * (sin(d * 25.0 + radialSymmetry * 2.0 + 2.0) * 0.5 + 0.5);
                float3 bCol = float3(0.1, 0.4, 1.0) * (sin(d * 25.0 + radialSymmetry * 2.0 + 4.0) * 0.5 + 0.5);

                float3 spectralCol = (rCol + gCol + bCol) * w;

                // Background / base tint
                float3 baseCol = u_color.xyz * 0.15;

                // Combine cell pattern with base color
                float3 finalCol = mix(baseCol, spectralCol, 1.0 - cellBorder);

                // Center vignette / soft shade
                float centerVignette = my_smoothstep(0.9, 0.2, length(p));
                finalCol *= centerVignette;

                // Add highlight based on hover
                float glow = exp(-length(fp) * 4.0) * u_hover * 0.45;
                finalCol += u_color.xyz * glow;

                return half4(finalCol, u_color.w);
            }
        ";

        public const string QuantumDotsShaderSource = @"
            uniform float u_time;
            uniform float2 u_resolution;
            uniform float4 u_color;
            uniform float u_hover;

            float my_smoothstep(float edge0, float edge1, float x) {
                float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
                return t * t * (3.0 - 2.0 * t);
            }

            float2 hash22(float2 p) {
                float3 p3 = fract(float3(p.xyx) * float3(443.897, 441.423, 437.195));
                p3 += dot(p3, p3.yzx + 19.19);
                return fract((p3.xx + p3.yz) * p3.zy);
            }

            // Distance from point p to line segment between a and b
            float distToSegment(float2 p, float2 a, float2 b) {
                float2 ap = p - a;
                float2 ab = b - a;
                float h = clamp(dot(ap, ab) / dot(ab, ab), 0.0, 1.0);
                return length(ap - ab * h);
            }

            half4 main(float2 fragCoord) {
                float2 uv = fragCoord / u_resolution;
                float2 p = (fragCoord - u_resolution * 0.5) / u_resolution.y;

                float3 finalColor = u_color.xyz * 0.04; // Very dark ambient background tint

                // Scale for cell grid
                float scale = 9.0;
                float2 warpedP = p * scale;

                float2 ip = floor(warpedP);
                float2 fp = fract(warpedP) - 0.5;

                // We will collect the positions of all particles in the 3x3 neighborhood
                // local positions are relative to the current cell center
                float2 positions[9];
                int count = 0;

                for (float y = -1.0; y <= 1.0; y++) {
                    for (float x = -1.0; x <= 1.0; x++) {
                        float2 neighbor = float2(x, y);
                        float2 cellId = ip + neighbor;
                        float2 rand = hash22(cellId);

                        // Calculate particle position inside that cell
                        float speedVal = 0.4 + rand.y * 0.6;
                        float2 offset = float2(
                            sin(u_time * speedVal + rand.x * 6.28318),
                            cos(u_time * speedVal + rand.y * 6.28318)
                        ) * 0.38;

                        // Position relative to current fragment's cell center
                        positions[count] = neighbor + offset;
                        count++;
                    }
                }

                // Draw connecting lines between close particles
                float maxDist = 1.35; // Maximum distance to connect
                float lineDraw = 0.0;

                for (int i = 0; i < 9; i++) {
                    for (int j = i + 1; j < 9; j++) {
                        float distBetween = length(positions[i] - positions[j]);
                        if (distBetween < maxDist) {
                            // Distance from current fragment coord 'fp' to the segment connecting i and j
                            float dSeg = distToSegment(fp, positions[i], positions[j]);
                            
                            // Line intensity fades out as they move apart
                            float fade = 1.0 - (distBetween / maxDist);
                            fade = fade * fade; // Satisfying exponential falloff for connection strength

                            // Draw the line segment (sharp thin lines)
                            float lineVal = exp(-dSeg * 65.0) * fade * 0.75;
                            lineDraw = max(lineDraw, lineVal);
                        }
                    }
                }

                // Draw the particles themselves (white dots)
                float dotDraw = 0.0;
                for (int i = 0; i < 9; i++) {
                    float dDot = length(fp - positions[i]);
                    
                    // Satisfying core size and ambient glow
                    float core = my_smoothstep(0.045, 0.03, dDot);
                    float glow = exp(-dDot * 25.0) * 0.4;
                    
                    dotDraw = max(dotDraw, core + glow);
                }

                // Combine: dots and lines are primarily pure white (constellation style)
                float3 webColor = float3(0.95, 0.98, 1.0); // Clean slightly blueish white
                finalColor += webColor * (dotDraw + lineDraw * (0.7 + u_hover * 0.3));

                // Soft background vignette
                float vignette = my_smoothstep(0.95, 0.25, length(p));
                finalColor *= vignette;

                return half4(finalColor, u_color.w);
            }
        ";

        public const string LiquidPaintShaderSource = @"
            uniform shader u_backdrop;
            uniform float u_time;
            uniform float2 u_resolution;
            uniform float4 u_color;
            uniform float u_hover;
            uniform float u_mixingRate;

            float my_smoothstep(float edge0, float edge1, float x) {
                float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
                return t * t * (3.0 - 2.0 * t);
            }

            half4 main(float2 fragCoord) {
                float2 uv = fragCoord / u_resolution;
                
                // Sample the drawing canvas bitmap
                half4 drawing = sample(u_backdrop, fragCoord);
                
                // Clean sleek slate canvas background
                float3 canvasBg = float3(0.96, 0.96, 0.95); 
                
                // Organic canvas fine grain noise
                float noise = fract(sin(dot(fragCoord, float2(12.9898, 78.233))) * 43758.5453);
                
                // 3D normal mapping: sample neighbor height gradients
                float step = 1.2;
                float hL = sample(u_backdrop, fragCoord + float2(-step, 0.0)).a;
                float hR = sample(u_backdrop, fragCoord + float2(step, 0.0)).a;
                float hD = sample(u_backdrop, fragCoord + float2(0.0, -step)).a;
                float hU = sample(u_backdrop, fragCoord + float2(0.0, step)).a;
                
                float3 normal = normalize(float3(hL - hR, hD - hU, 0.22));
                
                // Glossy specular highlight from top-left light source
                float3 lightDir = normalize(float3(-1.0, 1.5, 2.5));
                float3 viewDir = float3(0.0, 0.0, 1.0);
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(max(dot(normal, halfDir), 0.0), 28.0) * 0.45;
                
                // Soft edges on paint strokes
                float edge = my_smoothstep(0.0, 0.1, drawing.a);
                
                // Unpremultiply color to prevent dim black outlines at anti-aliased edges
                float3 paintCol = drawing.rgb;
                float alpha = drawing.a;
                if (alpha > 0.005) {
                    paintCol = clamp(drawing.rgb / alpha, 0.0, 1.0);
                }
                
                // Blend/mix neighboring colors subtractively (CMY) if mixing is enabled
                if (u_mixingRate > 0.001 && alpha > 0.005) {
                    float stepVal = (1.5 + 3.0 * u_mixingRate);
                    float3 sumCMY = 1.0 - paintCol;
                    float totalWeight = 1.0;
                    float diag = stepVal * 0.707;
                    
                    half4 n;
                    
                    n = sample(u_backdrop, fragCoord + float2(-stepVal, 0.0));
                    if (n.a > 0.005) { sumCMY += 1.0 - clamp(n.rgb / n.a, 0.0, 1.0); totalWeight += 1.0; }
                    
                    n = sample(u_backdrop, fragCoord + float2(stepVal, 0.0));
                    if (n.a > 0.005) { sumCMY += 1.0 - clamp(n.rgb / n.a, 0.0, 1.0); totalWeight += 1.0; }
                    
                    n = sample(u_backdrop, fragCoord + float2(0.0, -stepVal));
                    if (n.a > 0.005) { sumCMY += 1.0 - clamp(n.rgb / n.a, 0.0, 1.0); totalWeight += 1.0; }
                    
                    n = sample(u_backdrop, fragCoord + float2(0.0, stepVal));
                    if (n.a > 0.005) { sumCMY += 1.0 - clamp(n.rgb / n.a, 0.0, 1.0); totalWeight += 1.0; }
                    
                    n = sample(u_backdrop, fragCoord + float2(-diag, -diag));
                    if (n.a > 0.005) { sumCMY += 1.0 - clamp(n.rgb / n.a, 0.0, 1.0); totalWeight += 1.0; }
                    
                    n = sample(u_backdrop, fragCoord + float2(diag, -diag));
                    if (n.a > 0.005) { sumCMY += 1.0 - clamp(n.rgb / n.a, 0.0, 1.0); totalWeight += 1.0; }
                    
                    n = sample(u_backdrop, fragCoord + float2(-diag, diag));
                    if (n.a > 0.005) { sumCMY += 1.0 - clamp(n.rgb / n.a, 0.0, 1.0); totalWeight += 1.0; }
                    
                    n = sample(u_backdrop, fragCoord + float2(diag, diag));
                    if (n.a > 0.005) { sumCMY += 1.0 - clamp(n.rgb / n.a, 0.0, 1.0); totalWeight += 1.0; }
                    
                    float3 mixedCMY = sumCMY / totalWeight;
                    float3 mixedRgb = clamp(1.0 - mixedCMY, 0.0, 1.0);
                    
                    // Interpolate between original color and mixed color based on mixing rate
                    paintCol = mix(paintCol, mixedRgb, u_mixingRate * 0.85);
                }
                
                paintCol += float3(spec); // Glossy wet highlight
                paintCol += (noise - 0.5) * 0.02; // Fine texture grain
                
                float3 col = mix(canvasBg, paintCol, edge);
                
                // Draw fine canvas grain on empty canvas space
                if (edge < 0.01) {
                    col += (noise - 0.5) * 0.008;
                }
                
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
                    BackgroundShaderType.HolographicLattice => HolographicLatticeShaderSource,
                    BackgroundShaderType.QuantumDots => QuantumDotsShaderSource,
                    BackgroundShaderType.LiquidPaint => LiquidPaintShaderSource,
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

        private static void TrySetUniform(SKRuntimeEffectUniforms uniforms, string name, float value)
        {
            try
            {
                uniforms[name] = value;
            }
            catch (ArgumentException)
            {
                // Ignored: uniform not present in this shader type
            }
        }

        private static void TrySetUniform(SKRuntimeEffectUniforms uniforms, string name, float[] value)
        {
            try
            {
                uniforms[name] = value;
            }
            catch (ArgumentException)
            {
                // Ignored: uniform not present in this shader type
            }
        }

        public static SKShader CreateShader(BackgroundShaderType type, float time, float width, float height, SKColor color, float hoverProgress)
        {
            if (type == BackgroundShaderType.None) return null!;

            var effect = GetOrCreateEffect(type);
            var uniforms = new SKRuntimeEffectUniforms(effect);
            TrySetUniform(uniforms, "u_time", time);
            TrySetUniform(uniforms, "u_resolution", new float[] { width, height });
            TrySetUniform(uniforms, "u_color", new float[] { color.Red / 255f, color.Green / 255f, color.Blue / 255f, color.Alpha / 255f });
            TrySetUniform(uniforms, "u_hover", hoverProgress);

            return effect.ToShader(true, uniforms);
        }

        public static SKShader CreateGlassShader(BackgroundShaderType type, float time, float width, float height, SKColor color, float hoverProgress, SKShader backdrop, SKRect elementBounds, float scaleX = 1f, float scaleY = 1f, float mixingRate = 0.25f)
        {
            if (type == BackgroundShaderType.None) return null!;

            var effect = GetOrCreateEffect(type);
            var uniforms = new SKRuntimeEffectUniforms(effect);
            TrySetUniform(uniforms, "u_time", time);
            TrySetUniform(uniforms, "u_resolution", new float[] { width, height });
            TrySetUniform(uniforms, "u_color", new float[] { color.Red / 255f, color.Green / 255f, color.Blue / 255f, color.Alpha / 255f });
            TrySetUniform(uniforms, "u_hover", hoverProgress);
            TrySetUniform(uniforms, "u_elementPos", new float[] { elementBounds.Left, elementBounds.Top });
            TrySetUniform(uniforms, "u_elementScale", new float[] { scaleX, scaleY });
            TrySetUniform(uniforms, "u_mixingRate", mixingRate);

            var children = new SKRuntimeEffectChildren(effect);
            children.Add("u_backdrop", backdrop);

            return effect.ToShader(true, uniforms, children);
        }

        public static SKShader CreateGlassBorderShader(BorderEffectType type, float time, float width, float height, SKColor color, float hoverProgress, SKShader backdrop, SKRect elementBounds, float borderWidth, float scaleX = 1f, float scaleY = 1f)
        {
            if (type == BorderEffectType.None) return null!;

            var effect = GetOrCreateBorderEffect(type);
            var uniforms = new SKRuntimeEffectUniforms(effect);
            TrySetUniform(uniforms, "u_time", time);
            TrySetUniform(uniforms, "u_resolution", new float[] { width, height });
            TrySetUniform(uniforms, "u_color", new float[] { color.Red / 255f, color.Green / 255f, color.Blue / 255f, color.Alpha / 255f });
            TrySetUniform(uniforms, "u_hover", hoverProgress);
            TrySetUniform(uniforms, "u_elementPos", new float[] { elementBounds.Left, elementBounds.Top });
            TrySetUniform(uniforms, "u_elementScale", new float[] { scaleX, scaleY });
            TrySetUniform(uniforms, "u_borderWidth", borderWidth);

            var children = new SKRuntimeEffectChildren(effect);
            children.Add("u_backdrop", backdrop);

            return effect.ToShader(true, uniforms, children);
        }

        public const string HalftoneTransitionShaderSource = @"
            uniform float u_progress;
            uniform float2 u_resolution;
            uniform float2 u_elementPos;

            float my_smoothstep(float edge0, float edge1, float x) {
                float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
                return t * t * (3.0 - 2.0 * t);
            }

            half4 main(float2 fragCoord) {
                float2 screenCoord = fragCoord + u_elementPos;
                float cellSize = 16.0;
                
                // Compute coordinates inside the grid cell in screen space
                float2 cellCoord = screenCoord - cellSize * floor(screenCoord / cellSize);
                float2 center = float2(cellSize / 2.0);
                float dist = distance(cellCoord, center);
                
                // Max radius to cover the cell corners completely
                float maxRadius = cellSize * 0.75;
                
                // Progress-based dot radius. We add a gradient fade from left to right!
                // The gradient is relative to the element's local coordinate space
                float gradient = fragCoord.x / u_resolution.x;
                
                // Let's calculate local progress for this cell
                float localProgress = u_progress;
                
                // Grow dots dynamically
                float dotProgress = clamp((localProgress * 1.3) - (gradient * 0.3), 0.0, 1.0);
                
                float targetRadius = maxRadius * dotProgress;
                
                // Smoothstep for anti-aliasing the edges
                float alpha = 1.0 - my_smoothstep(targetRadius - 1.0, targetRadius + 1.0, dist);
                
                return half4(1.0, 1.0, 1.0, alpha);
            }
        ";

        public static SKShader CreateHalftoneShader(float progress, float width, float height, float screenX, float screenY)
        {
            if (_halftoneEffect == null)
            {
                lock (_lock)
                {
                    if (_halftoneEffect == null)
                    {
                        _halftoneEffect = SKRuntimeEffect.Create(HalftoneTransitionShaderSource, out string errors);
                        if (_halftoneEffect == null)
                        {
                            Console.WriteLine("[SHADER ERROR] Halftone Transition Shader compilation failed: " + errors);
                            return null!;
                        }
                    }
                }
            }

            var uniforms = new SKRuntimeEffectUniforms(_halftoneEffect);
            TrySetUniform(uniforms, "u_progress", progress);
            TrySetUniform(uniforms, "u_resolution", new float[] { width, height });
            TrySetUniform(uniforms, "u_elementPos", new float[] { screenX, screenY });

            return _halftoneEffect.ToShader(false, uniforms);
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

                var halftone = SKRuntimeEffect.Create(HalftoneTransitionShaderSource, out string halftoneErrors);
                Console.WriteLine("[SHADER TEST] Halftone Transition compilation: " + (halftone != null ? "SUCCESS" : "FAILED - " + halftoneErrors));

                var holoLattice = SKRuntimeEffect.Create(HolographicLatticeShaderSource, out string holoLatticeErrors);
                Console.WriteLine("[SHADER TEST] Holographic Lattice compilation: " + (holoLattice != null ? "SUCCESS" : "FAILED - " + holoLatticeErrors));

                var quantumDots = SKRuntimeEffect.Create(QuantumDotsShaderSource, out string quantumDotsErrors);
                Console.WriteLine("[SHADER TEST] Quantum Dots compilation: " + (quantumDots != null ? "SUCCESS" : "FAILED - " + quantumDotsErrors));

                var liquidPaint = SKRuntimeEffect.Create(LiquidPaintShaderSource, out string liquidPaintErrors);
                Console.WriteLine("[SHADER TEST] Liquid Paint compilation: " + (liquidPaint != null ? "SUCCESS" : "FAILED - " + liquidPaintErrors));
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

                _halftoneEffect?.Dispose();
                _halftoneEffect = null;
            }
        }
    }

    public static class SKSLShaderTimeTracker
    {
        public static float DeltaTime { get; set; } = 0.016f;
        public static float ElapsedSeconds { get; set; } = 0f;
    }
}
