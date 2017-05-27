//using System;
//using System.Collections;
//using System.Collections.Generic;

//namespace Locus.Threading
//{
//    //coroutine itself
//    public class Coroutine {

//        readonly TaskFiber fiber;
//        readonly IEnumerator Enumerator;
//        readonly List<Coroutine> waitingCoroutines;
//        bool isDone = false;
//        bool isStopped = false;

//        public Coroutine(IEnumerator routine, TaskFiber ent)
//        {
//            Enumerator = routine;
//            fiber = ent;
//            waitingCoroutines = new List<Coroutine>();
//            //add and run
//            if (ent.IsSameThread)
//                UpdateCoroutines();
//            else
//                fiber.Enqueue(UpdateCoroutines);
//        }

//        internal void Stop()
//        {
//            isStopped = true;
//        }

//        void RegisterEndCoroutine(Coroutine coroutine)
//        {
//            //this will be excuted in target coroutine
//            //quick exit
//            if (isDone)
//                coroutine.fiber.Enqueue(coroutine.UpdateCoroutines);
//            else
//                waitingCoroutines.Add(coroutine);
//        }


//        void UpdateCoroutines()
//        {
//            //this is excuted in owner's taskfiber
//            //if it's canceled, 
//            bool canRun;

//            if (isStopped)
//            {
//                canRun = false;
//            }
//            else
//            {
//                try
//                {
//                    canRun = Enumerator.MoveNext();
//                }
//                catch (Exception e)
//                {
//                    //if there's exception while excuting coroutine, it must be ended.
//                    //but there're waiting coroutines, those who have to run 
//                    fiber.OnException(e);
//                    canRun = false;
//                }
//            }

//            if(!canRun)
//            {
//                isDone = true;
//                for (int i = 0; i < waitingCoroutines.Count; i++)
//                    waitingCoroutines[i].fiber.Enqueue(waitingCoroutines[i].UpdateCoroutines);
//                waitingCoroutines.Clear();
//            }
//            else
//            {
//                object current = Enumerator.Current;

//                //if it's coroutine, we have to register it's done action
//                if(current is Coroutine)
//                {
//                    var targetCoroutine = current as Coroutine;
//                    targetCoroutine.fiber.Enqueue(() => targetCoroutine.RegisterEndCoroutine(this));
//                }
//                //if action, just start new action and continue with update
//                else if(current is Action)
//                {
//                    Task.Run(current as Action).ContinueWith(() => fiber.Enqueue((UpdateCoroutines)));
//                }
//                //IYieldable should be done 
//                else if(current is IYieldable)
//                {
//                    (current as IYieldable).Continue(fiber, UpdateCoroutines);
//                }
//                else if(current is Task)
//                {
//                    (current as Task).ContinueWith(() => fiber.Enqueue(UpdateCoroutines));
//                }
//                else
//                {
//                    fiber.Enqueue (UpdateCoroutines);
//                }
//            }
//        }
//    }

//    //Iyieldable is dangerous
//    //TODO keep coroutine when if IYieldable fails
//    public interface IYieldable
//    {
//        void Continue(TaskFiber fiber, Action updateCoroutine);
//    }

//    public class WaitForSeconds : IYieldable
//    {
//        readonly long waitTimeMs;
//        public WaitForSeconds(float second)
//        {
//            waitTimeMs = (long)(second*1000);
//        }

//        public void Continue(TaskFiber fiber, Action updateCoroutine)
//        {
//            fiber.Schedule(updateCoroutine, waitTimeMs);
//        }
//    }
////
////    internal class FiberAction : IYieldable
////    {
////        public Action waitAction {get; private set;}
////        public TaskFiber target {get; private set;}
////        internal FiberAction(Action action, TaskFiber targetFiber)
////        {
////            if (action == null)
////                throw new ArgumentNullException("action");
////            if (targetFiber == null)
////                throw new ArgumentNullException("targetFiber");
////            waitAction = action;
////            target = targetFiber;
////        }
////
////        public void Continue(TaskFiber fiber, Action updateCoroutine)
////        {
////            target.Enqueue(() =>
////                {
////                    try
////                    {
////                        waitAction();
////                    }
////                    finally
////                    {
////                        fiber.Enqueue(updateCoroutine);
////                    }
////                });
////        }
////    }
//}
