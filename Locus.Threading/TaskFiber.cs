using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;


namespace Locus.Threading
{
    public class TaskFiber// : ITaskFiber
    {
        Task tail;
        WaitCallback callback;
        Thread currentThread;
        public Action<Exception> OnException;
        public bool IsSameThread{ get { return currentThread == Thread.CurrentThread; } }

        public Coroutine StartCoroutine(IEnumerator enumrator)
        {
            var coroutine = new Coroutine(enumrator, this);
            return coroutine;
        }

        public static void StopCoroutine(Coroutine coroutine)
        {
            coroutine.Stop();
        }

        protected virtual void OnFiberExeption(Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }

       
        public TaskFiber()
        {
            OnException = (e) => OnFiberExeption(e);
            callback = (obj) => DoCallback((Task)obj);
        }

        void DoCallback(Task runHead)
        {
            var thread = Thread.CurrentThread;
            Task next;
            while(true)
            {
                currentThread = thread;
                runHead.StartInSameThread();
                if(runHead.Error != null)
                {
                    if(OnException != null)
                        OnException(runHead.Error);
                    else
                    {
                        Console.WriteLine(runHead.Error.Message);
                        Console.WriteLine(runHead.Error.StackTrace);
                    }
                }
                currentThread = null;
                next = runHead.GetNext();
                if (next == null)
                {
                    break;
                }
                runHead.Return();
                runHead = next;
            }
        }

        void Enqueue(Task newTail)
        {
            var oldTail = Interlocked.Exchange(ref tail, newTail);
            if (oldTail == null)
            {
                ThreadPool.QueueUserWorkItem(callback, newTail);
                return;
            }
            else if (oldTail.TryTail(newTail))
                return;
            else
            {
                oldTail.Return();
                ThreadPool.QueueUserWorkItem(callback, newTail);
            }
        }


        public void Enqueue(Action action)
        {
            //create a task that has not been started
            if (action == null)
                throw new Exception("Task is null");
            var newTask = Task.GetFromPoolOrCreate(action);
            newTask.MarkAsReturnable();
            Enqueue(newTask);
        }

        public Task EnqueueWait(Action action)
        {
            if (action == null)
                throw new Exception("Task is null");
            //create a task that has not been started
            var newTask = Task.GetFromPoolOrCreate(action);
            Enqueue(newTask);
            return newTask;
        }

        public Task<T> Enqueue<T>(Func<T> returnAction)
        {
            if (returnAction == null)
                throw new Exception("Task is null");
            //create a task that has not been started
            var newTask = new Task<T>(returnAction);
            Enqueue(newTask);
            return newTask;
        }

        public IDisposable Schedule(Action action, float time)
        {
            var timeMs = (long)(time * 1000);
            return Schedule(action, timeMs);
        }

        public IDisposable Schedule(Action action, int timeMs)
        {
            ScheduleTimer schedule = null;
            //duetime is important.
            schedule = new ScheduleTimer(new Timer(obj =>
                {
                    schedule.DisposeTimer();
                    if(!schedule.IsDisposed)
                        Enqueue(() => { if(!schedule.IsDisposed) action(); });
                }, null, timeMs, 5000));
            return schedule;
        }

        public IDisposable ScheduleOnInterval(Action action, float dueTime, float interval)
        {
            var dueTimeMs = (long)(dueTime * 1000);
            var intervalMs = (long)(interval * 1000);
            return ScheduleOnInterval(action, dueTimeMs, intervalMs);
        }

        public IDisposable ScheduleOnInterval(Action action, int dueTimeMs, int intervalMs)
        {
            ScheduleTimer schedule = null;
            schedule = new ScheduleTimer(new Timer(obj =>
                {
                    if(schedule.IsDisposed)
                        schedule.DisposeTimer();
                    else
                        Enqueue(() => { if(!schedule.IsDisposed) action(); });
                }, null, dueTimeMs, intervalMs));
            return schedule;
        }

        public IDisposable ScheduleOnInterval(Action action, float interval)
        {
            return ScheduleOnInterval(action, interval, interval);
        }

        public IDisposable ScheduleOnInterval(Action action, long intervalMs)
        {
            return ScheduleOnInterval(action, intervalMs, intervalMs);
        }

        public Coroutine WhenAll(IEnumerable<Coroutine> coroutines)
        {
            return StartCoroutine(_WhenAll(coroutines));
        }

        IEnumerator _WhenAll(IEnumerable<Coroutine> coroutines)
        {
            foreach (var thisCoroutine in coroutines)
                yield return thisCoroutine;
        }

        public Coroutine WhenAll(IEnumerable<Task> tasks)
        {
            return StartCoroutine(_WhenAll(tasks));
        }

        IEnumerator _WhenAll(IEnumerable<Task> tasks)
        {
            foreach (var thisTask in tasks)
                yield return thisTask;
        }

        //use like a cancellation token
        class ScheduleTimer : IDisposable
        {
            Timer m_Timer;

            public ScheduleTimer(Timer timerToDispose)
            {
                m_Timer = timerToDispose;
            }

            public void DisposeTimer()
            {
                var timerToDispose = Interlocked.Exchange(ref m_Timer, null);
                if(timerToDispose != null)
                    timerToDispose.Dispose();
            }

            public bool IsDisposed { get; private set; }
            public void Dispose() {
                IsDisposed = true;
                DisposeTimer();
            }
        }
    }
}
