using System;
using SkiaSharp;

namespace Blossom.Testing.Components;

/// <summary>
/// Opens a black hole on hover. The lens bends the live frame under the button (page, grid, its own
/// label): Einstein ring, chromatic split, frame dragging, an accretion disk and gravitational
/// waves. The effect spills <see cref="Reach"/> px past the box; hit-testing stays on the pill.
/// The hole follows the pointer.
/// </summary>
public sealed class EventHorizonButton : ShaderButton
{
    /// <summary>How far (UI px) spacetime bends past the button's box.</summary>
    public const float Reach = 240f;

    private const string LensSource = @"
        uniform shader u_scene;   // the frame under the effect, device px
        uniform float2 u_center;  // singularity, device px
        uniform float u_scale;    // device px per UI px
        uniform float u_p;        // hover progress 0..1
        uniform float u_time;     // seconds since the hole formed
        uniform float u_pulse;    // seconds since the last click, -1 when none
        uniform float u_reach;    // lens falloff radius, device px
        uniform float3 u_accent;

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

        float2 rotate(float2 v, float a) {
            float c = cos(a);
            float s = sin(a);
            return float2(c * v.x - s * v.y, s * v.x + c * v.y);
        }

        // Point-mass lens: where the light reaching offset d left from (UI px from the singularity).
        float2 bend(float2 d, float einstein, float fall) {
            return d * (1.0 - fall * einstein * einstein / max(dot(d, d), 0.0001));
        }

        // Accretion disk in its tilted plane. rgb is emitted light, a is coverage.
        float4 disk(float2 q, float rin, float rout, float grow) {
            float rq = length(q);
            if (rq < rin * 0.85 || rq > rout) return float4(0.0);
            float band = smoothstep(rin * 0.85, rin * 1.2, rq) * (1.0 - smoothstep(rout * 0.55, rout, rq));
            // Keplerian shear: inner gas laps the outer gas, so the clouds wind into spirals.
            float omega = 3.2 * pow(rin / rq, 1.5);
            float gas = fbm(rotate(q, -u_time * omega) * (4.0 / rin));
            float lanes = 0.55 + 0.45 * sin(rq * (9.0 / rin) + gas * 6.0);
            float heat = clamp(1.0 - (rq - rin) / (rout - rin), 0.0, 1.0);
            float3 ember = mix(float3(1.0, 0.36, 0.08), u_accent, 0.45);
            float3 col = mix(ember, float3(1.0, 0.94, 0.84), heat * heat);
            // Relativistic beaming: gas moving toward the viewer (left side) is brighter and bluer.
            float toward = -q.x / max(rq, 0.001);
            float beam = pow(1.0 + 0.6 * toward, 2.4);
            col = mix(col, col * float3(0.75, 0.9, 1.3), max(toward, 0.0) * 0.6);
            float a = band * (0.35 + 0.9 * gas * lanes) * grow;
            return float4(col * beam, clamp(a * (0.6 + 0.4 * beam), 0.0, 1.0));
        }

