using System.Numerics;

public class Shadow
{
    
    private float _X = 0;
    private float _Y = 0;
    private float _Spread = 0f;
    private SkiaSharp.SKColor _Color = new(0, 0, 0, 0);

    public Vector2 Offset { get => new Vector2(_X, _Y); }

    public float Spread
    {
        get => _Spread;
        set
        {
            _Spread = value;
            //! #render
        }
    }

    public SkiaSharp.SKColor Color
    {
        get { return _Color; }
        set
        {
            _Color = value;
            //! #render
        }
    }

    public Shadow(float X, float Y) {
        _X = X;
        _Y = Y;
    }
}