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
    public class MessageFiberTest
    {
        public class TestMsgFiber : MessageFiber<int>
        {
            public int i = 0;
            public List<Exception> exceptions = new List<Exception>();
            protected override void OnException(Exception exception)
            {
                exceptions.Add(exception);
            }

            protected override void OnMessage(int message)
            {
                i += message;
            }
        }

        [TestMethod]
        public void TestFunc()
        {
            //this test is to examine the actions are executed in thread safe manner in threadpool
            var tmf = new TestMsgFiber();
            var add = MultiThreadTest.RunMultiple(() => tmf.Enqueue(1), 10000);
            var minus = MultiThreadTest.RunMultiple(() => tmf.Enqueue(-1), 10000);
            System.Threading.Tasks.Task.WhenAll(add).Wait();
            System.Threading.Tasks.Task.WhenAll(minus).Wait();
            //the i variable must be 0, and exception count should be zero too
            Assert.IsTrue(tmf.i == 0);
            Assert.IsTrue(tmf.exceptions.Count == 0);
        }


        [TestMethod]
        public void TaskFiberTest()
        {
            //this test is to examine the actions are executed in thread safe manner in threadpool
            var tmf = new TaskFiber();
            var intVal = 0;
            var add = MultiThreadTest.RunMultiple(() => tmf.Enqueue(() => intVal++), 10000);
            var minus = MultiThreadTest.RunMultiple(() => tmf.Enqueue(() => intVal--), 10000);
            System.Threading.Tasks.Task.WhenAll(add).Wait();
            System.Threading.Tasks.Task.WhenAll(minus).Wait();
            //the i variable must be 0, and exception count should be zero too
            Assert.IsTrue(intVal == 0);
        }
    }
}
