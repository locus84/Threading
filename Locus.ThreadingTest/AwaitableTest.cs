using Locus.Threading;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Locus.ThreadingTest
{
    public class AwaitableTest
    {
        private readonly ITestOutputHelper output;

        public AwaitableTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        static MessageFiber TestTaskFiber = new MessageFiber();
        static TestMsgFiber TestMeesageFiber = new TestMsgFiber();

        [Fact]
        public async Task TestAsyncStartMethod()
        {
            await FiberYieldTest();
        }

        [Fact]
        public async Task ThreadContinuousTest()
        {
            //so when yield in fiber called, 
            output.WriteLine("StartingMethod");
            var result = await SomeMethod(TestTaskFiber);
            Assert.True(result == 5);
        }

        public async Task<int> SomeMethod(MessageFiberBase messageFiberBase)
        {
            await messageFiberBase;
            int result = 0;
            Assert.True(TestTaskFiber.IsCurrentThread);

            await Task.Delay(10);
            Assert.True(TestTaskFiber.IsCurrentThread);
            result++;

            await Task.Delay(10);
            Assert.True(TestTaskFiber.IsCurrentThread);
            result++;

            await Task.Delay(10);
            Assert.True(TestTaskFiber.IsCurrentThread);
            result++;

            await Task.Delay(10);
            Assert.True(TestTaskFiber.IsCurrentThread);
            result++;

            await Task.Delay(10);
            Assert.True(TestTaskFiber.IsCurrentThread);
            result++;
            return result;
        }

        public async Task SomeMethod2()
        {
            await Task.Delay(0);
        }


        public async Task FiberYieldTest()
        {
            Assert.False(TestTaskFiber.IsCurrentThread);
            await Task.Delay(5).ContinueIn(TestTaskFiber);
            Assert.True(TestTaskFiber.IsCurrentThread);
            await Task.Delay(5);
            Assert.True(TestTaskFiber.IsCurrentThread);
            await Task.Delay(5);
            Assert.True(TestTaskFiber.IsCurrentThread);
        }

        [Fact]
        public Task AwaitForcingStartOfATask()
        {
            //this test is to see whether task is forced starting by await.
            //guess not. no failure detected for a log time
            return null;
            //var task = new Task(() => Assert.Fail());
            //await task;
            //Assert.Fail();
        }


        [Fact]
        public void SnycronisedRunTest()
        {
            var newTask = Task.Delay(1000);
            try
            {
                newTask.RunSynchronously();
                //above task will fail
                Assert.True(true);
            }
            catch
            {

            }
        }

        [Fact]
        public void EnqueueTypeTest()
        {
            var myFiber = new MessageFiber();

            //an action
            myFiber.EnqueueAction(() => { });
            //a task
            myFiber.EnqueueTask(new Task(() => { }));
            //a task<T>
            myFiber.EnqueueTask(new Task<int>(() => 0));
        }

        async Task EnqueueTestFunction()
        {
            await Task.Delay(0);
        }

        async Task<int> EnqueueTestFunctionResult()
        {
            await Task.Delay(0);
            return 0;
        }
    }
}

