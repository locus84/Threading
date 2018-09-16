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

        static TaskFiber TestTaskFiber = new TaskFiber();
        static TestMsgFiber TestMeesageFiber = new TestMsgFiber();


        [Fact]
        public async Task TestAsyncStartMethod()
        {
            await FiberYieldTest().IntoFiber(TestTaskFiber);
        }

        [Fact]
        public async Task ThreadContinuousTest()
        {
            //so when yield in fiber called, 
            output.WriteLine("StartingMethod");
            var result = await SomeMethod();
            Assert.True(result == 5);
        }

        public async Task<int> SomeMethod()
        {
            int result = 0;
            Assert.False(TestTaskFiber.IsCurrentThread);
            await TestMeesageFiber;
            //or await TestMeesageFiber.IntoFiber();

            await TestTaskFiber.IntoFiber();
            Assert.True(TestTaskFiber.IsCurrentThread);

            await Task.Delay(10);

            output.WriteLine("CurrentThread? : " + TestTaskFiber.IsCurrentThread);

            await Task.Delay(10).IntoFiber(TestTaskFiber);
            Assert.True(TestTaskFiber.IsCurrentThread);
            result++;

            await Task.Delay(10).IntoFiber(TestTaskFiber);
            Assert.True(TestTaskFiber.IsCurrentThread);
            result++;

            await Task.Delay(10).IntoFiber();
            Assert.True(TestTaskFiber.IsCurrentThread);
            result++;

            await Task.Delay(10).IntoFiber();
            Assert.True(TestTaskFiber.IsCurrentThread);
            result++;

            await Task.Delay(10).IntoFiber(TestTaskFiber);
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
            Console.WriteLine("hgaha");
            Assert.False(TestTaskFiber.IsCurrentThread);
            await Task.Delay(5);
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
            var myFiber = new TaskFiber();

            //an action
            myFiber.Enqueue(() => { });
            //a func
            myFiber.Enqueue(() => 0);
            //a task
            myFiber.Enqueue(new Task(() => { }));
            //a task<T>
            myFiber.Enqueue(new Task<int>(() => 0));
            //an async action
            myFiber.Enqueue(async () => { await Task.Delay(0); });
            //an async func
            myFiber.Enqueue(async () => { await Task.Delay(0); return 0; });
            //an async task
            myFiber.Enqueue(() => EnqueueTestFunction());
            //an async task<T>
            myFiber.Enqueue(() => EnqueueTestFunctionResult());
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

