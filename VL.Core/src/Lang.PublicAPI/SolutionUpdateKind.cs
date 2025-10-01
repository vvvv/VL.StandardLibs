using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Model
{
    public enum SolutionUpdateKind
    {
        AffectPatchEditor = 1,
        AffectSession = 2,
        AddToHistory = 4,
        SkipHistory = 256,
        AffectCompilation = 8,
        AffectRuntime = 16,
        AffectMenu = 32,
        AffectSolutionBrowser = 64,
        AffectOtherUI = 128,

        UpdateUI = AffectSession | AffectPatchEditor | AffectMenu | AffectSolutionBrowser | AffectOtherUI,
        Default = UpdateUI | AffectSession | AddToHistory | AffectCompilation | AffectRuntime,
        DontCompile = Default - AffectCompilation,
        TweakLast = Default - AddToHistory, // will not create new entries in undo histories, but overwrite old ones (used by compiler)
        UpdatePatchEditorOnly = AffectPatchEditor,
        UpdateUIOnly = UpdateUI - AffectSession,
        UpdateUIAndRuntime = AffectPatchEditor | AffectRuntime,
        /// <summary>
        /// To update UI and add change to history for undo, but does not trigger a recompile
        /// </summary>
        UpdateUIAndHistory = UpdateUI | AddToHistory,

        CommitToValue = UpdateUI | AddToHistory,
        DragValue = UpdateUI | SkipHistory | DragValueFlag,

        DragValueFlag = 512,
    }
}
