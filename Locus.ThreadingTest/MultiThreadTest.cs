using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Locus.ThreadingTest
{
    //this is helper class to simplify test of multithreading environment
    //here we make use of threading.task so we can rely on it's functionality
    public class MultiThreadTest
    {
        public static IEnumerable<Task> RunMultiple(Action toRun, int count)
        {
            var taskList = new List<Task>();
            for (int i = 0; i < count; i++)
                taskList.Add(Task.Run(toRun));
            return taskList;
        }
    }
}
