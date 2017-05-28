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

        public Task Enqueue(Action action)
        {
            var newTask = new Task(action);
            return EnqueueInternal(newTask);
        }

        public Task<T> Enqueue<T>(Func<T> returnAction)
        {
            var newTask = new Task<T>(returnAction);
            return EnqueueInternal(newTask);
        }

        internal void EnqueueAwaitableContinuation(Action continuation)
        {
            Enqueue(continuation);
        }

        public IAwaiter IntoFiber()
        {
            return new EnsureInFiber(this);
        }
    }
}
