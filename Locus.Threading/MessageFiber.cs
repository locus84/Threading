using System;
using System.Threading;

namespace Locus.Threading
{
    public abstract class MessageFiber<T>
    {
        class MessageNode
        {
            public MessageNode Next;
            public bool canRecycle;
            public T Item;
        }

        MessageNode head;
        MessageNode tail;
        MessageNode lastTale;

        static readonly MessageNode Blocked = new MessageNode() { canRecycle = false };

        public MessageFiber()
        {
            RunInternalWaitCallback = RunInternal;
            head = tail = lastTale = new MessageNode() { canRecycle = false, Next = Blocked };
        }

        protected abstract void OnMessage(T message);
        protected abstract void OnException(Exception exception);

        WaitCallback RunInternalWaitCallback;
        void RunInternal(object obj)
        {
            var messageNode = (MessageNode)obj;
            
            do
            {
                //restore blocked, let it can be recycle
                lastTale.Next = messageNode;
                lastTale.Item = default(T);
                lastTale.canRecycle = true;

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
        
        private MessageNode GetAvailableNode()
        {
            MessageNode oldHead, oldNext, result;

            while (true)
            {
                oldHead = head;
                //hazard
                if (oldHead != head)
                    continue;
                oldNext = oldHead.Next;

                if (oldHead != head)
                    continue;
                
                if (!oldHead.canRecycle)
                {
                    //it's head then it's still executing..
                    if (oldHead == head)
                        return new MessageNode();
                    //if it's not head it's already recycled by other thread
                    else
                        continue;
                }

                result = oldHead;
                if (Interlocked.CompareExchange(ref head, oldNext, oldHead) == oldHead)
                    break;
            }

            result.Next = null;
            result.canRecycle = false;
            return result;
        }

        /// <summary>
        /// Enqueue a message to this fiber.
        /// The message will be executed on a threadpool thread with OnMessage implementation
        /// Each call is thread safe
        /// </summary>
        /// <param name="message">Message to enqueue</param>
        public void Enqueue(T message)
        {
            var newTail = GetAvailableNode();
            newTail.Item = message;

            var oldTail = Interlocked.Exchange(ref tail, newTail);

            //if oldtail is null or tailing is failed
            if (!TryTail(oldTail, newTail))
                ThreadPool.QueueUserWorkItem(RunInternalWaitCallback, newTail);
        }


        //if next is already blocked, then previous actions are already executed.
        static bool TryTail(MessageNode prev, MessageNode next)
        {
            return Interlocked.CompareExchange(ref prev.Next, next, null) == null;
        }

        //get next node and mark it as blocked
        static MessageNode GetNext(MessageNode prev)
        {
            return Interlocked.Exchange(ref prev.Next, Blocked);
        }
    }
}

