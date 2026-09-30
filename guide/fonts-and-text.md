# Fonts and text

SkiaSharp 4: measure, break, and draw text on **`SKFont`**, not `SKPaint`. `new SKFont()` has an empty typeface (zero-width glyphs) until you pass a face.

`SKTypeface.FromFamilyName` still does not fail on a missing family. Blossom does not cache a mismatched face under the name you asked for.

## Registry

```csharp
SKTypeface face = Fonts.RegisterFromFile("DejaVuSansMono.ttf");
Fonts.RegisterFromStream(stream, cacheKey: "DejaVuSansMono");
Fonts.RegisterFromData(bytes);

if (!Fonts.TryGetExact("DejaVuSansMono", out var exact))
    throw new InvalidOperationException("face not registered");

SKFont font = Fonts.CreateFont(exact, 14f);
SKFont also = Fonts.CreateFont("DejaVuSansMono", 14f); // registered first, then MatchFamily
V2 size = Fonts.Measure(font, "hello");
```

Search order for `GetTypeface` / `CreateFont`: **registered faces**, then `SKFontManager.Default.MatchFamily`. Linux native assets include fontconfig (`SkiaSharp.NativeAssets.Linux`).

`Fonts.ResolveForCodepoint` / `HasGlyph` pick a fallback for a missing glyph. Color emoji need a registered COLR/CPAL face (Noto/Twemoji or similar); v4 can draw palettes once that face is present. Bundled Roboto has no emoji and few symbols — demo labels stay ASCII unless you register more faces.

## TextLayout (caret)

`TextLayout` is the public measure/draw/caret service a field or terminal should use. Build with `SKFont`. Then:

```csharp
int index = layout.CaretIndexFromPoint(localX, localY);
SKRect caret = layout.CaretRect(index);
int next = layout.MoveByGrapheme(index, +1);
int word = layout.MoveByWord(index, -1);
int n = layout.GraphemeCount;
```

One width for drawing and caret. The demo `InputField` uses these APIs. A custom password field is policy (mask, paste, max length) on top of `TextLayout` + keyboard + pointer capture + clipboard.

Blossom will ship a field primitive later on this same surface.
