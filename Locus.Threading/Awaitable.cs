using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Locus.Threading
{
    public static class AwaiterExtensions
    {
        public static IAwaitable YieldInFiber(this Task task, TaskFiber fiber)
        {
            return new FiberAwaitable(task, fiber);
        }

        public static IAwaitable<TResult> YieldInFiber<TResult>(this Task<TResult> task, TaskFiber fiber)
        {
            return new FiberAwaitable<TResult>(task, fiber);
        }
    }


    internal struct FiberAwaitable<TResult> : IAwaitable<TResult>
    {
        Task<TResult> m_Task;
        TaskFiber m_Fiber;

        public FiberAwaitable(Task<TResult> task, TaskFiber fiber)
        {
            m_Task = task;
            m_Fiber = fiber;
        }

        public IAwaiter<TResult> GetAwaiter()
        {
            return new FiberAwaiter<TResult>(m_Task.GetAwaiter(), m_Fiber);
        }
    }

    internal struct FiberAwaiter<TResult> : IAwaiter<TResult>
    {
        public bool IsCompleted => m_Awaiter.IsCompleted;

        TaskAwaiter<TResult> m_Awaiter;
        TaskFiber m_Fiber;

        public FiberAwaiter(TaskAwaiter<TResult> awaiter, TaskFiber fiber)
        {
            m_Awaiter = awaiter;
            m_Fiber = fiber;
        }

        public TResult GetResult()
        {
            return m_Awaiter.GetResult();
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

    internal struct FiberAwaitable : IAwaitable
    {
        Task m_Task;
        TaskFiber m_Fiber;

        public FiberAwaitable(Task task, TaskFiber fiber)
        {
            m_Task = task;
            m_Fiber = fiber;
        }

        public IAwaiter GetAwaiter()
        {
            return new FiberAwaiter(m_Task.GetAwaiter(), m_Fiber);
        }
    }


    internal struct FiberAwaiter : IAwaiter
    {
        public bool IsCompleted => m_Awaiter.IsCompleted;

        TaskAwaiter m_Awaiter;
        TaskFiber m_Fiber;

        public FiberAwaiter(TaskAwaiter awater, TaskFiber fiber)
        {
            m_Awaiter = awater;
            m_Fiber = fiber;
        }

        public void GetResult()
        {
            m_Awaiter.GetResult();
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


    public interface IAwaitable
    {
        IAwaiter GetAwaiter();
    }

    public interface IAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        bool IsCompleted { get; }

        void GetResult();
    }

    public interface IAwaitable<out TResult>
    {
        IAwaiter<TResult> GetAwaiter();
    }

    public interface IAwaiter<out TResult> : ICriticalNotifyCompletion, INotifyCompletion
    {
        bool IsCompleted { get; }

        TResult GetResult();
    }

}
