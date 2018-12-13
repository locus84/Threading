using System;
using Locus.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Locus.ThreadingTest
{
    public class TestMsgFiber : MessageFiber<int>
    {
        public int i = 0;

        protected override void OnMessage(int message)
        {
            i += message;
        }
    }

    public class MessageFiberTest
    {

        [Fact]
        public async Task MessageFiberSpeedTest()
        {
            //this test is to examine the actions are executed in thread safe manner in threadpool
            var tmf = new TestMsgFiber();
            var add = MultiThreadTest.RunMultiple(() => tmf.EnqueueMessage(1), 100000);
            var minus = MultiThreadTest.RunMultiple(() => tmf.EnqueueMessage(-1), 100000);
            await Task.WhenAll(add);
            await Task.WhenAll(minus);
            //this await anything in this fiber
            await tmf.EnqueueTask(new Task(() => { })).ContinueIn(tmf);
            //the i variable must be 0, and exception count should be zero too
            Assert.True(tmf.IsCurrentThread);
            Assert.True(tmf.i == 0);
        }


        [Fact]
        public async Task TaskFiberSpeedTest()
        {
            //this test is to examine the actions are executed in thread safe manner in threadpool
            var tmf = new MessageFiber();
            var intVal = 0;
            var add = MultiThreadTest.RunMultiple(() => tmf.EnqueueAction(() => intVal++), 100000);
            var minus = MultiThreadTest.RunMultiple(() => tmf.EnqueueAction(() => intVal--), 100000);
            await Task.WhenAll(add);
            await Task.WhenAll(minus);
            //this await anything in this fiber
            await tmf;
            //the i variable must be 0, and exception count should be zero too
            Assert.True(intVal == 0);
        }
    }
}
