using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;

namespace VL.Core
{
    public class RuntimeException : Exception
    {
        public RuntimeException(RuntimeException inner, IVLObject instance)
            : base(inner.Original.Message, inner)
        {
            Original = inner.Original;
            Instance = instance;
            NodeContext = instance.Context;
        }

        public RuntimeException(Exception original, IVLObject instance)
            : base(original.Message, original)
        {
            Original = original;
            Instance = instance;
            NodeContext = instance.Context;
        }

        public RuntimeException(Exception original, NodeContext nodeContext)
            : base(original.Message, original)
        {
            Original = original;
            NodeContext = nodeContext;
        }

        public static RuntimeException Create(Exception e, IVLObject instance)
        {
            if (e is RuntimeException)
                return new RuntimeException(e as RuntimeException, instance);
            else
                return new RuntimeException(e, instance);
        }

        Dictionary<uint, object> FLocalValues = new Dictionary<uint, object>();
        public IReadOnlyDictionary<uint, object> LocalValues => FLocalValues;

        public Exception Original { get; private set; }
        public IVLObject Instance { get; }
        public NodeContext NodeContext { get; }

        public void AddValue(uint id, object value)
        {
            FLocalValues[id] = value;
        }
    }

    public enum RuntimeCommand { Restart }
    public class RuntimeCommandException : Exception
    {
        [ThreadStatic]
        public static bool HasBeenThrownAlready;

        [ThreadStatic]
        public static RuntimeCommandException Latest;

        [ThreadStatic]
        static int Shielded;

        public RuntimeCommand RuntimeCommand { get; }

        public RuntimeCommandException(string message, RuntimeCommand command) 
            : base(message)
        {
            HasBeenThrownAlready = true;
            Latest = this;
            RuntimeCommand = command;
        }

        public static void Reset()
        {
            Latest = null;
            HasBeenThrownAlready = false;
        }

        internal static void Complain(string exceptionMessage, RuntimeCommand runtimeCommand)
        {
            if (Shielded != 0) return;
            Console.WriteLine(exceptionMessage);
            throw new RuntimeCommandException(exceptionMessage, runtimeCommand);
        }

        internal static void BeginShield()
        {
            Shielded++;
        }

        internal static void EndShield()
        {
            Shielded--;
        }

        public static IDisposable Shield()
        {
            BeginShield();
            return Disposable.Create(() => EndShield());
        }

    }
}
