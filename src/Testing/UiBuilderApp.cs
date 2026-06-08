using System;
using Blossom.Core;
using Blossom.Testing.Views;

namespace Blossom.Testing;

public class UiBuilderApplication : Application
{
    private readonly UiBuilderView _builderView;

    public UiBuilderApplication()
    {
        _builderView = new UiBuilderView();
        AddView(_builderView);
        SetActiveView(_builderView);
    }
}
