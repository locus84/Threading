﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Locus.Threading
{
    /// <summary>
    /// When we scheduled a message in a fiber,
    /// that message object will be treated as MessageNodeBase
    /// But when we need to pool it for later usage(to avoid allocation)
    /// It should be pooled seperately based of it's auctual generic type
    /// </summary>
    internal class MessageNodeBase
    {
        public MessageNodeBase Next;
        public virtual void Invoke() { }
        public virtual void PushToPool() { }
        public virtual void Clear() { }
    }

    internal class ContextPostMessageNode : MessageNodeBase
    {
        public SendOrPostCallback Delegate;
        public object State;

        public override void Invoke()
        {
            Delegate.Invoke(State);
        }

        public override void Clear()
        {
            Delegate = null;
            State = null;
        }

        public override void PushToPool()
        {
            NodePool<ContextPostMessageNode>.Push(this);
        }
    }

    internal class MessageNode<T> : MessageNodeBase
    {
        public T Message;

        public override void Clear()
        {
            Message = default(T);
        }

        public override void PushToPool()
        {
            NodePool<MessageNode<T>>.Push(this);
        }
    }

    internal class ActionMessageNode : MessageNode<Action>
    {
        public override void PushToPool()
        {
            NodePool<ActionMessageNode>.Push(this);
        }

        public override void Invoke()
        {
            Message.Invoke();
        }
    }

    internal class TaskMessageNode : MessageNode<Task>
    {
        public override void PushToPool()
        {
            NodePool<TaskMessageNode>.Push(this);
        }

        public override void Invoke()
        {
            Message.RunSynchronously();
        }
    }

    ////make this static so make use of thread safe functionality
    internal static class NodePool<T> where T : MessageNodeBase, new()
    {
        static NodePool()
        {
            head = tail = new T();
        }

        //as this class is generic, this variable will ge generated per type
        private static MessageNodeBase head, tail;

        public static void Push(T newNode)
        {
            newNode.Clear();
            newNode.Next = null;
            var prevTail = Atomic.Swap(ref tail, newNode);
            prevTail.Next = newNode;
            return;
        }

        public static T Pop()
        {
            //we don't use head always, just get next of the head
            //if nex of the head is null, make a new instance of the node.
            MessageNodeBase result, newHead;

            do
            {
                result = head;
                newHead = result.Next;
                if (newHead == null)
                    return new T();
            } while (!Atomic.SwapIfSame(ref head, newHead, result));
            //clear up next
            result.Next = null;
            return (T)result;
        }
    }
}
