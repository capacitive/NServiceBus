﻿namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;
    using Messages;

    class CreatePhysicalMessageBehavior:IBehavior<SendLogicalMessagesContext>
    {
        public MessageMetadataRegistry MessageMetadataRegistry { get; set; }

        public UnicastBus UnicastBus { get; set; }

        public PipelineFactory PipelineFactory { get; set; }

        public Address DefaultReplyToAddress { get; set; }

        public void Invoke(SendLogicalMessagesContext context, Action next)
        {
            var sendOptions = context.SendOptions;

            var toSend = new TransportMessage
            {
                MessageIntent = sendOptions.Intent,
                CorrelationId = sendOptions.CorrelationId,
                ReplyToAddress = sendOptions.ReplyToAddress
            };

            //apply static headers
            foreach (var kvp in UnicastBus.OutgoingHeaders)
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }

            //apply individual headers
            foreach(var kvp in context.LogicalMessages.SelectMany(m=>m.Headers))
            {
                toSend.Headers[kvp.Key] = kvp.Value;
            }
                
            if (toSend.ReplyToAddress == null)
            {
                toSend.ReplyToAddress = DefaultReplyToAddress;
            }

            //todo: pull this out to the distributor when we split it to a separate repo
            if (UnicastBus.PropagateReturnAddressOnSend)
            {
                var incomingMessage = PipelineFactory.CurrentTransportMessage;

                if (incomingMessage != null)
                {
                    sendOptions.ReplyToAddress = incomingMessage.ReplyToAddress;
                }
            }


            var messageDefinitions = context.LogicalMessages.Select(m => MessageMetadataRegistry.GetMessageDefinition(m.MessageType)).ToList();

            toSend.TimeToBeReceived = messageDefinitions.Min(md => md.TimeToBeReceived);
            toSend.Recoverable = messageDefinitions.Any(md => md.Recoverable);

            context.Set(toSend);

            PipelineFactory.InvokeSendPipeline(sendOptions,toSend);

            next();
        }
    }
}