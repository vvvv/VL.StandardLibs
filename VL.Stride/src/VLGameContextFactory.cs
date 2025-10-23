using Stride.Games;
using System;
using VL.Core;

namespace VL.Stride;

internal class VLGameContextFactory
{
    public static GameContext CreateContext(
        NodeContext nodeContext, 
        bool alwaysOnTop, 
        bool extendIntoTitleBar, 
        AppContextType appContextType, 
        int requestedWidth = 0, 
        int requestedHeight = 0, 
        bool isUserManagingRun = false)
    {
        if (appContextType == AppContextType.Desktop && OperatingSystem.IsWindows())
        {
            return new GameContextWinforms(
                new VLGameForm(nodeContext, new(alwaysOnTop, extendIntoTitleBar)),
                requestedWidth,
                requestedHeight,
                isUserManagingRun);
        }
        return GameContextFactory.NewGameContext(appContextType, requestedWidth, requestedHeight, isUserManagingRun);
    }
}
