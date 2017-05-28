using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Locus.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Locus.ThreadingTest
{
    public class TestMsgFiber : MessageFiber<int>
    {
        public int i = 0;
        protected override void OnException(Exception exception)
        {
            
        }

        protected override void OnMessage(int message)
        {
            i += message;
        }
    }

    [TestClass]
    public class MessageFiberTest
    {

        [TestMethod]
        public async Task MessageFiberSpeedTest()
        {
            //this test is to examine the actions are executed in thread safe manner in threadpool
            var tmf = new TestMsgFiber();
            var add = MultiThreadTest.RunMultiple(() => tmf.EnqueueMessage(1), 100000);
            var minus = MultiThreadTest.RunMultiple(() => tmf.EnqueueMessage(-1), 100000);
            await Task.WhenAll(add);
            await Task.WhenAll(minus);
            //this await anything in this fiber
            await tmf.IntoFiber();
            //the i variable must be 0, and exception count should be zero too
            Assert.IsTrue(tmf.i == 0);
        }


        [TestMethod]
        public async Task TaskFiberSpeedTest()
        {
            //this test is to examine the actions are executed in thread safe manner in threadpool
            var tmf = new TaskFiber();
            var intVal = 0;
            var add = MultiThreadTest.RunMultiple(() => tmf.Enqueue(() => intVal++), 100000);
            var minus = MultiThreadTest.RunMultiple(() => tmf.Enqueue(() => intVal--), 100000);
            await Task.WhenAll(add);
            await Task.WhenAll(minus);
            //this await anything in this fiber
            await tmf.IntoFiber();
            //the i variable must be 0, and exception count should be zero too
            Assert.IsTrue(intVal == 0);
        }
    }
}
