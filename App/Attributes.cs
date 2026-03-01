using System;
using System.Collections.Generic;
using System.Text;

namespace ValSharp_Demo
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class CommandAttribute : Attribute
    {
        public readonly string Command;

        public CommandAttribute(string command)
        {
            Command = command;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class IncomingAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class OutgoingAttribute : Attribute { }
}
