using Locus.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Locus.ThreadingTest
{
    public class ConsumerQueueTest
    {
        [Fact]
        public async Task TestSingleConsumerMethod()
        {
            var tmf = new SingleConsumerQueue<int>();
            var add = MultiThreadTest.RunMultiple(() => tmf.EnqueueMessage(1), 100000);
            var sum = 0;
            var result = 0;
            //do while adding
            while(tmf.TryGetOne(out result))
                sum += result;
            await Task.WhenAll(add);
            //do after adding
            while (tmf.TryGetOne(out result))
                sum += result;
            Assert.True(sum == 100000);
        }
    }
}
