using System;
using System.Threading;
using System.Collections.Generic;

namespace Locus.Threading
{
    public class LockFreeQueue<T> {

        class SingleLinkNode {
            public SingleLinkNode Next;
            public bool canRecycle;
            public T Item;
        }

        SingleLinkNode recycleHead;
        SingleLinkNode head;
        SingleLinkNode tail;
        int m_Count = 0;

        public int Count { get { return m_Count; } }

        public LockFreeQueue() {
            head = tail = recycleHead = new SingleLinkNode {canRecycle = true};
        }

        private SingleLinkNode GetAvailableNode()
        {
            SingleLinkNode oldTail, oldNext, oldHead, result;

            while (true) {

                oldHead = recycleHead;
                //hazard
                if (oldHead != recycleHead)
                    continue;
                oldTail = head;
                oldNext = oldHead.Next;
                //hazard
                if (oldHead != recycleHead) 
                    continue;

                if (oldHead == oldTail) 
                    return new SingleLinkNode();

                //not ready
                if (!oldHead.canRecycle)
                    return new SingleLinkNode();

                result = oldHead;
                if(Interlocked.CompareExchange(ref recycleHead, oldNext, oldHead) == oldHead)
                    break;

            }

            result.Next = null;
            result.canRecycle = false;
            return result;
        }

        public void Enqueue(T item) {

            Interlocked.Increment(ref m_Count);
            SingleLinkNode node, oldTail, oldNext;
            node = GetAvailableNode();
            node.Item = item;

            while (true) {

                oldTail = tail;

                //hazard
                if (oldTail != tail)
                    continue;

                oldNext = oldTail.Next;
                if (oldTail != tail)
                    continue;

                if (oldNext != null) {
                    Interlocked.CompareExchange(ref tail, oldNext, oldTail);
                    continue;
                }

                if (Interlocked.CompareExchange(ref oldTail.Next, node, null) == null)
                    break;
            }

            Interlocked.CompareExchange(ref tail, node, oldTail);
        }

        public void Enqueue(IEnumerable<T> items)
        {
            foreach (var item in items)
                Enqueue(item);
        }

        public int TryDrain(IList<T> addTo)
        {
            int added = 0;
            T item;
            while(TryDequeue(out item))
            {
                added++;
                addTo.Add(item);
            }
            return added;
        }

        public bool Contains(T item)
        {
            if (head == tail)
                return false;
            var currentNode = head.Next;
            while(currentNode != null)
            {
                if(object.ReferenceEquals(currentNode.Item, item))
                    return true;
                else
                    currentNode = currentNode.Next;
            }
            return false;
        }


        public bool TryDequeue(out T item)
        {
            SingleLinkNode oldTail, oldNext, oldHead;

            while (true) {

                oldHead = head;
                //hazard
                if (oldHead != head)       // Check head hasn't changed
                    continue;
                oldTail = tail;
                oldNext = oldHead.Next;
                //hazard
                if (oldHead != head) 
                    continue;
                if (oldNext == null)
                {
                    item = default(T);
                    return false;
                }

                if (oldHead == oldTail) {
                    Interlocked.CompareExchange(ref tail, oldNext, oldTail);
                    continue;
                }

                item = oldNext.Item;
                if(Interlocked.CompareExchange(ref head, oldNext, oldHead) == oldHead)
                    break;
            }

            oldNext.Item = default(T);
            oldNext.canRecycle = true;
            Interlocked.Decrement(ref m_Count);
            return true;
        }

        public T Dequeue() {
            T result;
            TryDequeue(out result);
            return result;
        }
    }
}



