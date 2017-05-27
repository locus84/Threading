//using System;
//using System.Threading;

//namespace Locus.Threading
//{
//    public class Task
//    {
//        //to run
//        protected Action _action;

//        static readonly NextTaskNode LastNode = new NextTaskNode();
//        NextTaskNode nextHead = LastNode;

//        public bool IsStarted { get; private set; }
//        public bool IsDone { get; private set; }
//        public Exception Error { get; private set; }
//        internal bool CanReturn { get; private set; }

//        public class NextTaskNode
//        {
//            public Task task;
//            public NextTaskNode next;
//            public void Reset()
//            {
//                next = null;
//                task = null;
//            }
//        }

//        #region ForTaskFiber
//        public static readonly Task Blocked = new Task() { IsStarted = true, IsDone = true };
//        protected Task m_Next;

//        public Task GetNext()
//        {
//            return Interlocked.Exchange(ref m_Next, Blocked);
//        }

//        public bool TryTail(Task next)
//        {
//            return Interlocked.CompareExchange(ref m_Next, next, null) == null;
//        }

//        public virtual void Return()
//        {
//            if (CanReturn)
//            {
//                Reset();
//                GenericPool<Task>.Return(this);
//            }
//        }
//        #endregion

//        #region ForRecycle
//        protected void Reset()
//        {
//            _action = null;
//            m_Next = null;
//            IsStarted = false;
//            IsDone = false;
//            Error = null;
//            CanReturn = false;
//            nextHead = LastNode;
//        }

//        public void MarkAsReturnable()
//        {
//            CanReturn = true;
//        }
//        #endregion

//        protected Task(){}

//        internal static Task GetFromPoolOrCreate(Action action)
//        {
//            var result = GenericPool<Task>.GetOne();
//            if (result == null)
//                return new Task(action);
//            result._action = action;
//            return result;
//        }

//        public Task(Action action)
//        {
//            _action = action;
//        }

//        public void Wait()
//        {
//            if (!IsDone)
//            {
//                var mre = new ManualResetEvent(false);
//                ContinueWith(() => mre.Set());
//                mre.WaitOne();
//            }
//        }

//        public void Start()
//        {
//            if (IsStarted)
//                return;
//            IsStarted = true;
//			ThreadPool.QueueUserWorkItem(RunInternalWaitCallback, this);
//        }

//        internal void StartInSameThread()
//        {
//            if (IsStarted)
//                return;
//            IsStarted = true;
//            RunInternal();
//        }


//        //if we started by previous task, there's no reason to switch thread.
//        //we're already in threadpool thread so we just go ahead
//		static readonly WaitCallback RunInternalWaitCallback = RunInternal;
//		static void RunInternal(object obj)
//		{
//			((Task)obj).RunInternal ();
//		}


//        protected virtual void InvokeAction()
//		{
//			_action();
//		}

//        void RunInternal() 
//        {
//            try
//            {
//				InvokeAction();
//            }
//            catch (Exception e)
//            {
//                Error = e;
//            }
//            finally
//            {
//                var headToRun = Interlocked.Exchange(ref nextHead, null);
//                IsDone = true;

//                //if there is something
//                while (headToRun != LastNode)
//                {
//                    headToRun.task.Start();
//                    var prev = headToRun;
//                    headToRun = headToRun.next;
////                    prev.Reset();
////                    GenericPool<NextTaskNode>.Return(prev);
//                }
//            }
//        }

//        public static Task Run(Action action)
//        {
//            var newTask = new Task(action);
//            newTask.Start();
//            return newTask;
//        }

//        public Task ContinueWith(Action nextAction)
//        {
//            return ContinueWith(new Task(nextAction));
//        }

//        //parameter itself is return value
//        public Task ContinueWith(Task nextTask)
//        {
//            if (IsDone)
//            {
//                nextTask.Start();
//                return nextTask;
//            }
//			var newNode = new NextTaskNode();
////            var newNode = GenericPool<NextTaskNode>.GetOne();
////            if (newNode == null)
////                newNode = new NextTaskNode();
//            newNode.task = nextTask;

//            bool changedHead = false;
//            NextTaskNode oldHead;

//            do
//            {
//                oldHead = nextHead;
//                if(oldHead == null)
//                {
//                    nextTask.Start();
////                    newNode.Reset();
////                    GenericPool<NextTaskNode>.Return(newNode);
//                    return nextTask;
//                }
//                else
//                {
//                    newNode.next = oldHead;
//                    changedHead = oldHead == Interlocked.CompareExchange(ref nextHead, newNode, oldHead);
//                }
//            }
//            while(!changedHead);

//            return nextTask;
//        }
//    }


//    public class Task<T> : Task
//    {
//        public T Result { get; protected set; }
//        protected Func<T> _func;

//        public Task(Func<T> returnAction)
//        {
//			_func = returnAction;
//        }

//		protected override void InvokeAction ()
//		{
//			Result = _func ();
//		}
//    }
//}
