using System;
using System.Threading;
using System.Threading.Tasks;

namespace Locus.Threading
{

    public class MessageFiber : MessageFiberBase
    {
        /// <summary>
        /// Create new Instance of MessageFiber<typeparamref name="T"/>
        /// MessageFiber can run Task, Action, or overriden OnMessage<typeparamref name="T"/> function
        /// </summary>
        public MessageFiber() : base() { }

        internal override void InvokeMessage(MessageNodeBase message)
        {
            message.Invoke();
        }
    }


    public class MessageFiber<T> : MessageFiberBase
    {
        /// <summary>
        /// Create new Instance of MessageFiber<typeparamref name="T"/>
        /// MessageFiber can run Task, Action, or overriden OnMessage<typeparamref name="T"/> function
        /// </summary>
        public MessageFiber() : base() { }

        internal override void InvokeMessage(MessageNodeBase message)
        {
            var typedMessage = message as MessageNode<T>;
            if (typedMessage == null)
                message.Invoke();
            else
                OnMessage(typedMessage.Message);
        }

        protected virtual void OnMessage(T message)
        {

        }

        /// <summary>
        /// Enqueue a message to this fiber.
        /// The message will be executed on a threadpool thread with OnMessage implementation
        /// Each call is thread safe
        /// </summary>
        /// <param name="message">Message to enqueue</param>
        public void EnqueueMessage(T message)
        {
            var newTail = NodePool<MessageNode<T>>.Pop();
            newTail.Message = message;
            EnqueueInternal(newTail);
        }
    }

}

