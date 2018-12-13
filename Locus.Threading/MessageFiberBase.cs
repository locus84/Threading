using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Locus.Threading
{
    public abstract class MessageFiberBase
    {
        //thread static in debug mode is slow, but in realease mode, it runs faster than expected.
        [ThreadStatic]
        static MessageFiberBase CurrentIFiber = null;

        public bool IsCurrentThread { get { return CurrentIFiber == this; } }

        //the last tale, holds latest node that has been executed.
        //we cannot find it with 'Next'variable becuase we block 'Next' with 'Blocked'
        //to avoid race condition.
        //so when start executing new message, we have to return this node to pool
        static readonly MessageNodeBase Blocked = new MessageNodeBase();

        MessageNodeBase tail;
        MessageNodeBase lastTale;

        FiberSyncContext m_SyncContext;
        WaitCallback RunInternalWaitCallback;

        /// <summary>
        /// Create new Instance of MessageFiber<typeparamref name="T"/>
        /// MessageFiber can run Task, Action, or overriden OnMessage<typeparamref name="T"/> function
        /// </summary>
        public MessageFiberBase()
        {
            RunInternalWaitCallback = RunInternal;
            m_SyncContext = new FiberSyncContext(this);
            tail = lastTale = new MessageNodeBase() { Next = Blocked };
        }

        void RunInternal(object obj)
        {
            var messageNode = (MessageNodeBase)obj;

            //because it's thread specific value, we dont need to set
            //null everytime.
            CurrentIFiber = this;

            var cachedContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(m_SyncContext);

            do
            {
                //restore blocked, let it can be recycle
                lastTale.PushToPool();
                //remember last tale to be continued
                lastTale = messageNode;

                try
                {
                    InvokeMessage(messageNode);
                }
                catch (Exception e)
                {
                    throw e;
                }

                //if next is null, then it successfully replace it's Next to Blocked
                //otherwise messagenode is what recently trytail'ed
                messageNode = GetNext(messageNode);
            }
            while (messageNode != null);

            SynchronizationContext.SetSynchronizationContext(cachedContext);
            //now we're done is this thread pool thread,
            CurrentIFiber = null;
        }

        internal abstract void InvokeMessage(MessageNodeBase message);

        internal static void EnqueueInternal(MessageFiberBase fiber, Action continuation)
        {
            var newTail = NodePool<ActionMessageNode>.Pop();
            newTail.Message = continuation;
            fiber.EnqueueInternal(newTail);
        }

        internal void EnqueueInternal(MessageNodeBase newTail)
        {
            var oldTail = Atomic.Swap(ref tail, newTail);
            //if oldtail is null or tailing is failed
            if (!TryTail(oldTail, newTail))
            {
                //statis is a single object in heap, no further allocation with object[]
                ThreadPool.QueueUserWorkItem(RunInternalWaitCallback, newTail);
            }
        }

        //if next is already blocked, then previous actions are already executed.
        static bool TryTail(MessageNodeBase prev, MessageNodeBase next)
        {
            return Atomic.SwapIfSame(ref prev.Next, next, null);
        }

        //get next node and mark it as blocked
        //if it's not null, we dont need to execute exchange function
        static MessageNodeBase GetNext(MessageNodeBase prev)
        {
            return Atomic.Swap(ref prev.Next, Blocked);
        }

        public void EnqueueAction(Action action)
        {
            var newTail = NodePool<ActionMessageNode>.Pop();
            newTail.Message = action;
            EnqueueInternal(newTail);
        }

        public Task EnqueueTask(Task task)
        {
            var newTail = NodePool<TaskMessageNode>.Pop();
            newTail.Message = task;
            EnqueueInternal(newTail);
            return task;
        }

        public Task<T> EnqueueTask<T>(Task<T> task)
        {
            EnqueueTask((Task)task);
            return task;
        }
    }


    /// <summary>
    /// Virtualize SynchronizationContext to Fiber compatible
    /// </summary>
    internal class FiberSyncContext : SynchronizationContext
    {
        MessageFiberBase m_Fiber;
        public FiberSyncContext(MessageFiberBase fiber)
        {
            m_Fiber = fiber;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            var newTail = NodePool<ContextPostMessageNode>.Pop();
            newTail.Delegate = d;
            newTail.State = state;
            m_Fiber.EnqueueInternal(newTail);
        }
    }
}

