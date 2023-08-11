#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using VL.Core;
using VL.Lib.IO;

namespace VL.Lang.PublicAPI
{
    // longterm we need a central place where to start from. it's super chaotic right now.
    // it's an interface to give us a higher degree of freedom 
    // * where to implement (e.g. in a ui library) vl.ui.forms
    // * and in the way we design the hopefully accessible and easy to use api
    // it might also need to get moved to VL.Core at some point, but for now this doesn't make
    // too much sense. Also in the interface you need access to the one or other type that currently
    // resides in VL.Lang.
    // maybe at some point it's not so much about moving everything to VL.Core and doing the abstraction
    // there. we could do the same by moving some concrete implementations outof VL.Lang and only keep
    // basic astractions inside VL.Lang.
    // so let's start here from scratch and keep it organised.

    // * THIS DOCUMENT SHOULDN'T BE CHANGED WITHOUT CONSENT

    // * in it's current form there is no consent yet. THIS IS A DRAFT

    // * TODO: discuss PointF / Vector2

    ///// <summary>
    ///// Provides access to the session that covers everythign from vl source file handling,
    ///// tabs and canvases, selections
    ///// </summary>
    public interface IDevSession
    {
        public static IDevSession? Current => AppHost.CurrentOrGlobal.Services.GetService<IDevSession>();

        void Paste(string modelSnippet, PointF location);

        void ReportException(Exception e);

        // From static Session
        ISolution CurrentSolution { get; }

        void OpenDocument(Path filePath);

        void CloseDocument(Path filePath, bool showDialogIfChanged = true);

        void OpenDocuments(IEnumerable<Path> filePaths);

        [Obsolete("Please use the overload with the unique id")]
        ISolution CloseDocumentOfNode(uint nodeID, bool showDialogIfChanged = true);

        ISolution CloseDocumentOfNode(UniqueId nodeID, bool showDialogIfChanged = true);

        [Obsolete("Please use the overload with the unique id")]
        void ShowPatchOfNode(uint nodeID);

        void ShowPatchOfNode(UniqueId nodeID);

        // HACK: Used by Renderer [Skia] node only, we should be able to get rid of it once we have some sort of unified view over our windows
        Keys OneUp { get; }
    }
}
#nullable restore