using System;
using System.Threading;

namespace Locus.Threading
{
    public abstract class MessageFiber<T>
    {
        SingleNode<T> head;
        SingleNode<T> tail;
        SingleNode<T> lastTale;
        Thread m_CurrentThread;
        public bool IsCurrentThread { get { return m_CurrentThread == Thread.CurrentThread; } }


        static readonly SingleNode<T> Blocked = new SingleNode<T>();

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
            var messageNode = (SingleNode<T>)obj;
            
            do
            {
                //restore blocked, let it can be recycle
                NodePool<T>.Push(lastTale);
                //remember last tale to be continued
                lastTale = messageNode;

                //set current Thread
                m_CurrentThread = Thread.CurrentThread;
                try
                {
                    OnMessage(messageNode.Item);
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
        public void Enqueue(T message)
        {
            var newTail = NodePool<T>.Pop();
            newTail.Item = message;

            var oldTail = Interlocked.Exchange(ref tail, newTail);

            //if oldtail is null or tailing is failed
            if (!TryTail(oldTail, newTail))
                ThreadPool.QueueUserWorkItem(RunInternalWaitCallback, newTail);
        }


        //if next is already blocked, then previous actions are already executed.
        static bool TryTail(SingleNode<T> prev, SingleNode<T> next)
        {
            return SingleNode<T>.CAS(ref prev.Next, next, null);
        }

        //get next node and mark it as blocked
        static SingleNode<T> GetNext(SingleNode<T> prev)
        {
            return Interlocked.Exchange(ref prev.Next, Blocked);
        }
    }
}

