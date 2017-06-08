using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Locus.Threading
{
    public static class AwaiterExtensions
    {
        public static IAwaiter IntoFiber(this Task task, IFiber fiber = null)
        {
            fiber = fiber ?? ThreadSpecific.Current;
            if(fiber == null) throw new Exception("Thread is not in any fiber");
            return new FiberAwaiter(task, fiber);
        }

        public static IAwaiter IntoFiber(this IFiber fiber)
        {
            return new EnsureInFiber(fiber);
        }

        public static IAwaiter<TResult> IntoFiber<TResult>(this Task<TResult> task, IFiber fiber = null)
        {
            fiber = fiber ?? ThreadSpecific.Current;
            if (fiber == null) throw new Exception("Thread is not in any fiber");
            return new FiberAwaiter<TResult>(task, fiber);
        }
    }

    internal struct EnsureInFiber : IAwaiter
    {
        //if it's completed in the first time, it just continue it's execution
        public bool IsCompleted => m_Fiber.IsCurrentThread;

        IFiber m_Fiber;

        public EnsureInFiber(IFiber fiber)
        {
            m_Fiber = fiber;
        }

        public IAwaiter GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
        {
            m_Fiber.EnqueueAwaitableContinuation(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            m_Fiber.EnqueueAwaitableContinuation(continuation);
        }
    }


    internal struct FiberAwaiter<TResult> : IAwaiter<TResult>
    {
        Task<TResult> m_Task;
        IFiber m_Fiber;

        public FiberAwaiter(Task<TResult> task, IFiber fiber)
        {
            m_Task = task;
            m_Task.ConfigureAwait(false);
            m_Fiber = fiber;
        }

        public bool IsCompleted => m_Task.IsCompleted && m_Fiber.IsCurrentThread;

        public IAwaiter<TResult> GetAwaiter()
        {
            return this;
        }

        public TResult GetResult()
        {
            return m_Task.Result;
        }

        public void OnCompleted(Action continuation)
        {
            if (m_Fiber.IsCurrentThread) continuation();
            else m_Fiber.EnqueueAwaitableContinuation(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (m_Fiber.IsCurrentThread) continuation();
            else m_Fiber.EnqueueAwaitableContinuation(continuation);
        }
    }

    internal struct FiberAwaiter : IAwaiter
    {
        Task m_Task;
        IFiber m_Fiber;

        public FiberAwaiter(Task task, IFiber fiber)
        {
            m_Task = task;
            m_Task.ConfigureAwait(false);
            m_Fiber = fiber;
        }

        public bool IsCompleted => m_Task.IsCompleted && m_Fiber.IsCurrentThread;

        public IAwaiter GetAwaiter()
        {
            return this;
        }

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
        {
            if (m_Fiber.IsCurrentThread) continuation();
            else m_Fiber.EnqueueAwaitableContinuation(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (m_Fiber.IsCurrentThread) continuation();
            else m_Fiber.EnqueueAwaitableContinuation(continuation);
        }
    }
    
    public interface IAwaiter : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }

        void GetResult();

        IAwaiter GetAwaiter();
    }
    
    public interface IAwaiter<out TResult> : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }

        TResult GetResult();

        IAwaiter<TResult> GetAwaiter();
    }
}
