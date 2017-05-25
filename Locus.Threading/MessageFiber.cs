using System;
using System.Threading;

namespace Locus.Threading
{
    public abstract class MessageFiber<T>
    {
        static NodePool<T> _NodePool = new NodePool<T>();

        SingleLinkNode<T> head;
        SingleLinkNode<T> tail;
        SingleLinkNode<T> lastTale;

        static readonly SingleLinkNode<T> Blocked = new SingleLinkNode<T>();

        public MessageFiber()
        {
            RunInternalWaitCallback = RunInternal;
            head = tail = lastTale = new SingleLinkNode<T>() { Next = Blocked };
        }

        protected abstract void OnMessage(T message);
        protected abstract void OnException(Exception exception);

        WaitCallback RunInternalWaitCallback;
        void RunInternal(object obj)
        {
            var messageNode = (SingleLinkNode<T>)obj;
            
            do
            {
                //restore blocked, let it can be recycle
                lastTale.Item = default(T);
                lastTale.Next = null;
                _NodePool.Push(lastTale);

                //remember last tale to be continued
                lastTale = messageNode;
                try
                {
                    OnMessage(messageNode.Item);
                }
                catch (Exception e)
                {
                    OnException(e);
                }
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
            var newTail = _NodePool.Pop();
            newTail.Item = message;

            var oldTail = Interlocked.Exchange(ref tail, newTail);

            //if oldtail is null or tailing is failed
            if (!TryTail(oldTail, newTail))
                ThreadPool.QueueUserWorkItem(RunInternalWaitCallback, newTail);
        }


        //if next is already blocked, then previous actions are already executed.
        static bool TryTail(SingleLinkNode<T> prev, SingleLinkNode<T> next)
        {
            return Interlocked.CompareExchange(ref prev.Next, next, null) == null;
        }

        //get next node and mark it as blocked
        static SingleLinkNode<T> GetNext(SingleLinkNode<T> prev)
        {
            return Interlocked.Exchange(ref prev.Next, Blocked);
        }
    }
}

