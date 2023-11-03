using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.IO.Redis
{
    internal static class MessageExtensions
    {
        internal static void AddExeption(this CompositeDisposable warnings, string message,  Exception ex, NodeContext nodeContext, IVLRuntime runtime)
        {
            if (runtime != null && warnings.Count == 0)
            {
                foreach (var id in nodeContext.Path.Stack.SkipLast(1))
                {
                    warnings.Add(runtime.AddPersistentMessage(new VL.Lang.Message(id, Lang.MessageSeverity.Error, message + Environment.NewLine + ex.Message)));
                }
            }
        }
    }
}
