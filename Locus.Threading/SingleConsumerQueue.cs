using System;
using System.Threading;
using System.Threading.Tasks;

namespace Locus.Threading
{
    public class SingleConsumerQueue<T>
    {
        SingleNode<T> tail;
        SingleNode<T> lastNode;

        public SingleConsumerQueue()
        {
            tail = lastNode = new SingleNode<T>();
        }

        public bool TryGetOne(out T dequeued)
        {
            var nextNode = lastNode.Next;

            if (nextNode == null)
            {
                dequeued = default(T);
                return false;
            }

            SingleNodePool<T>.Push(lastNode);
            dequeued = nextNode.Item;
            lastNode = nextNode;
            return true;
        }
        
        public void Enqueue(T message)
        {
            var newNode = SingleNodePool<T>.Pop();
            newNode.Item = message;
            var oldTail = Atomic.Swap(ref tail, newNode);
            oldTail.Next = newNode;
        }
    }
}

