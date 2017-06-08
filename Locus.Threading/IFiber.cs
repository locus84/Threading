using System;
using System.Collections.Generic;
using System.Text;

namespace Locus.Threading
{
    public interface IFiber
    {
        bool IsCurrentThread { get; }
        void EnqueueAwaitableContinuation(Action action);
    }

    internal static class ThreadSpecific
    {
        [ThreadStatic]
        internal static IFiber Current;
    }
}
