using System;
using System.Threading;
using System.Threading.Tasks;


namespace Locus.Threading
{
    public class TaskFiber
    {
        Task tail = Task.CompletedTask;
        public bool IsRunning = true;
        Thread m_CurrentThread;
        public bool IsCurrentThread { get { return m_CurrentThread == Thread.CurrentThread; } }

        public Task EnsureInFiber()
        {
            return Task.CompletedTask;
        }

        Task EnqueueInternal(Task newTail)
        {
            var oldTail = Interlocked.Exchange(ref tail, newTail);
            return oldTail.ContinueWith(result => DoNextTask(newTail));
        }

        void DoNextTask(Task next)
        {
            m_CurrentThread = Thread.CurrentThread;
            next.RunSynchronously();
            m_CurrentThread = null;
        }

        public async Task Enqueue(Action action)
        {
            var newTask = new Task(action);
            await EnqueueInternal(newTask);
        }

        public async Task<T> Enqueue<T>(Func<T> returnAction)
        {
            var newTask = new Task<T>(returnAction);
            await EnqueueInternal(newTask);
            return newTask.Result;
        }

        internal void EnqueueAwaitableContinuation(Action continuation)
        {
            if (!IsRunning) return;
#pragma warning disable CS4014
            Enqueue(continuation);
#pragma warning restore CS4014
        }

        public IAwaiter IntoFiber()
        {
            return new EnsureInFiber(this);
        }
    }
}
