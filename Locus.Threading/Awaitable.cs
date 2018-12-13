using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Locus.Threading
{
    public static class AwaiterExtensions
    {
        /// <summary>
        /// Return AwaitableObject
        /// </summary>
        /// <param name="task">task</param>
        /// <param name="fiber">fiber to continue</param>
        /// <returns></returns>
        public static FiberAwaiter ContinueIn(this Task task, MessageFiberBase fiber)
        {
            if (fiber == null) throw new Exception("target fiber is empty");
            return new FiberAwaiter(task, fiber);
        }

        /// <summary>
        /// Return AwaitableObject
        /// </summary>
        /// <typeparam name="TResult">Result Type</typeparam>
        /// <param name="task">task</param>
        /// <param name="fiber">fiber to continue</param>
        /// <returns></returns>
        public static FiberAwaiter<TResult> ContinueIn<TResult>(this Task<TResult> task, MessageFiberBase fiber)
        {
            if (fiber == null) throw new Exception("target fiber is empty");
            return new FiberAwaiter<TResult>(task, fiber);
        }

        public static IAwaiter GetAwaiter(this MessageFiberBase fiber) => new EnsureInFiber(fiber);

        public static IAwaiter GetAwaiter(this FiberAwaiter awaiter) => awaiter;

        public static IAwaiter<TResult> GetAwaiter<TResult>(this FiberAwaiter<TResult> awaiter) => awaiter;
    }

    /// <summary>
    /// This struct is needed to gen into fiber's execution chain
    /// </summary>
    public struct EnsureInFiber : IAwaiter
    {
        //if it's completed in the first time, it just continue it's execution
        public bool IsCompleted => m_Fiber.IsCurrentThread;

        MessageFiberBase m_Fiber;

        public EnsureInFiber(MessageFiberBase fiber)
        {
            m_Fiber = fiber;
        }

        public void GetResult()
        {
        }

        public void OnCompleted(Action continuation)
        {
            MessageFiberBase.EnqueueInternal(m_Fiber, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            MessageFiberBase.EnqueueInternal(m_Fiber, continuation);
        }
    }


    public struct FiberAwaiter<TResult> : IAwaiter<TResult>
    {
        Task<TResult> m_Task;
        MessageFiberBase m_Fiber;

        public FiberAwaiter(Task<TResult> task, MessageFiberBase fiber)
        {
            m_Task = task;
            m_Task.ConfigureAwait(false);
            m_Fiber = fiber;
        }

        public bool IsCompleted => m_Task.IsCompleted && m_Fiber.IsCurrentThread;

        public TResult GetResult() => m_Task.Result;

        public void OnCompleted(Action continuation)
        {
            if (m_Fiber.IsCurrentThread) continuation();
            else MessageFiberBase.EnqueueInternal(m_Fiber, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (m_Fiber.IsCurrentThread) continuation();
            else MessageFiberBase.EnqueueInternal(m_Fiber, continuation);
        }
    }

    public struct FiberAwaiter : IAwaiter
    {
        Task m_Task;
        MessageFiberBase m_Fiber;

        public FiberAwaiter(Task task, MessageFiberBase fiber)
        {
            m_Task = task;
            m_Task.ConfigureAwait(false);
            m_Fiber = fiber;
        }

        public bool IsCompleted => m_Task.IsCompleted && m_Fiber.IsCurrentThread;

        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            if (m_Fiber.IsCurrentThread) continuation();
            else MessageFiberBase.EnqueueInternal(m_Fiber, continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            if (m_Fiber.IsCurrentThread) continuation();
            else MessageFiberBase.EnqueueInternal(m_Fiber, continuation);
        }
    }
    
    public interface IAwaiter : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }

        void GetResult();
    }
    
    public interface IAwaiter<out TResult> : ICriticalNotifyCompletion
    {
        bool IsCompleted { get; }

        TResult GetResult();
    }
}
