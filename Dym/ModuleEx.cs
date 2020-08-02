using Dym.Logging;
using Dym.Util;
using Dym.Extensions;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Dym
{
    public abstract class ModuleEx
    {
        public IModuleHost _host;
        public string _ownHash;

        private const string UNSUPPORTFLAG = "__unsupported";
        private const string NOTFOUNDFLAG = "__notfound";

        public static readonly Logger _logger = LoggerFactory.CreateLogger(LogLevel.Info, Utilities.GetEnvLoggerFile(Constants.MFL.ToStr()));

        public ModuleType ModuleType { get; set; } = ModuleType.Undefined;
        public byte[] Uid { get; private set; }
        public string SessionKey { get; set; } = Nanoid.Generate(Constants.NID.ToStr(), 7);
        public string FriendlyName { get; set; } = Assembly.GetCallingAssembly().GetName().Name;
        public Version Version { get; set; } = Assembly.GetCallingAssembly().GetName().Version;
        public Guid _uid => new Guid(Uid);

        private MethodInvoker<MethodInvokerAttribute>[] _instanceMethodsInvokers;        

        public ModuleEx()
        {
            _instanceMethodsInvokers = GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
             .Where(x => x.GetCustomAttribute(typeof(MethodInvokerAttribute)) != null)
               .Select(x =>
                   new MethodInvoker<MethodInvokerAttribute>(
                       (MethodInvokerAttribute)x.GetCustomAttribute(typeof(MethodInvokerAttribute)),
                       x)
               ).ToArray();

            var attribute = Assembly.GetCallingAssembly().GetCustomAttribute(typeof(AssemblyProductAttribute)) as AssemblyProductAttribute;
            Debug.WriteLine("Product: " + attribute.Product);

            Guid.TryParse(attribute.Product, out Guid guid);
            if (guid == Guid.Empty)
                Uid = Guid.NewGuid().ToByteArray();
            else Uid = guid.ToByteArray();
        }

        public virtual void Startup(IModuleHost moduleHost, string ownHash)
        {
            _host = moduleHost;
            _ownHash = ownHash;
        }

        public virtual void Load(object parms)
        {
            FormattedPrintLog(nameof(Load), parms);
        }

        public virtual void Dispose(object parms)
        {
            FormattedPrintLog(nameof(Dispose), parms);
        }

        public virtual void IncomingMessageCallback(Guid uidFrom, string name, object messages)
        {
            FormattedPrintLog(nameof(IncomingMessageCallback), messages);

            IncomingMessageResolver(uidFrom, name, messages);
        }

        private void IncomingMessageResolver(Guid uidFrom, string name, object messages)
        {

            var instMethodByName = _instanceMethodsInvokers.Where(m => m.Attribute.MethodName == name);

            // invoke method
            if (instMethodByName.Any()) // todo: NotFoundModule
                switch(name)
                {
                    case UNSUPPORTFLAG:
                        _instanceMethodsInvokers.FirstOrDefault(m => m.Attribute.MethodName == name)
                            .TargetMethod.Invoke(this, new object[] { uidFrom, messages });
                        break;
                    default:
                        _instanceMethodsInvokers.FirstOrDefault(m => m.Attribute.MethodName == name)
                            .TargetMethod.Invoke(this, new object[] { messages });
                        break;
                }                
            else
            {
                _host.SendMessagesToModule(_uid, uidFrom, UNSUPPORTFLAG, messages);
            }
        }
        private bool ExecResultMethod(string name)
        {
            bool execMethod = false;

            var buildName = name.ToCamelCase();

            var instMethod = GetType().GetMethod(buildName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (instMethod != null)
            {
                instMethod.Invoke(this, null);
                execMethod = true;
            }

            return execMethod;
        }

        [MethodInvoker(UNSUPPORTFLAG, typeof(string))]
        public virtual string UnSupported(Guid uidFrom, object message)
        {
            var module = _host.GetModuleLoadedInfos(uidFrom);
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Name: {module.Value.FriendlyName}");            
            messageBuilder.AppendLine($"Module: {module.Value.Uid}");
            messageBuilder.AppendLine($"Session: {module.Value.SessionKey}");
            messageBuilder.AppendLine($"Message: {(message as List<TransportMessageBase>).Aggregate<TransportMessageBase, string>(null, (m1, m2) => (m1.IsNull() ? m1 : m1 + ", ") + m2)}");

            PrintLog($"{messageBuilder}", LogLevel.Warn);

            return messageBuilder.ToString();
        }

        [MethodInvoker(NOTFOUNDFLAG, typeof(ModuleType))]
        public virtual string NotFoundModule(ModuleType moduleType)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Session: {SessionKey}");
            messageBuilder.AppendLine($"Module: {moduleType}");
            messageBuilder.AppendLine($"Message: {nameof(NotFoundModule)}");

            PrintLog($"{messageBuilder}", LogLevel.Warn);

            return messageBuilder.ToString();
        }

        private void FormattedPrintLog(string methodName, object message)
        {
            string inline = string.Empty;

            if (message is List<TransportMessageBase>)
                inline = (message as List<TransportMessageBase>)?.Aggregate<TransportMessageBase, string>(null, (m1, m2) => (m1.IsNull() ? m1 : m1 + "|") + m2);

            PrintLog($"{methodName}{(!inline.IsNull() ? $" => {inline}|" : "")}");
        }

        public void PrintLog(string text, LogLevel logLevel = LogLevel.Info)
        {
            var logBuilder = new StringBuilder();
            logBuilder.Append($"{_ownHash.ToShortStr(10)}");
            logBuilder.Append("|");
            logBuilder.Append($"{SessionKey}");
            logBuilder.Append("|");
            logBuilder.Append($"{FriendlyName}");
            logBuilder.Append(" => ");

            switch (logLevel)
            {
                case LogLevel.Trace:
                    _logger.Info(logBuilder.ToString() + text);
                    break;
                case LogLevel.Debug:
                    _logger.Debug(logBuilder.ToString() + text);
                    break;
                case LogLevel.Info:
                    _logger.Info(logBuilder.ToString() + text);
                    break;
                case LogLevel.Warn:
                    _logger.Warn(logBuilder.ToString() + text);
                    break;
                case LogLevel.Error:
                    _logger.Error(logBuilder.ToString() + text);
                    break;
                case LogLevel.Fatal:
                    _logger.Fatal(logBuilder.ToString() + text);
                    break;
                case LogLevel.None:
                    break;
            }
        }
    }
}
