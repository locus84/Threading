using System;
using System.Threading;


namespace Locus.Threading
{
    public class ReaderWriterLockSimple
    {
        public IDisposable GetReadLock()
        {
            return new ReadLockObj(this);
        }

        public IDisposable GetWriteLock()
        {
            return new WriteLockObj(this);
        }

        private class ReadLockObj : IDisposable
        {
            ReaderWriterLockSimple _lock;

            public ReadLockObj(ReaderWriterLockSimple locker)
            {
                _lock = locker;
                _lock.BeginRead();
            }

            public void Dispose()
            {
                _lock.EndRead();
            }
        }

        private class WriteLockObj : IDisposable
        {
            ReaderWriterLockSimple _lock;

            public WriteLockObj(ReaderWriterLockSimple locker)
            {
                _lock = locker;
                _lock.BeginWrite();
            }

            public void Dispose()
            {
                _lock.EndWrite();
            }
        }

        public ReaderWriterLockSimple()
        {
            m_activeReaderCount = 0;
            m_activeWriter = false;

            m_countLock = new object();
        }

        public void BeginRead()
        {
            Monitor.Enter(m_countLock);

            while (m_activeWriter)
            {
                Monitor.Wait(m_countLock);
            }

            m_activeReaderCount++;

            Monitor.Exit(m_countLock);
        }

        public void EndRead()
        {
            Monitor.Enter(m_countLock);

            m_activeReaderCount--;
            if (m_activeReaderCount == 0)
            {
                // At this point we are sure that only writers can be in the
                // wait queue, so it is sufficient to wake up just one of them.
                Monitor.Pulse(m_countLock);
            }

            Monitor.Exit(m_countLock);
        }

        public void BeginWrite()
        {
            Monitor.Enter(m_countLock);

            while ((m_activeReaderCount != 0) || (m_activeWriter))
            {
                Monitor.Wait(m_countLock);
            }

            m_activeWriter = true;

            Monitor.Exit(m_countLock);
        }

        public void EndWrite()
        {
            Monitor.Enter(m_countLock);

            m_activeWriter = false;

            // Both readers and writers can be in the wait queue.
            // We will wake them up all and let them compete for
            // the right to read / write.
            Monitor.PulseAll(m_countLock);

            Monitor.Exit(m_countLock);
        }

        private int m_activeReaderCount;
        private bool m_activeWriter;
        private object m_countLock;
    }
}