using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.IO.Redis.Internal
{
    internal static class MessageExtensions
    {
        public static IDisposable AddException(this IVLRuntime runtime, NodeContext nodeContext, Exception exception)
        {
            var warnings = new CompositeDisposable();
            foreach (var id in nodeContext.Path.Stack.SkipLast(1))
            {
                warnings.Add(runtime.AddPersistentMessage(new Lang.Message(id, Lang.MessageSeverity.Warning, exception.Message)));
            }
            return warnings;
        }
    }
}
