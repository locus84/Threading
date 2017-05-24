using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Locus.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;

namespace Locus.ThreadingTest
{
    [TestClass]
    public class UnitTest1
    {
        static List<int> TestIntList = new List<int>();

        class TestFiber : MessageFiber<int>
        {
            protected override void OnException(Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.StackTrace);
                System.Diagnostics.Debug.WriteLine(exception.Message);
            }

            protected override void OnMessage(int message)
            {
                System.Threading.Thread.Sleep((new Random()).Next());
                TestIntList.Add(message);
            }
        }

        public class Wait
        {
            public bool ShouldWait = false;
        }

        [TestMethod]
        public void TestMethod1()
        {
            var testQ = new LockFreeQueue<char>();
            var wait = new Wait();
            testQ.Enqueue('A');
            testQ.Dequeue();
            System.Threading.Tasks.Task.Run(() =>
            {
                System.Threading.Thread.Sleep(50);
                testQ.Enqueue('B');
                testQ.Dequeue();
                //var adequeued = testQ.Dequeue();
                testQ.Enqueue('C');
            });
            testQ.Enqueue('X', true);
            var one = testQ.Dequeue();
            var two = testQ.Dequeue();
            var hoho = testQ.Dequeue();
            var hehe = string.Empty;
        }

        public void OnThread(object obj)
        {
            

        }


        [TestMethod]
        public void TestMethod3()
        {
            for (int j = 0; j < 1000; j++)
            {
                var q = new ConcurrentQueue<int>();
                for (int i = 0; i < 1000; i++)
                    q.Enqueue(i);
                int result;
                for (int i = 0; i < 1000; i++)
                    q.TryDequeue(out result);

            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            for(int j =0; j < 1000; j++)
            {
                var lfq = new LockFreeQueue<int>();
                for (int i = 0; i < 1000; i++)
                    lfq.Enqueue(i);
                int result;
                for (int i = 0; i < 1000; i++)
                    lfq.TryDequeue(out result);

            }

        }
    }
}


namespace Locus.Threading
{
    public class LockFreeQueue<T>
    {

        class SingleLinkNode
        {
            public SingleLinkNode Next;
            public bool canRecycle;
            public T Item;
        }

        SingleLinkNode recycleHead;
        SingleLinkNode head;
        SingleLinkNode tail;
        int m_Count = 0;

        public int Count { get { return m_Count; } }

        public LockFreeQueue()
        {
            head = tail = recycleHead = new SingleLinkNode { canRecycle = true };
        }

        private SingleLinkNode GetAvailableNode(bool test = false)
        {
            SingleLinkNode oldTail, oldNext, oldHead, result;

            while (true)
            {

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

                if (test)
                    Thread.Sleep(1000);

                result = oldHead;
                if (Interlocked.CompareExchange(ref recycleHead, oldNext, oldHead) == oldHead)
                    break;

            }

            result.Next = null;
            result.canRecycle = false;
            return result;
        }

        public void Enqueue(T item, bool test = false)
        {

            Interlocked.Increment(ref m_Count);
            SingleLinkNode node, oldTail, oldNext;
            node = GetAvailableNode(test);
            node.Item = item;

            while (true)
            {

                oldTail = tail;

                //hazard
                if (oldTail != tail)
                    continue;

                oldNext = oldTail.Next;
                if (oldTail != tail)
                    continue;

                if (oldNext != null)
                {
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
            SingleLinkNode oldTail, oldNext, oldHead;

            while (true)
            {

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

                //helping
                if (oldHead == oldTail)
                {
                    Interlocked.CompareExchange(ref tail, oldNext, oldTail);
                    continue;
                }

                item = oldNext.Item;
                if (Interlocked.CompareExchange(ref head, oldNext, oldHead) == oldHead)
                    break;
            }

            oldNext.Item = default(T);
            //oldHead.canRecycle = true;
            //oldNext.canRecycle = true;
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



//can recycle ���� �ȵǴ� ���
//A�� Enqueue��
//   rh,h -----t
// s0(0) - s1(A)  ���� s0�� recycle = true�ǰ�,
//��ť�ϸ�
//   rh -----h,t
// s0(0) -s1(0)
//1�������尡 �����ؼ� X�� ��ť�Ϸ��� GetAvailableNode���� canrecycle == trueüũ���� �Ϸ���
//�� ���߿� 2�������尡 �����ؼ� B�� ��ť
//   rh,h------t
//s1(0) - s0(B)
//���� 2�������尡 ���̰� B�� ��ť
//   rh------h,t
//s1(0) - s0(0)
//�ٽ� C�� ��ť
//  rh, h -----t
// s0(0)-s1(C)
//�״㿡 �����ؼ� ����Ǹ�, h�� �°� canrecycle���� üũ�� �Ǿ������� s0�� ������ ����.
//  rh -------t, h
//s1(C) - s0(X)
//��ġ ��ť�Ǿ��ִ� ���°� �ȴ�.
