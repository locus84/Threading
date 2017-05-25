using System.Threading;
using System.Collections.Generic;

namespace Locus.Threading
{
    public class LockFreeQueue<T> {

        static NodePool<T> _NodePool = new NodePool<T>();

        SingleLinkNode<T> head;
        SingleLinkNode<T> tail;
        int m_Count = 0;

        public int Count { get { return m_Count; } }

        public LockFreeQueue() {
            head = tail = new SingleLinkNode<T>();
        }
        
        public void Enqueue(T item) {

            Interlocked.Increment(ref m_Count);
            SingleLinkNode<T> node, oldTail, oldNext;
            node = _NodePool.Pop();
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
            SingleLinkNode<T> oldTail, oldNext, oldHead;

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
            _NodePool.Push(oldNext);
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


