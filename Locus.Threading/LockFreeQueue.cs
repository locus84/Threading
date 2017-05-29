//http://wiki.unity3d.com/index.php/Lock_Free_Queue
//this is messed up, node pool is not acutally thread safe, tested it for 10,- times.
//take a look for it
//http://www.boyet.com/articles/lockfreequeue.html
//idea came from here

using System.Threading;
using System.Collections.Generic;

namespace Locus.Threading
{
    internal class SingleNode<T>
    {
        public SingleNode<T> Next;
        public T Item;
    }

    internal static class SingleNodePool<T>
    {
        static SingleNodePool()
        {
            head = tail = new SingleNode<T>();
        }

        //as this class is generic, this variable will ge generated per type
        private static SingleNode<T> head, tail;

        public static void Push(SingleNode<T> newNode)
        {
            newNode.Item = default(T);
            newNode.Next = null;
            var prevTail = Atomic.Swap(ref tail, newNode);
            prevTail.Next = newNode;
            return;
        }

        public static SingleNode<T> Pop()
        {
            //we don't use head always, just get next of the head
            //if nex of the head is null, make a new instance of the node.
            SingleNode<T> result, newHead;

            do
            {
                result = head;
                newHead = result.Next;
                if (newHead == null)
                    return new SingleNode<T>();
            } while (!Atomic.SwapIfSame(ref head, newHead, result));
            //clear up next
            result.Next = null;
            return result;
        }
    }

    public class LockFreeQueue<T>
    {
        SingleNode<T> head;
        SingleNode<T> tail;
        int m_Count = 0;

        public int Count { get { return m_Count; } }

        public LockFreeQueue()
        {
            head = tail = new SingleNode<T>();
        }

        public void Enqueue(T item)
        {

            Interlocked.Increment(ref m_Count);
            SingleNode<T> node, oldTail, oldNext;
            node = SingleNodePool<T>.Pop();
            node.Item = item;

            while (true)
            {

                oldTail = tail;
                oldNext = (SingleNode<T>)oldTail.Next;

                if (oldTail != tail)
                    continue;

                if (oldNext != null)
                {
                    Atomic.SwapIfSame(ref tail, oldNext, oldTail);
                    continue;
                }

                if (Atomic.SwapIfSame(ref oldTail, node, null))
                    break;
            }

            Atomic.SwapIfSame(ref tail, node, oldTail);
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
            while (TryDequeue(out item))
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
            while (currentNode != null)
            {
                if (object.ReferenceEquals(currentNode.Item, item))
                    return true;
                else
                    currentNode = currentNode.Next;
            }
            return false;
        }


        public bool TryDequeue(out T item)
        {
            SingleNode<T> oldTail, oldNext, oldHead;

            while (true)
            {

                oldHead = head;
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

                if (oldHead == oldTail)
                {
                    Interlocked.CompareExchange(ref tail, oldNext, oldTail);
                    continue;
                }

                item = oldNext.Item;
                if (Interlocked.CompareExchange(ref head, oldNext, oldHead) == oldHead)
                    break;
            }

            SingleNodePool<T>.Push(oldNext);
            Interlocked.Decrement(ref m_Count);
            return true;
        }

        public T Dequeue()
        {
            T result;
            TryDequeue(out result);
            return result;
        }
    }
}


//can recycle 형태 안되는 경우
//A가 Enqueue됨
//   rh,h -----t
// s0(0) - s1(A)  이후 s0은 recycle = true되고,
//디큐하면
//   rh -----h,t
// s0(0) -s1(0)
//1번쓰레드가 접근해서 X를 인큐하려고 GetAvailableNode도중 canrecycle == true체크까지 완료함
//그 와중에 2번쓰레드가 접근해서 B를 인큐
//   rh,h------t
//s1(0) - s0(B)
//다음 2번쓰레드가 아이가 B를 디큐
//   rh------h,t
//s1(0) - s0(0)
//다시 C를 인큐
//  rh, h -----t
// s0(0)-s1(C)
//그담에 지속해서 실행되면, h는 맞고 canrecycle까지 체크가 되어있으니 s0을 가져다 쓴다.
//  rh -------t, h
//s1(C) - s0(X)
//마치 디큐되어있는 형태가 된다.


