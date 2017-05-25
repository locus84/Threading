using System.Threading;

namespace Locus.Threading
{
    internal class SingleLinkNode<T>
    {
        public SingleLinkNode<T> Next;
        public T Item;
        public static bool CAS(ref SingleLinkNode<T> toChange, SingleLinkNode<T> newValue, SingleLinkNode<T> comparand)
        {
            return ReferenceEquals(comparand, Interlocked.CompareExchange(ref toChange, newValue, comparand));
        }
    }

    internal class NodePool<T>
    {
        private SingleLinkNode<T> head;

        public NodePool()
        {
            head = new SingleLinkNode<T>();
        }

        public void Push(SingleLinkNode<T> newNode)
        {
            //attach this to next of head.
            //so assign head's next as new node's next.
            //now set head's next into new node, previous next will be
            //tailed as next of newnode.
            do
            {
                newNode.Next = head.Next;
            } while (!SingleLinkNode<T>.CAS(ref head.Next, newNode, newNode.Next));
            return;
        }

        public SingleLinkNode<T> Pop()
        {
            //we don't use head always, just get next of the head
            //if nex of the head is null, make a new instance of the node.
            SingleLinkNode<T> result;
            do
            {
                result = head.Next;
                if (result == null)
                    return new SingleLinkNode<T>();
            } while (!SingleLinkNode<T>.CAS(ref head.Next, result.Next, result));
            result.Next = null;
            return result;
        }
    }
}
