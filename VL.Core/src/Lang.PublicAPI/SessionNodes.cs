#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive.Disposables;
using VL.Core;
using VL.Core.Commands;
using VL.Core.Logging;
using VL.Lib.IO;

namespace VL.Lang.PublicAPI
{
    // Helper class for VL to avoid a lot of null checks
    public static class SessionNodes
    {
        private static IDevSession? Current => IDevSession.Current;

        public static void Paste(string modelSnippet, PointF location) 
            => Current?.Paste(modelSnippet, location);

        public static ICommandList? DevCommands 
            => Current?.Commands;

        public static void ReportException(Exception e)
            => Current?.ReportException(e);

        public static ISolution CurrentSolution 
            => Current?.CurrentSolution ?? ISolution.Dummy;

        public static void OpenDocument(Path filePath) 
            => Current?.OpenDocument(filePath);

        public static void CloseDocument(Path filePath, bool showDialogIfChanged = true) 
            => Current?.CloseDocument(filePath, showDialogIfChanged);

        public static void OpenDocuments(IEnumerable<Path> filePaths) => Current?.OpenDocuments(filePaths);

        [Obsolete("Please use the overload with the unique id")]
        public static ISolution CloseDocumentOfNode(uint nodeID, bool showDialogIfChanged = true) 
            => Current?.CloseDocumentOfNode(nodeID, showDialogIfChanged) ?? ISolution.Dummy;

        public static ISolution CloseDocumentOfNode(UniqueId nodeID, bool showDialogIfChanged = true) 
            => Current?.CloseDocumentOfNode(nodeID, showDialogIfChanged) ?? ISolution.Dummy;

        [Obsolete("Please use the overload with the unique id")]
        public static void ShowPatchOfNode(uint nodeID)
            => Current?.ShowPatchOfNode(nodeID);

        public static void ShowPatchOfNode(UniqueId nodeID)
            => Current?.ShowPatchOfNode(nodeID);

        public static void ShowPatchOfNode(NodePath nodePath)
            => Current?.ShowPatchOfNode(nodePath);

        public static Keys OneUp => Current?.OneUp ?? Keys.None;

        /// <summary>
        /// Add a message for one frame. 
        /// </summary>
        public static void AddMessage(UniqueId elementId, string message, MessageSeverity severity = MessageSeverity.Warning)
            => IVLRuntime.Current?.AddMessage(new Message(elementId, severity, message, source: LogSource.App));

        /// <summary>
        /// Add a message for one frame. 
        /// </summary>
        public static void AddMessage(Message message)
            => IVLRuntime.Current?.AddMessage(message);

        /// <summary>
        /// Toggle message on and off. Note: you are responsible of turning the message off again! 
        /// </summary>
        [Obsolete("Please use AddPersistentMessage")]
        public static void ToggleMessage(Message message, bool on)
            => IVLRuntime.Current?.TogglePersistentUserRuntimeMessage(message, on);

        /// <summary>
        /// Add a persistent message. Use the returned disposable to remove the message.
        /// </summary>
        public static IDisposable AddPersistentMessage(Message message)
            => IVLRuntime.Current?.AddPersistentMessage(message) ?? Disposable.Empty;
    }
}
#nullable restore