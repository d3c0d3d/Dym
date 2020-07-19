using Dym;
using System;
using System.Collections.Generic;

namespace ModuleTester1
{
    public class MainTester1 : ModuleEx, IModule
    {
        [MethodInvoker("print-message", typeof(object))]
        private void PrintMessage(object message)
        {
            List<TransportMessageBase> messages = new List<TransportMessageBase>();

            if (message is List<TransportMessageBase>)
                messages = (message as List<TransportMessageBase>);

            messages.ForEach(x => Console.WriteLine($"Receveid -> {x}"));
        }
    }
}
