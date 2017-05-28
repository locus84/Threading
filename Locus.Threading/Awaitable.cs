using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Locus.Threading
{
    public static class AwaiterExtensions
    {
        public static IAwaiter IntoFiber<T>(this Task task, MessageFiber<T> fiber)
        {
            return new FiberAwaiter(task, fiber);
        }

        public static IAwaiter<TResult> IntoFiber<TResult, T>(this Task<TResult> task, MessageFiber<T> fiber)
        {
            return new FiberAwaiter<TResult>(task, fiber);
        }

        public static IAwaiter IntoFiber<T>(this MessageFiber<T> fiber)
        {
            return new EnsureInFiber(fiber);
        }

        public static IAwaiter IntoFiber(this Task task, TaskFiber fiber)
        {
            return new FiberAwaiter(task, fiber);
        }

        public static IAwaiter<TResult> IntoFiber<TResult>(this Task<TResult> task, TaskFiber fiber)
        {
            return new FiberAwaiter<TResult>(task, fiber);
        }

        public static IAwaiter IntoFiber(this TaskFiber fiber)
        {
            return new EnsureInFiber(fiber);
        }
    }

    internal struct EnsureInFiber : IAwaiter
    {
        //if it's completed in the first time, it just continue it's execution
        public bool IsCompleted => m_Fiber.IsCurrentThread? true : false;

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

    internal interface IFiber
    {
        bool IsCurrentThread { get; }
        void EnqueueAwaitableContinuation(Action action);
    }
}
