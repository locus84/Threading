using Locus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
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
            await FiberYieldTest().YieldInFiber(GlobalTestFiber);
        }

        [TestMethod]
        public async Task ThreadContinuousTest()
        {
            await SomeMethod().YieldInFiber(GlobalTestFiber);
            Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
        }

        public async Task SomeMethod()
        {
            await SomeMethod2().YieldInFiber(GlobalTestFiber);
            Assert.IsTrue(GlobalTestFiber.IsCurrentThread);

            //await Task.Delay(0).YieldInFiber(GlobalTestFiber);
            //await Task.Delay(0).YieldInFiber(GlobalTestFiber);
            //Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
            //await Task.Delay(0).YieldInFiber(GlobalTestFiber);
            //await Task.Delay(0).YieldInFiber(GlobalTestFiber);
            //await Task.Yield();
            //await Task.Yield();
            //await Task.Yield();
            //await Task.Yield();
            //await Task.Yield();
            //Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
            await Task.Delay(1);
            await Task.Delay(1);
            await Task.Delay(1);
            await Task.Delay(1);
            //Assert.IsTrue(GlobalTestFiber.IsCurrentThread);
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
            newTask.RunSynchronously();
        }
    }
}

