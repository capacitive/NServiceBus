﻿namespace NServiceBus.Unicast.Behaviors
{
    using System;

    class MessageHandler
    {
        public object Instance { get; set; }
        public Action<object, object> Invocation { get; set; }
    }
}