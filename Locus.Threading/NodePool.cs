using System.Threading;

namespace Locus.Threading
{
    public class SingleNode<T>
    {
        public SingleNode<T> Next;
        public T Item;

        public static bool CAS(ref SingleNode<T> toChange, SingleNode<T> newValue, SingleNode<T> comparand)
        {
            return ReferenceEquals(comparand, Interlocked.CompareExchange(ref toChange, newValue, comparand));
        }
    }

    //make this static so make use of thread safe functionality
    public static class NodePool<T>
    {
        //as this class is generic, this variable will ge generated per type
        private static SingleNode<T> head = new SingleNode<T>();

        public static void Push(SingleNode<T> newNode)
        {
            //attach this to next of head.
            //so assign head's next as new node's next.
            //now set head's next into new node, previous next will be
            //tailed as next of newnode.

            //we don't need to clear it's next variable because it'll be replaced anyway.
            //but we have to clear it's item value
            newNode.Item = default(T);

            do
            {
                newNode.Next = head.Next;
            } while (!SingleNode<T>.CAS(ref head.Next, newNode, newNode.Next));
            return;
        }

        public static SingleNode<T> Pop()
        {
            //we don't use head always, just get next of the head
            //if nex of the head is null, make a new instance of the node.
            SingleNode<T> result;
            do
            {
                result = head.Next;
                if (result == null)
                    return new SingleNode<T>();
            } while (!SingleNode<T>.CAS(ref head.Next, result.Next, result));
            //clear up next
            result.Next = null;
            return result;
        }
    }
}
