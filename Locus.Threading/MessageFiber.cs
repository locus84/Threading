using System;
using System.Threading;

namespace Locus.Threading
{
    public class MessageFiber<T>
    {
        Action<T> _onProcessMessage;

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

        public MessageFiber(Action<T> onProcessMessage)
        {
            _onProcessMessage = onProcessMessage;
            RunInternalWaitCallback = RunInternal;
            head = tail = lastTale = new MessageNode() { canRecycle = false, Next = Blocked };
        }

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
                _onProcessMessage(messageNode.Item);
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


        
        public void Enqueue(T parameter)
        {
            var newTail = GetAvailableNode();
            newTail.Item = parameter;

            var oldTail = Interlocked.Exchange(ref tail, newTail);

            //if oldtail is null or tailing is failed
            if (!TryTail(oldTail, newTail))
                ThreadPool.UnsafeQueueUserWorkItem(RunInternalWaitCallback, newTail);
        }

        
        static bool TryTail(MessageNode prev, MessageNode next)
        {
            return Interlocked.CompareExchange(ref prev.Next, next, null) == null;
        }

        //if next is already blocked, then previous actions are already executed.
        //we're free to start new thread to execute next one
        static MessageNode GetNext(MessageNode prev)
        {
            return Interlocked.Exchange(ref prev.Next, Blocked);
        }
    }
}

