using System;
using System.Threading;
using System.Threading.Tasks;


namespace Locus.Threading
{
    public class TaskFiber : IFiber
    {
        Task tail = Task.CompletedTask;
        public bool IsCurrentThread { get { return ThreadSpecific.CurrentIFiber == this; } }

        Task EnqueueInternal(Task newTail)
        {
            var oldTail = Atomic.Swap(ref tail, newTail);
            return oldTail.ContinueWith(prev => DoNextTask(newTail), TaskContinuationOptions.ExecuteSynchronously);
        }

        void DoNextTask(Task next)
        {
            ThreadSpecific.CurrentIFiber = this;
            next.RunSynchronously();
            ThreadSpecific.CurrentIFiber = null;
        }

        T DoNextTaskTyped<T>(Task<T> next)
        {
            ThreadSpecific.CurrentIFiber = this;
            next.RunSynchronously();
            ThreadSpecific.CurrentIFiber = null;
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
            EnqueueInternal(newTask);
            return newTask;
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
            EnqueueInternal(task);
            return task;
        }

        /// <summary>
        /// Moves execution context to this fiber.
        /// </summary>
        /// <returns>Awaitable struct</returns>
        void IFiber.EnqueueAwaitableContinuation(Action action)
        {
            Enqueue(action);
        }
    }
}
