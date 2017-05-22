using System;

namespace Locus.Threading
{
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

