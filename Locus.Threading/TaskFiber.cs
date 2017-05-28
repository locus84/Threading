using System;
using System.Threading;
using System.Threading.Tasks;


namespace Locus.Threading
{
    public class TaskFiber
    {
        Task tail = Task.CompletedTask;
        Thread m_CurrentThread;
        public bool IsCurrentThread { get { return m_CurrentThread == Thread.CurrentThread; } }

        Task EnqueueInternal(Task newTail)
        {
            var oldTail = Interlocked.Exchange(ref tail, newTail);
            return oldTail.ContinueWith(prev => DoNextTask(newTail), TaskContinuationOptions.ExecuteSynchronously);
        }

        Task<T> EnqueueInternal<T>(Task<T> newTail)
        {
            var oldTail = Interlocked.Exchange(ref tail, newTail);
            return oldTail.ContinueWith(prev => DoNextTaskTyped(newTail), TaskContinuationOptions.ExecuteSynchronously);
        }

        void DoNextTask(Task next)
        {
            m_CurrentThread = Thread.CurrentThread;
            next.RunSynchronously();
            m_CurrentThread = null;
        }

        T DoNextTaskTyped<T>(Task<T> next)
        {
            m_CurrentThread = Thread.CurrentThread;
            next.RunSynchronously();
            m_CurrentThread = null;
            return next.Result;
        }

        /// <summary>
        /// Schedule an action in this TaskFiber
        /// </summary>
        /// <param name="action">Action to Schedule</param>
        /// <returns></returns>
        public Task Enqueue(Action action)
        {
            var newTask = new Task(action);
            return EnqueueInternal(newTask);
        }

        /// <summary>
        /// Schedule a Func<typeparamref name="T"/> in this TaskFiber
        /// </summary>
        /// <typeparam name="T">Return Type</typeparam>
        /// <param name="func">Func<typeparamref name="T"/> to schedule</param>
        /// <returns></returns>
        public Task<T> Enqueue<T>(Func<T> func)
        {
            var newTask = new Task<T>(func);
            return EnqueueInternal(newTask);
        }

        /// <summary>
        /// Schedule a Task in this TaskFiber
        /// </summary>
        /// <param name="task">Task to schedule</param>
        /// <returns></returns>
        public Task Enqueue(Task task)
        {
            return EnqueueInternal(task);
        }

        /// <summary>
        /// Schedule a Task<typeparamref name="T"/> in this TaskFiber
        /// </summary>
        /// <typeparam name="T">Return Type</typeparam>
        /// <param name="task">Task to Schedule</param>
        /// <returns></returns>
        public Task<T> Enqueue<T>(Task<T> task)
        {
            return EnqueueInternal(task);
        }

        internal void EnqueueAwaitableContinuation(Action continuation)
        {
            Enqueue(continuation);
        }

        /// <summary>
        /// Moves execution context to this fiber.
        /// </summary>
        /// <returns>Awaitable struct</returns>
        public IAwaiter IntoFiber()
        {
            return new EnsureInFiber(this);
        }
    }
}
