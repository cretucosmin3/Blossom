using System;
using System.Diagnostics;
using System.Numerics;

namespace Blossom.Testing.Components;

/// <summary>
/// Gravity hang for a dragged card about the grab pivot (transforms only).
/// <para>
/// Mass at the geometric center; pivot is the grab. Gravity dominates the free-end drop.
/// Hand motion only adds a <b>soft</b> opposite lag (drag right → free end trails left),
/// with dead-zones so small mouse jitter does not thrash the card.
/// </para>
/// </summary>
public class CardDragPhysics
{
    // --- Gravity (primary feel) ---
    // Strong enough that a corner grab falls into a hang / flip without a violent flick.
    private const float Gravity = 9800f;

    // --- Hand lag (secondary; deliberately mild) ---
    private const float InertiaScale = 0.28f;
    private const float VelocityLag = 1.6f;

    // Ignore tiny mouse noise; map the rest through a soft curve.
    private const float VelDeadzone = 90f;       // px/s
    private const float VelSoftMax = 900f;       // px/s → full lag contribution
    private const float AccelDeadzone = 2500f;   // px/s²
    private const float AccelSoftMax = 18000f;

    // Cap hand-driven angular accel so flicks stay controllable (deg/s²).
    private const float MaxHandAlphaDeg = 2800f;

    // --- Damping ---
    private const float Damping = 1.8f;
    private const float HangDamping = 4.5f;

    private const float MinArmPx = 8f;

    private float _theta;
    private float _omega;
    private float _originX = 0.5f;
    private float _originY = 0.5f;

    private float _comX;
    private float _comY;
    private float _armLen;
    private float _inertia;

    private Vector2 _lastPos;
    private Vector2 _velocity;
    private Vector2 _accel;
    private readonly Stopwatch _timer = new();
    private bool _hasLastPos;

    public float RotationZ => _theta;
    public float OriginX => _originX;
    public float OriginY => _originY;

    public void Start(float grabOffsetX, float grabOffsetY, float cardWidth, float cardHeight, Vector2 startPos)
    {
        cardWidth = Math.Max(1f, cardWidth);
        cardHeight = Math.Max(1f, cardHeight);

        _originX = Math.Clamp(grabOffsetX / cardWidth, 0.02f, 0.98f);
        _originY = Math.Clamp(grabOffsetY / cardHeight, 0.02f, 0.98f);

        _comX = (0.5f - _originX) * cardWidth;
        _comY = (0.5f - _originY) * cardHeight;
        _armLen = MathF.Sqrt(_comX * _comX + _comY * _comY);

        // Slightly lighter self-inertia so long corner arms actually flip under gravity.
        float self = (cardWidth * cardWidth + cardHeight * cardHeight) * 0.012f;
        _inertia = Math.Max(_armLen * _armLen + self, 400f);

        _theta = 0f;
        _omega = 0f;

        _lastPos = startPos;
        _velocity = Vector2.Zero;
        _accel = Vector2.Zero;
        _hasLastPos = true;
        _timer.Restart();
    }

    public void OnDragMove(Vector2 currentPos)
    {
        float dt = ConsumeDt();
        if (_hasLastPos)
        {
            Vector2 delta = currentPos - _lastPos;
            Vector2 instant = delta / Math.Max(dt, 1e-4f);

            // Heavy smooth on velocity so pixel jitter does not read as a whip.
            _velocity = Vector2.Lerp(_velocity, instant, Math.Min(1f, dt * 10f));

            Vector2 rawAccel = (instant - _velocity) / Math.Max(dt, 1e-4f);
            _accel = Vector2.Lerp(_accel, rawAccel, Math.Min(1f, dt * 8f));
        }
        _lastPos = currentPos;
        _hasLastPos = true;
        Step(dt);
    }

    public void StepFrame()
    {
        float dt = ConsumeDt();
        _accel = Vector2.Lerp(_accel, Vector2.Zero, Math.Min(1f, dt * 10f));
        _velocity = Vector2.Lerp(_velocity, Vector2.Zero, Math.Min(1f, dt * 8f));
        Step(dt);
    }

