using Dym.Logging;
using Dym.OptionCommand;
using Dym.Util;
using Dym.Extensions;
using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dym
{
    public abstract class ModuleEx
    {
        public IModuleHost _host;
        public string _ownHash;
        public Guid _uid;
        /// <summary>
        /// -result
        /// </summary>
        public const string RETFLAG = "-result";
        private const string UNSUPPORTFLAG = "unsupported";

        private OptionSet _optionSet;

        public static readonly Logger _logger = LoggerFactory.CreateLogger(LogLevel.Info, Utilities.GetEnvLoggerFile(Constants.MFL.ToStr()));

        public Guid _instanceUidFrom;
        public string[] _instanceMessages;

        public ModuleType ModuleType { get; set; }

        public byte[] Uid { get; set; }

        public string SessionKey { get; set; }
        public string FriendlyName { get; set; }

        public Version Version { get; set; }

        public ModuleEx()
        {
            SessionKey = Nanoid.Generate(Constants.NID.ToStr(), 7);

            _optionSet = new OptionSet().Add(UNSUPPORTFLAG, _ => UnSupported());
        }

        public ModuleEx MethodRegistry(string name, Action<string> action)
        {
            _optionSet.Add(name, action);
            return this;
        }

        public virtual void Startup(IModuleHost moduleHost, string ownHash)
        {
            _host = moduleHost;
            _ownHash = ownHash;
        }

        public virtual void Load(params string[] parms)
        {
            string inline = parms?.Aggregate<string, string>(null, (m1, m2) => (m1.IsNull() ? m1 : m1 + "|") + m2);
            PrintLog($"{(!inline.IsNull() ? $"{inline}|" : "")}{nameof(Load)}");
        }

        public virtual void Dispose(params string[] parms)
        {
            string inline = parms?.Aggregate<string, string>(null, (m1, m2) => (m1.IsNull() ? m1 : m1 + "|") + m2);
            PrintLog($"{(!inline.IsNull() ? $"{inline}|" : "")}{nameof(Dispose)}");

        }

        public virtual void MessageCallback(Guid uidFrom, string name, params string[] messages)
        {
            string inline = messages?.Aggregate<string, string>(null, (m1, m2) => (m1.IsNull() ? m1 : m1 + "|") + m2);
            PrintLog($"{(!inline.IsNull() ? $"{inline}|" : "")}{nameof(MessageCallback)}");

            ProcessMessages(uidFrom, name, messages);
        }

        private void ProcessMessages(Guid uidFrom, string name, string[] messages)
        {
            _instanceUidFrom = uidFrom;
            _instanceMessages = messages;

            // todo: future update
            if (name.EndsWith(RETFLAG))
            {
                if (ExecResultMethod(name))
                    return;
            }

            var ret = _optionSet.Parse(new[] { name });
            if (ret.Count > 0)
            {
                string addmessage = string.Empty;

                if (_instanceUidFrom == Guid.Empty) // message from modulehost
                {
                    addmessage = _instanceUidFrom.ToString();
                    _instanceUidFrom = _uid;
                }

                // send message
                _host.SendMessagesToModule(_uid, _instanceUidFrom, UNSUPPORTFLAG, name, addmessage);

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

        public virtual string UnSupported()
        {
            var module = _host.GetModuleLoadedInfos(_instanceUidFrom);
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Name: {module.Item3}");
            messageBuilder.AppendLine($"Module: {module.Item1}");
            messageBuilder.AppendLine($"Session: {SessionKey}");
            messageBuilder.AppendLine($"Message: {_instanceMessages.Aggregate<string, string>(null, (m1, m2) => (m1.IsNull() ? m1 : m1 + ", ") + m2)}");

            PrintLog($"{messageBuilder}", LogLevel.Warn);

            return messageBuilder.ToString();
        }

        public virtual string NotFoundModule(ModuleType moduleType)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Session: {SessionKey}");
            messageBuilder.AppendLine($"Module: {moduleType}");
            messageBuilder.AppendLine($"Message: {nameof(NotFoundModule)}");

            PrintLog($"{messageBuilder}", LogLevel.Warn);

            return messageBuilder.ToString();
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
