using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Locus.Threading;
using System.Collections.Generic;

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


        [TestMethod]
        public void TestMethod1()
        {
            Console.WriteLine("Starting");
            var msgFiber = new TestFiber();
            for(int i = 0; i < 1000000; i++)
            {
                msgFiber.Enqueue((new Random()).Next());
            }
            System.Threading.Thread.Sleep(1000);
        }
    }
}
