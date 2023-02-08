using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib
{
    public class ManageProcess<TState, TOutput> : IDisposable
    {
        TState FState;
        TOutput FLastOutput;

        public TOutput Update(Func<TState> create, Func<TState, Tuple<TState, TOutput>> update, bool enabled = true, bool reset = false)
        {
            if (FState == null || reset)
            {
                Dispose();
                FState = create();
            }

            if (enabled)
            {
                var t = update(FState);
                FState = t.Item1;
                FLastOutput = t.Item2;
                return FLastOutput;
            }
            else
                return FLastOutput;
        }

        public void Dispose()
        {
            DisposeInternalState();
        }

        private void DisposeInternalState()
        {
            try
            {
                (FState as IDisposable)?.Dispose();
            }
            finally
            {
                FState = default(TState);
            }
        }
    }

    public class ManageProcess2<TState, TOutput, TOutput2> : IDisposable
    {
        TState FState;
        Tuple<TOutput, TOutput2> FLastOutput;

        public Tuple<TOutput, TOutput2> Update(Func<TState> create, Func<TState, Tuple<TState, TOutput, TOutput2>> update, bool enabled = true, bool reset = false)
        {
            if (FState == null || reset)
            {
                DisposeInternalState();
                FState = create();
            }

            if (enabled)
            {
                var t = update(FState);
                FState = t.Item1;
                FLastOutput = new Tuple<TOutput, TOutput2>(t.Item2, t.Item3);
                return FLastOutput;
            }
            else
                return FLastOutput;
        }

        public void Dispose()
        {
            DisposeInternalState();
        }

        private void DisposeInternalState()
        {
            try
            {
                (FState as IDisposable)?.Dispose();
            }
            finally
            {
                FState = default(TState);
            }
        }
    }
}
