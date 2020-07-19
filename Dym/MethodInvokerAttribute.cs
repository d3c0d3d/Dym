using System;

namespace Dym
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodInvokerAttribute : Attribute
    {
        public string MethodName { get; set; }
        public Type ArgumentsType { get; set; }
        public MethodInvokerAttribute(string methodName, Type argumentType)
        {
            MethodName = methodName;
            ArgumentsType = argumentType;
        }
    }
}
