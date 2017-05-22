//using System;
//using System.Threading;
//using System.Collections.Generic;
//
//namespace Locus.Threading
//{
//    public class PoolFiber
//    {
//        Runnable toRun;
//        Runnable tail;
//		WaitCallback callback;
//        Thread currentThread;
//        public Action<Exception> OnException = (e) => Console.WriteLine(e);
//        public bool IsCurrent{ get { return currentThread == Thread.CurrentThread; } }
//
//		public PoolFiber()
//		{
//            callback = (obj) =>
//                {
//                    var thread = Thread.CurrentThread;
//                    Runnable next;
//                    while(true)
//                    {
//                        currentThread = thread;
//                        try{
//                            toRun.Run();
//                        }
//                        catch (Exception e)
//                        {
//                            if(OnException != null)
//                                OnException(e);
//                            else
//                            {
//                                Console.WriteLine(e.Message);
//                                Console.WriteLine(e.StackTrace);
//                            }
//                        }
//                        currentThread = null;
//                        next = toRun.GetNext();
//                        if(next == null) break;
//                        toRun.Return();
//                        toRun = next;
//                    }
//                };
//		}
//
//        void Enqueue(Runnable newTail)
//		{
//			var oldTail = Interlocked.Exchange(ref tail, newTail);
//			if (oldTail == null)
//			{
//				toRun = newTail;
//				ThreadPool.UnsafeQueueUserWorkItem(callback, null);
//				return;
//			}
//			else if (oldTail.TryTail(newTail))
//				return;
//			else
//			{
//				toRun = newTail;
//				oldTail.Return();
//				ThreadPool.QueueUserWorkItem(callback, null);
//			}
//		}
//
//
//        class Runnable<T>
//		{
//			static readonly object locker = new object();
//            public static readonly Runnable Blocked = new Runnable<T>();
//
//            protected Runnable m_Next;
//
//            public Runnable GetNext()
//			{
//                return Interlocked.Exchange(ref m_Next, Blocked);
//			}
//
//            public bool TryTail(Runnable next)
//			{
//				return Interlocked.CompareExchange(ref m_Next, next, null) == null;
//			}
//
//			public virtual void Run()
//			{
//                throw new NotImplementedException();
//			}
//
//            public virtual void OnReturn()
//			{
//                throw new NotImplementedException();
//			}
//
//            public void Return()
//            {
//
//            }
//		}
//
//        class RunnableWithPool<T> : Runnable where T : Runnable, new()
//        {
//            protected static LockFreeQueue<T> pool = new LockFreeQueue<T>();
//            public static T GetOne()
//            {
//                var result = pool.Dequeue();
//                if (result == null)
//                    result = new T();
//                return result;
//            }
//        }
//
//        class TubleAction : RunnableWithPool<TubleAction>
//		{
//			public Action m_action;
//
//			public override void Run()
//			{
//				m_action();
//			}
//
//            public override void Return()
//			{
//                m_Next = null;
//                m_action = null;
//                pool.Enqueue(this);
//			}
//		}
//
//		public void Enqueue(Action action)
//		{
//            var runnable = TubleAction.GetOne();
//			runnable.m_action = action;
//			Enqueue (runnable);
//		}
//
//        class TubleAction<T> : RunnableWithPool<TubleAction>
//		{
//			public Action<T> m_action;
//			public T m_param;
//
//			public override void Run()
//			{
//				m_action(m_param);
//			}
//
//			protected override TubleAction<T> OnReturn()
//			{
//				m_action = null;
//				m_param = default(T);
//				return this;
//			}
//		}
//
//		public void Enqueue<T>(Action<T> action, T param)
//		{
//			var runnable = TubleAction<T>.GetOne ();
//			runnable.m_action = action;
//			runnable.m_param = param;
//			Enqueue (runnable);
//		}
////
////
////		class TubleAction<T1, T2> : Runnable<TubleAction<T1, T2>>
////		{
////			public Action<T1, T2> m_action;
////			public T1 m_param1;
////			public T2 m_param2;
////
////			public override void Run()
////			{
////				m_action(m_param1, m_param2);
////			}
////
////			protected override TubleAction<T1, T2> OnReturn()
////			{
////				m_action = null;
////				m_param1 = default(T1);
////				m_param2 = default(T2);
////				return this;
////			}
////		}
////
////		public void Enqueue<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2)
////		{
////			var runnable = TubleAction<T1, T2>.GetOne ();
////			runnable.m_action = action;
////			runnable.m_param1 = param1;
////			runnable.m_param2 = param2;
////			Enqueue (runnable);
////		}
////
////
////        public static class RunnableExtension 
////        {
////            public static T GetOne<T>() where T : Runnable
////            {
////                return GenericPool<T>.GetOne();
////            }
////        }
//    }
//}
//
