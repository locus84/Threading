using System;
using System.Threading;
using System.Threading.Tasks;

namespace Locus.Threading
{
    internal class SingleNodeBase
    {
        public SingleNodeBase Next;

        public static bool CAS(ref SingleNodeBase toChange, SingleNodeBase newValue, SingleNodeBase comparand)
        {
            return ReferenceEquals(comparand, Interlocked.CompareExchange(ref toChange, newValue, comparand));
        }

        public virtual bool TryInvokeSelf() { return false; }
        public virtual void PushToPool() { }
        public virtual void Clear() { }
    }

    internal class SingleNode<T> : SingleNodeBase
    {
        public T Item;

        public static SingleNode<T> PopFromPool()
        {
            return NodePool<SingleNode<T>>.Pop();
        }

        public override void Clear()
        {
            Item = default(T);
        }

        public override void PushToPool()
        {
            NodePool<SingleNode<T>>.Push(this);
        }
    }

    internal class SingleActionNode : SingleNode<Action>
    {
        public new static SingleActionNode PopFromPool()
        {
            return NodePool<SingleActionNode>.Pop();
        }

        public override void PushToPool()
        {
            NodePool<SingleActionNode>.Push(this);
        }

        public override bool TryInvokeSelf()
        {
            Item.Invoke();
            return true;
        }
    }

    internal class SingleTaskNode : SingleNode<Task>
    {
        public new static SingleTaskNode PopFromPool()
        {
            return NodePool<SingleTaskNode>.Pop();
        }

        public override void PushToPool()
        {
            NodePool<SingleTaskNode>.Push(this);
        }

        public override bool TryInvokeSelf()
        {
            Item.RunSynchronously();
            return true;
        }
    }

    //internal class SingleTaskNode<TResult> : SingleNode<Task<TResult>>
    //{
    //    public new static SingleTaskNode<TResult> PopFromPool()
    //    {
    //        return NodePool<SingleTaskNode<TResult>>.Pop();
    //    }

    //    public override void PushToPool()
    //    {
    //        NodePool<SingleTaskNode<TResult>>.Push(this);
    //    }
    //}


    //make this static so make use of thread safe functionality
    internal static class NodePool<T> where T : SingleNodeBase, new ()
    {
        //as this class is generic, this variable will ge generated per type
        private static T head = new T();

        public static void Push(T newNode)
        {
            //attach this to next of head.
            //so assign head's next as new node's next.
            //now set head's next into new node, previous next will be
            //tailed as next of newnode.
            newNode.Clear();
            //we don't need to clear it's next variable because it'll be replaced anyway.
            //but we have to clear it's item value
            do
            {
                newNode.Next = head.Next;
            } while (!SingleNodeBase.CAS(ref head.Next, newNode, newNode.Next));
            return;
        }

        public static T Pop()
        {
            //we don't use head always, just get next of the head
            //if nex of the head is null, make a new instance of the node.
            T result;
            do
            {
                result = head.Next as T;
                if (result == null)
                    return new T();
            } while (!SingleNode<T>.CAS(ref head.Next, result.Next, result));
            //clear up next
            result.Next = null;
            return result;
        }
    }
}
