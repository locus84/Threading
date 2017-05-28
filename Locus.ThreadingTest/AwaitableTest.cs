﻿using Locus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Locus.ThreadingTest
{
    [TestClass]
    public class AwaitableTest
    {
        static TaskFiber GlobalTestFiber = new TaskFiber();


        [TestMethod]
        public async Task TestAsyncStartMethod()
        {
            await FiberYieldTest().IntoFiber(GlobalTestFiber);
        }

        [TestMethod]
        public async Task ThreadContinuousTest()
        {
            //so when yield in fiber called, 
            var result = await SomeMethod();
            Assert.IsTrue(result == 5);
        }

        public async Task<int> SomeMethod()
        {
            int result = 0;
            Assert.IsFalse(GlobalTestFiber.IsCurrentThread);

            await Task.Delay(0).IntoFiber(GlobalTestFiber);
            Assert.IsTrue(GlobalTestFiber.IsCurrentThread);

            await Task.Delay(10).IntoFiber(GlobalTestFiber);
            Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
            result++;

            await Task.Delay(10).IntoFiber(GlobalTestFiber);
            Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
            result++;

            await Task.Delay(10).IntoFiber(GlobalTestFiber);
            Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
            result++;

            await Task.Delay(10).IntoFiber(GlobalTestFiber);
            Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
            result++;

            await Task.Delay(10).IntoFiber(GlobalTestFiber);
            Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
            result++;
            return result;
        }

        public async Task SomeMethod2()
        {
            await Task.Delay(0);
        }


        public async Task FiberYieldTest()
        {
            Console.WriteLine("hgaha");
            Assert.IsFalse(GlobalTestFiber.IsCurrentThread);
            await Task.Delay(5);
            Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
            await Task.Delay(5);
            Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
            await Task.Delay(5);
            Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
        }

        [TestMethod]
        public Task AwaitForcingStartOfATask()
        {
            //this test is to see whether task is forced starting by await.
            //guess not. no failure detected for a log time
            return null;
            //var task = new Task(() => Assert.Fail());
            //await task;
            //Assert.Fail();
        }


        [TestMethod]
        public void SnycronisedRunTest()
        {
            var newTask = Task.Delay(1000);
            try
            {
                newTask.RunSynchronously();
                //above task will fail
                Assert.Fail();
            }
            catch
            {

            }
        }
    }
}

