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
        }

        public void OnThread(object obj)
        {
            

        }


        [TestMethod]
        public void TestMethodOne()
        {
            var conb = new ConcurrentBag<int>();
            for(int i = 0; i < 100000; i++)
            {
                conb.Add(1);
                int takeVal;
                conb.TryTake(out takeVal);
            }
        }

        //make it bunch method

        [TestMethod]
        public void TestMethodTwo()
        {
        }
    }
}
