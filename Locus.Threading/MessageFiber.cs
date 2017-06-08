using System;
using System.Threading;
using System.Threading.Tasks;

namespace Locus.Threading
{
    public abstract class MessageFiber<T> : IFiber
    {
        //the last tale, holds latest node that has been executed.
        //we cannot find it with 'Next'variable becuase we block 'Next' with 'Blocked'
        //to avoid race condition.
        //so when start executing new message, we have to return this node to pool
        MessageNodeBase tail;
        MessageNodeBase lastTale;

        [ThreadStatic]
        static MessageFiber<T> m_CurrentFiber;
        public bool IsCurrentThread { get { return m_CurrentFiber == this; } }

        static readonly MessageNode<T> Blocked = new MessageNode<T>();

        public MessageFiber()
        {
            RunInternalWaitCallback = RunInternal;
            tail = lastTale = new MessageNode<T>() { Next = Blocked };
        }

        protected abstract void OnMessage(T message);
        protected abstract void OnException(Exception exception);

        WaitCallback RunInternalWaitCallback;

        //TODO: make this to run bunch of them
        void RunInternal(object obj)
        {
            var messageNode = (MessageNodeBase)obj;

            //because it's thread specific value, we dont need to set
            //null everytime.
            m_CurrentFiber = this;

            do
            {
                //restore blocked, let it can be recycle
                lastTale.PushToPool();
                //remember last tale to be continued
                lastTale = messageNode;

                try
                {
                    if(!messageNode.TryInvoke())
                    {
                        OnMessage(((MessageNode<T>)messageNode).Message);
                    }
                        
                }
                catch (Exception e)
                {
                    var type = messageNode.GetType();
                    OnException(e);
                }

                //if next is null, then it successfully replace it's Next to Blocked
                //otherwise messagenode is what recently trytail'ed
                messageNode = GetNext(messageNode);
            }
            while (messageNode != null);

            //now we're done is this thread pool thread,
            m_CurrentFiber = null;
        }

        /// <summary>
        /// Enqueue a message to this fiber.
        /// The message will be executed on a threadpool thread with OnMessage implementation
        /// Each call is thread safe
        /// </summary>
        /// <param name="message">Message to enqueue</param>
        public void EnqueueMessage(T message)
        {
            var newTail = NodePool<MessageNode<T>>.Pop();
            newTail.Message = message;
            EnqueueInternal(newTail);
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

        public Task<T> EnqueueTask(Task<T> task)
        {
            EnqueueTask(task);
            return task;
        }

        void EnqueueInternal(MessageNodeBase newTail)
        {
            var oldTail = Atomic.Swap(ref tail, newTail);
            //if oldtail is null or tailing is failed
            if (!TryTail(oldTail, newTail))
                ThreadPool.QueueUserWorkItem(RunInternalWaitCallback, newTail);
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

        void IFiber.EnqueueAwaitableContinuation(Action action)
        {
            EnqueueAction(action);
        }
    }
}

