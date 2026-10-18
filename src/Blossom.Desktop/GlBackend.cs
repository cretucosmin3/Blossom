using System;
using Silk.NET.GLFW;
using Silk.NET.Windowing;

namespace Blossom.Desktop;

/// <summary>
/// Picks the OpenGL flavour for the window. Desktop GL 3.2 core where the driver has it; otherwise
/// OpenGL ES, which low-end Linux GPUs do have (Raspberry Pi 4/5: GL 3.1 and GLES 3.1; Pi 3: GL 2.1
/// and GLES 2.0). Skia renders on any of them.
/// <para>
/// Set <c>BLOSSOM_GL</c> to <c>gl</c>, <c>gles3</c> or <c>gles2</c> to force one.
/// </para>
/// </summary>
internal static class GlBackend
{
    internal enum Kind { DesktopGl, Gles3, Gles2 }

    internal static Kind Current { get; private set; } = Kind.DesktopGl;

    internal static bool IsGles => Current != Kind.DesktopGl;

    /// <summary>GLES 2.0 has no glBlitFramebuffer.</summary>
    internal static bool CanBlit => Current != Kind.Gles2;

    /// <summary>Call before any other GLFW window hint is set: probing resets the hints.</summary>
    internal static GraphicsAPI Choose()
    {
        Current = Forced() ?? Probe();
        Log.Info($"OpenGL backend: {Current}");
        return Current switch
        {
            Kind.Gles3 => new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(3, 0)),
            Kind.Gles2 => new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(2, 0)),
            _ => new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 2)),
        };
    }

    private static Kind? Forced()
    {
        switch (Environment.GetEnvironmentVariable("BLOSSOM_GL")?.Trim().ToLowerInvariant())
        {
            case null or "": return null;
            case "gl": return Kind.DesktopGl;
            case "gles3": return Kind.Gles3;
            case "gles2": return Kind.Gles2;
            case var other:
                Log.Warning($"Ignoring BLOSSOM_GL={other} (use gl, gles3 or gles2).");
                return null;
        }
    }

    private static Kind Probe()
    {
        // Windows and macOS drivers always offer GL 3.2 core, and have no GLES to fall back to.
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
            return Kind.DesktopGl;

        foreach (var kind in new[] { Kind.DesktopGl, Kind.Gles3, Kind.Gles2 })
        {
            if (CanCreate(kind))
                return kind;
        }
        Log.Warning("No OpenGL 3.2, GLES 3.0 or GLES 2.0 context could be created; trying OpenGL 3.2 anyway.");
        return Kind.DesktopGl;
    }

    /// <summary>Creates and destroys a hidden 1x1 window with that context.</summary>
    private static unsafe bool CanCreate(Kind kind)
    {
        var glfw = GlfwProvider.GLFW.Value;
        // A refused context is expected here; keep it out of Silk's error queue and the log.
        GlfwCallbacks.ErrorCallback quiet = (_, _) => { };
        glfw.SetErrorCallback(quiet);
        try
        {
            glfw.DefaultWindowHints();
            glfw.WindowHint(WindowHintBool.Visible, false);
            if (kind == Kind.DesktopGl)
            {
                glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGL);
                glfw.WindowHint(WindowHintInt.ContextVersionMajor, 3);
                glfw.WindowHint(WindowHintInt.ContextVersionMinor, 2);
                glfw.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
                glfw.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
            }
            else
            {
                glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGLES);
                glfw.WindowHint(WindowHintInt.ContextVersionMajor, kind == Kind.Gles3 ? 3 : 2);
                glfw.WindowHint(WindowHintInt.ContextVersionMinor, 0);
            }

            var probe = glfw.CreateWindow(1, 1, "", null, null);
            if (probe == null)
                return false;
            glfw.DestroyWindow(probe);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning($"OpenGL probe for {kind} failed: {ex.Message}");
            return false;
        }
        finally
        {
            glfw.DefaultWindowHints();
            glfw.SetErrorCallback(Glfw.ErrorCallback);
            GC.KeepAlive(quiet);
        }
    }
}
