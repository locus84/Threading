
namespace Locus.Threading
{
    //when it comes to performance, System.Concurrent offers better than this.
    //so consider using concurrentbag<T> later on
    public static class GenericPool<T>
    {
        static LockFreeQueue<T> pool = new LockFreeQueue<T>();
        public static T GetOne()
        {
            return pool.Dequeue();
        }

        public static void Return(T toReturn)
        {
            pool.Enqueue(toReturn);
        }

        public static int Count { get { return pool.Count; } }

        public static void Clear()
        {
            pool = new LockFreeQueue<T>();
        }
    }
}

