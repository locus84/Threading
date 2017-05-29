
namespace Locus.Threading
{
    public static class GenericPool<T>
    {
        static GenericPool()
        {
            head = tail = new SingleNode<T>();
        }

        //as this class is generic, this variable will ge generated per type
        private static SingleNode<T> head, tail;

        public static void Push(T Item)
        {
            var newNode = SingleNodePool<T>.Pop();
            newNode.Item = Item;
            var prevTail = Atomic.Swap(ref tail, newNode);
            prevTail.Next = newNode;
            return;
        }

        public static T Pop()
        {
            //we don't use head always, just get next of the head
            //if nex of the head is null, make a new instance of the node.
            SingleNode<T> result, newHead;

            do
            {
                result = head;
                newHead = result.Next;
                if (newHead == null)
                    return default(T);
            } while (!Atomic.SwapIfSame(ref head, newHead, result));
            //clear up next
            var itemToReturn = result.Item;
            SingleNodePool<T>.Push(result);
            return itemToReturn;
        }
    }
}