        half4 main(float2 frag) {
            float2 d = (frag - u_center) / u_scale;
            float r = length(d);
            float2 dir = d / max(r, 0.001);
            float reach = u_reach / u_scale;
            float grow = u_p * u_p * (3.0 - 2.0 * u_p);

            float rs = 18.0 * grow;       // shadow radius
            float einstein = 2.3 * rs;    // Einstein ring radius
            float fall = 1.0 - smoothstep(reach * 0.3, reach, r);

            // Gravitational waves: one ring as the hole forms, a stronger one per click.
            float front = u_time * 260.0;
            float wave = 7.0 * sin((r - front) * 0.09) * exp(-sq((r - front) / 46.0)) * exp(-u_time * 1.4);
            if (u_pulse >= 0.0) {
                float pf = u_pulse * 340.0;
                wave += 18.0 * sin((r - pf) * 0.07) * exp(-sq((r - pf) / 60.0)) * exp(-u_pulse * 1.1);
            }
            wave *= 1.0 - smoothstep(reach * 0.55, reach, r);

            // Frame dragging: space near a spinning hole is twisted along with it.
            float twist = fall * grow * 2.2 * sq(einstein / (r + einstein + 0.001)) * (1.0 + 0.25 * sin(u_time * 0.9));
            float2 dw = rotate(d + dir * wave, twist);

            // Dispersion: each channel bends a little differently, so the Einstein ring splits into a rainbow.
            float k = 0.07 * grow;
            half4 g = u_scene.eval(u_center + bend(dw, einstein, fall) * u_scale);
            float3 col = float3(
                u_scene.eval(u_center + bend(dw, einstein * (1.0 + k), fall) * u_scale).r,
                g.g,
                u_scene.eval(u_center + bend(dw, einstein * (1.0 - k), fall) * u_scale).b);
            col *= 1.0 - 0.4 * fall * grow * exp(-r / (einstein * 2.5 + 0.001));

            if (grow > 0.001) {
                float rin = rs * 1.6;
                float rout = rs * 4.8;
                const float tilt = 0.2;

                // Far side of the disk, bent over and under the shadow (the Interstellar halo).
                float2 src = bend(d, einstein, fall);
                float4 halo = disk(float2(src.x, src.y / tilt), rin, rout, grow);
                halo.a *= smoothstep(rs * 0.3, rs * 1.2, abs(d.y));
                col = mix(col, halo.rgb, halo.a);

                // Disk seen directly: the back half goes behind the shadow, the front half crosses it.
                float4 seen = disk(float2(d.x, d.y / tilt), rin, rout, grow);
                if (d.y < 0.0) col = mix(col, seen.rgb, seen.a);

                float aa = 0.8 / u_scale;
                float hole = 1.0 - smoothstep(rs - aa, rs + aa, r);
                col *= 1.0 - hole;

                // Photon ring: light that orbited the hole before escaping.
                float ring = exp(-sq((r - rs * 1.06) / (0.9 + rs * 0.05))) * grow;
                col += mix(float3(1.0), u_accent, 0.3) * ring * 1.4 * (1.0 - 0.5 * dir.x);

                if (d.y >= 0.0) col = mix(col, seen.rgb, seen.a);

                col += u_accent * 0.25 * grow * exp(-max(r - rs, 0.0) / (rs * 1.6 + 1.0)) * (1.0 - hole);
            }

            return half4(half3(col), 1.0);
        }
    ";

    private static SKRuntimeEffect? s_lens;

    private SKPoint _focus, _aim;
    private bool _spawn;        // the next pointer position places a new hole

    public EventHorizonButton(string label) : base(label)
    {
        Name = "EventHorizonButton";
        Events.OnMouseMove += (_, e) =>
        {
            _aim = Aim(e.Relative.X, e.Relative.Y);
            if (_spawn) { _focus = _aim; _spawn = false; }
        };
    }

    protected override SKRect EffectOutset => new(Reach, Reach, Reach, Reach);

    protected override SKRuntimeEffect Effect => s_lens ??= Compile(LensSource, "Event horizon");

    // A new hole forms where the pointer enters.
    protected override void OnEffectStarted() => _spawn = true;

    protected override void OnEffectUpdate(float dt)
    {
        float follow = 1f - MathF.Exp(-dt * 7f);
        _focus = new SKPoint(_focus.X + (_aim.X - _focus.X) * follow, _focus.Y + (_aim.Y - _focus.Y) * follow);
    }

    private SKPoint Aim(float x, float y)
    {
        // Track the pointer across the pill; damp the vertical so the disk stays inside the label band.
        float r = H / 2f;
        return new SKPoint(Math.Clamp(x, r * 0.6f, W - r * 0.6f), r + (Math.Clamp(y, 0, H) - r) * 0.4f);
    }

    protected override void PaintGlyph(SKCanvas c, float cx, float cy, float radius)
    {
        // A tiny hole with its ring.
        using var paint = new SKPaint { IsAntialias = true, Color = Accent.WithAlpha(110) };
        c.DrawCircle(cx, cy, radius + 2f, paint);
        paint.Color = SKColors.Black;
        c.DrawCircle(cx, cy, radius - 1f, paint);
    }

    protected override void SetUniforms(SKRuntimeEffectUniforms uniforms, SKMatrix m)
    {
        var center = m.MapPoint(_focus);
        uniforms["u_center"] = new[] { center.X, center.Y };
        uniforms["u_reach"] = Reach * m.ScaleX;
        uniforms["u_accent"] = new[] { Accent.Red / 255f, Accent.Green / 255f, Accent.Blue / 255f };
    }
}