    public void Reset()
    {
        _theta = 0f;
        _omega = 0f;
        _originX = 0.5f;
        _originY = 0.5f;
        _comX = _comY = 0f;
        _armLen = 0f;
        _inertia = 400f;
        _velocity = Vector2.Zero;
        _accel = Vector2.Zero;
        _hasLastPos = false;
        _timer.Reset();
    }

    private float ConsumeDt()
    {
        float dt = (float)_timer.Elapsed.TotalSeconds;
        _timer.Restart();
        if (dt <= 1e-4f || dt > 0.05f) dt = 1f / 60f;
        return dt;
    }

    /// <summary>
    /// Dead-zone + soft remap so small motion ≈ 0 and moderate motion reaches full effect
    /// without needing extreme flicks.
    /// </summary>
    private static Vector2 SoftenHand(Vector2 v, float deadzone, float softMax)
    {
        float len = v.Length();
        if (len < deadzone || softMax <= deadzone)
            return Vector2.Zero;

        float t = (len - deadzone) / (softMax - deadzone);
        t = Math.Clamp(t, 0f, 1f);
        // Ease-in: gentle at first, full by softMax (not a linear hypersensitive ramp).
        t = t * t;
        float outLen = t * softMax;
        return v * (outLen / len);
    }

    private void Step(float dt)
    {
        if (_armLen < MinArmPx)
        {
            _omega *= MathF.Exp(-18f * dt);
            _theta *= MathF.Exp(-18f * dt);
            if (MathF.Abs(_theta) < 0.2f && MathF.Abs(_omega) < 2f)
            {
                _theta = 0f;
                _omega = 0f;
            }
            return;
        }

        float rad = _theta * (MathF.PI / 180f);
        float cos = MathF.Cos(rad);
        float sin = MathF.Sin(rad);
        float wx = _comX * cos - _comY * sin;
        float wy = _comX * sin + _comY * cos;

        // 1) Gravity dominates — free end falls under the pivot on its own.
        float torque = wx * Gravity;

        // 2) Soft hand lag (opposite direction): drag right → free end trails left.
        Vector2 handVel = SoftenHand(_velocity, VelDeadzone, VelSoftMax);
        Vector2 handAcc = SoftenHand(_accel, AccelDeadzone, AccelSoftMax);

        float handTorque =
            (wy * handAcc.X - wx * handAcc.Y) * InertiaScale
            + (wy * handVel.X - wx * handVel.Y) * VelocityLag;

        // Convert hand torque to alpha and clamp so mouse cannot overpower gravity entirely.
        float handAlphaDeg = (handTorque / _inertia) * (180f / MathF.PI);
        handAlphaDeg = Math.Clamp(handAlphaDeg, -MaxHandAlphaDeg, MaxHandAlphaDeg);

        float gravAlphaDeg = (torque / _inertia) * (180f / MathF.PI);
        float alphaDeg = gravAlphaDeg + handAlphaDeg;

        _omega += alphaDeg * dt;

        float damp = Damping;
        bool hanging =
            wy > 0f
            && MathF.Abs(wx) < Math.Max(4f, _armLen * 0.1f);
        if (hanging)
            damp += HangDamping;

        _omega *= MathF.Exp(-damp * dt);
        _theta += _omega * dt;

        // Only snap when truly still — don't fight an intentional swing/flip mid-motion.
        if (hanging
            && MathF.Abs(_omega) < 8f
            && handVel.LengthSquared() < 1f
            && handAcc.LengthSquared() < 1f)
        {
            float localComDeg = MathF.Atan2(_comY, _comX) * (180f / MathF.PI);
            float hangDeg = 90f - localComDeg;
            float turns = MathF.Round((_theta - hangDeg) / 360f);
            _theta = hangDeg + turns * 360f;
            _omega = 0f;
        }

        if (MathF.Abs(_theta) > 4000f)
            _theta = WrapTo180(_theta);
    }

    private static float WrapTo180(float deg)
    {
        deg %= 360f;
        if (deg > 180f) deg -= 360f;
        if (deg < -180f) deg += 360f;
        return deg;
    }
}
