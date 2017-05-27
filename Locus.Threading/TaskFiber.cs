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
            next.RunSynchronously(TaskScheduler.Default);
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
#pragma warning disable CS4014 // 이 호출을 대기하지 않으므로 호출이 완료되기 전에 현재 메서드가 계속 실행됩니다.
            Enqueue(continuation);
#pragma warning restore CS4014 // 이 호출을 대기하지 않으므로 호출이 완료되기 전에 현재 메서드가 계속 실행됩니다.
        }
    }
}
