using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>
/// Catches fire on hover. Flames wrap the pill and rise a little above it, the heat makes the
/// live pixels above shimmer, the pill chars in from its edge with glowing ember fronts, and sparks
/// rise. A click flares the fire.
/// </summary>
public sealed class FireButton : ShaderButton
{
    private const string FireSource = @"
        uniform shader u_scene;   // the frame under the effect, device px
        uniform float2 u_origin;  // button top-left, device px
        uniform float u_scale;    // device px per UI px
        uniform float2 u_size;    // button size, UI px
        uniform float4 u_region;  // effect area in button space, UI px (left, top, right, bottom)
        uniform float u_p;        // hover progress 0..1
        uniform float u_time;     // seconds since ignition
        uniform float u_pulse;    // seconds since the last click, -1 when none

        float hash(float2 p) { return fract(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

        float noise(float2 p) {
            float2 i = floor(p);
            float2 f = fract(p);
            float2 u = f * f * (3.0 - 2.0 * f);
            return mix(mix(hash(i), hash(i + float2(1.0, 0.0)), u.x),
                       mix(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), u.x), u.y);
        }

        float fbm(float2 p) {
            float v = 0.0;
            float a = 0.5;
            for (int i = 0; i < 4; i++) {
                v += a * noise(p);
                p = p * 2.03 + float2(1.7, 9.2);
                a *= 0.5;
            }
            return v;
        }

        // pow() with a negative base is undefined in SkSL, so squares are spelled out.
        float sq(float x) { return x * x; }

        // Signed distance to the pill (negative inside).
        float pill(float2 p, float2 size) {
            float r = size.y * 0.5;
            return length(p - float2(clamp(p.x, r, size.x - r), r)) - r;
        }

        // Black-body ramp: embers, red, orange, yellow, white.
        float3 heat(float t) {
            t = clamp(t, 0.0, 1.0);
            return float3(smoothstep(0.0, 0.4, t), smoothstep(0.3, 0.8, t) * 0.92, smoothstep(0.7, 1.0, t) * 0.8);
        }

        half4 main(float2 frag) {
            float2 p = (frag - u_origin) / u_scale;
            float2 size = u_size;
            float grow = u_p * u_p * (3.0 - 2.0 * u_p);
            float flare = u_pulse >= 0.0 ? exp(-u_pulse * 2.2) : 0.0;
            float t = u_time;

            // Everything fades to the untouched frame at the edge of the effect area.
            float edge = min(min(p.x - u_region.x, u_region.z - p.x), min(p.y - u_region.y, u_region.w - p.y));
            float fade = smoothstep(0.0, 28.0, edge);

            float d = pill(p, size);
            // Flames hug the pill: they rise ~2x further than they spread sideways and lick a little under
            // it. The sides pull in as they rise so the fire narrows into a crown.
            float mid = size.y * 0.5;
            float above = max(mid - p.y, 0.0);
            float2 pp = float2(size.x * 0.5 + (p.x - size.x * 0.5) * (1.0 + above * 0.0045),
                               mid + (p.y - mid) * (p.y < mid ? 0.55 : 1.15));
            float dd = pill(pp, size);
            // Uneven crown: some columns burn taller than others, and it shifts over time.
            float crown = 0.6 + 0.8 * noise(float2(p.x / 34.0, t * 0.9));
            float len = (20.0 + 18.0 * flare) * grow * crown;

            // Fuel: the rim inside the pill (the label stays clear), fading with distance outside.
            float fuel = d < 0.0 ? smoothstep(-10.0, 0.0, d) : clamp(1.0 - max(dd, 0.0) / max(len, 0.001), 0.0, 1.0);

            // Rising, domain-warped turbulence, stretched vertically into tongues.
            float2 q = float2(p.x / 22.0, p.y / 44.0 + t * 1.7);
            float2 w = float2(fbm(q * 0.8 + float2(0.0, t * 0.6)), fbm(q * 0.8 + float2(4.3, 1.7)));
            float n = fbm(q + (w - 0.5) * 1.8);
            float fire = clamp((fuel - (1.0 - n) * 0.85) * 1.6, 0.0, 1.0) * smoothstep(0.0, 0.15, grow) * fade;

            // Heat haze: the live pixels above the flames shimmer.
            float hazeAmt = clamp(1.0 - max(dd, 0.0) / (len * 2.4 + 0.001), 0.0, 1.0) * grow * fade;
            float2 haze = (float2(fbm(q * 2.3 + 7.0), fbm(q * 2.3 + 3.1)) - 0.5) * (7.0 + 6.0 * flare) * hazeAmt;
            float3 col = u_scene.eval(u_origin + (p + haze) * u_scale).rgb;

            // The pill chars from its rim: a burn front of glowing embers eats inward.
            if (d < 0.0 && grow > 0.001) {
                float burn = noise(p / 7.0) * 0.45 + noise(p / 16.0 + 3.0) * 0.35 + smoothstep(-16.0, 0.0, d) * 0.5;
                float front = 1.04 - 0.3 * grow - 0.12 * flare;
                float charred = smoothstep(front - 0.02, front + 0.02, burn) * fade;
                float ember = exp(-sq((burn - front) / 0.025)) * grow;
                float smoulder = 0.5 + 0.5 * sin(t * 7.0 + noise(p / 6.0) * 6.2831);
                col = mix(col, col * 0.12 + float3(0.35, 0.06, 0.0) * smoulder * 0.35, charred);
                col += float3(1.0, 0.42, 0.06) * ember * (0.75 + 0.25 * smoulder);
            }

            float3 flame = heat(fire * 0.95);
            col = mix(col, flame, smoothstep(0.0, 0.35, fire)) + flame * fire * 0.35;

            // Warm light cast on the surroundings.
            col += float3(1.0, 0.42, 0.12) * (0.14 + 0.2 * flare) * grow * exp(-sq(max(d, 0.0) / 40.0)) * fade;

            // Sparks: sparse cells drifting up, wobbling, cooling with height.
            float sparks = 0.0;
            for (int i = 0; i < 2; i++) {
                float layer = float(i);
                float cell = 16.0 + 8.0 * layer;
                float2 sp = p + float2(sin(p.y * 0.05 + t * 2.0 + layer * 2.1) * 6.0, t * (70.0 + 40.0 * layer));
                float2 id = floor(sp / cell);
                if (hash(id + layer * 17.0) > 0.8) {
                    float2 c = (id + 0.5 + (float2(hash(id + 3.1), hash(id + 7.7)) - 0.5) * 0.6) * cell;
                    float2 dv = sp - c;
                    sparks += exp(-dot(dv, dv) / 2.2) * (1.0 - clamp(-p.y / 110.0, 0.0, 1.0));
                }
            }
            float column = 1.0 - smoothstep(size.x * 0.5 - 10.0, size.x * 0.5 + 30.0, abs(p.x - size.x * 0.5));
            sparks *= column * smoothstep(6.0, -10.0, p.y) * grow * fade * (1.0 + 1.5 * flare);
            col += float3(1.0, 0.62, 0.22) * sparks * 1.6;

            return half4(half3(col), 1.0);
        }
    ";

    private static SKRuntimeEffect? s_fire;
    private static readonly SKRect Outset = new(80, 130, 80, 70);

    public FireButton(string label) : base(label)
    {
        Name = "FireButton";
    }

    protected override SKRect EffectOutset => Outset;

    protected override SKRuntimeEffect Effect => s_fire ??= Compile(FireSource, "Fire");

    protected override SKColor GlowColour => new(0xFF, 0x6A, 0x1A);

    // Catches quickly, dies down a little slower than the black hole evaporates.
    protected override float GrowRate => 3.2f;
    protected override float FadeRate => 3.5f;

    protected override void PaintGlyph(SKCanvas c, float cx, float cy, float radius)
    {
        // A small ember.
        using var paint = new SKPaint { IsAntialias = true };
        paint.Shader = SKShader.CreateRadialGradient(new SKPoint(cx, cy + 1f), radius + 2f,
            new[] { new SKColor(0xFF, 0xF1, 0xB0), new SKColor(0xFF, 0x8A, 0x1C), new SKColor(0xC2, 0x26, 0x0A, 0) },
            new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp);
        c.DrawCircle(cx, cy, radius + 2f, paint);
    }

    protected override void SetUniforms(SKRuntimeEffectUniforms uniforms, SKMatrix m)
    {
        var origin = m.MapPoint(0, 0);
        uniforms["u_origin"] = new[] { origin.X, origin.Y };
        uniforms["u_size"] = new[] { W, H };
        uniforms["u_region"] = new[] { -Outset.Left, -Outset.Top, W + Outset.Right, H + Outset.Bottom };
    }
}
