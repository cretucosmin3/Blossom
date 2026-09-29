using System;

namespace Blossom.Reactive;

/// <summary>
/// Internal contract representing a reactive signal dependency.
/// </summary>
internal interface ISignal
{
    void Subscribe(Effect effect);
    void Unsubscribe(Effect effect);
}
