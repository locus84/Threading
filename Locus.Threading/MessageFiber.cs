using System;
using System.Threading;
using System.Threading.Tasks;

namespace Locus.Threading
{
    public abstract class MessageFiber<T> : IFiber
    {
        SingleNodeBase head;
        SingleNodeBase tail;
        //the last tale, holds latest node that has been executed.
        //we cannot find it with 'Next'variable becuase we block 'Next' with 'Blocked'
        //to avoid race condition.
        //so when start executing new message, we have to return this node to pool
        SingleNodeBase lastTale;
        Thread m_CurrentThread;
        public bool IsCurrentThread { get { return m_CurrentThread == Thread.CurrentThread; } }

        static readonly SingleNodeBase Blocked = new SingleNodeBase();

        public MessageFiber()
        {
            RunInternalWaitCallback = RunInternal;
            head = tail = lastTale = new SingleNode<T>() { Next = Blocked };
        }

        protected abstract void OnMessage(T message);
        protected abstract void OnException(Exception exception);

        WaitCallback RunInternalWaitCallback;

        //TODO: make this to run bunch of them
        void RunInternal(object obj)
        {
            var messageNode = (SingleNodeBase)obj;
            //store current Thread to reduce property call
            var currentThread = Thread.CurrentThread;

            do
            {
                //restore blocked, let it can be recycle
                lastTale.PushToPool();
                //remember last tale to be continued
                lastTale = messageNode;

                //set current Thread
                m_CurrentThread = currentThread;

                try
                {
                    if(!messageNode.TryInvokeSelf())
                    OnMessage((messageNode as SingleNode<T>).Item);
                }
                catch (Exception e)
                {
                    OnException(e);
                }

                //release current Thread
                m_CurrentThread = null;

                //if next is null, then it successfully replace it's Next to Blocked
                //otherwise messagenode is what recently trytail'ed
                messageNode = GetNext(messageNode);
            }
            while (messageNode != null);
        }

        /// <summary>
        /// Enqueue a message to this fiber.
        /// The message will be executed on a threadpool thread with OnMessage implementation
        /// Each call is thread safe
        /// </summary>
        /// <param name="message">Message to enqueue</param>
        public void EnqueueMessage(T message)
        {
            var newTail = SingleNode<T>.PopFromPool();
            newTail.Item = message;
            EnqueueInternal(newTail);
        }

        public void EnqueueAction(Action action)
        {
            var newTail = SingleActionNode.PopFromPool();
            newTail.Item = action;
            EnqueueInternal(newTail);
        }

        public Task EnqueueTask(Task task)
        {
            var newTail = SingleTaskNode.PopFromPool();
            newTail.Item = task;
            EnqueueInternal(newTail);
            return task;
        }

        public Task<T> EnqueueTask(Task<T> task)
        {
            var newTail = SingleTaskNode.PopFromPool();
            newTail.Item = task;
            EnqueueInternal(newTail);
            return task;
        }

        void EnqueueInternal(SingleNodeBase newTail)
        {
            var oldTail = Interlocked.Exchange(ref tail, newTail);
            //if oldtail is null or tailing is failed
            if (!TryTail(oldTail, newTail))
                ThreadPool.QueueUserWorkItem(RunInternalWaitCallback, newTail);
        }

        //if next is already blocked, then previous actions are already executed.
        static bool TryTail(SingleNodeBase prev, SingleNodeBase next)
        {
            return SingleNodeBase.CAS(ref prev.Next, next, null);
        }

        //get next node and mark it as blocked
        //if it's not null, we dont need to execute exchange function
        static SingleNodeBase GetNext(SingleNodeBase prev)
        {
            if (prev.Next != null) return prev.Next;
            return Interlocked.Exchange(ref prev.Next, Blocked);
        }

        void IFiber.EnqueueAwaitableContinuation(Action action)
        {
            EnqueueAction(action);
        }
    }
}

