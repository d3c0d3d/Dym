using System;
using System.Diagnostics;
using System.Text;

namespace Dym.Logging
{
    /// <summary>
    /// Represents a log data used by the <see cref="Logger"/> class.
    /// </summary>
    public class LogData
    {
        public LogData(LogLevel level, StackFrame caller, string message)
        {
            Level = level;
            Caller = caller;
            Message = message ?? string.Empty;
            Date = DateTime.Now;
        }

        /// <summary>
        /// Gets the information of the logging method caller.
        /// </summary>
        /// <value>
        /// A <see cref="StackFrame"/> that provides the information of the logging method caller.
        /// </value>
        public StackFrame Caller { get; }

        /// <summary>
        /// Gets the date and time when the log data was created.
        /// </summary>
        /// <value>
        /// A <see cref="DateTime"/> that represents the date and time when the log data was created.
        /// </value>
        public DateTime Date { get; }

        /// <summary>
        /// Gets the logging level of the log data.
        /// </summary>
        /// <value>
        /// One of the <see cref="LogLevel"/> enum values, indicates the logging level of the log data.
        /// </value>
        public LogLevel Level { get; }

        /// <summary>
        /// Gets the message of the log data.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the message of the log data.
        /// </value>
        public string Message { get; }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="LogData"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current <see cref="LogData"/>.
        /// </returns>
        public override string ToString()
        {
            var header = $"{Date}|{Level,-5}|";
            var method = Caller.GetMethod();
            var type = method.DeclaringType;
#if DEBUG
            var lineNum = Caller.GetFileLineNumber();
            var headerAndCaller = $"{header}{type.Name}.{method.Name}:{lineNum}|";
#else
            var headerAndCaller = String.Format("{0}{1}.{2}|", header, type.Name, method.Name);
#endif
            var msgs = Message.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            if (msgs.Length <= 1)
                return $"{headerAndCaller}{Message}";

            var buff = new StringBuilder($"{headerAndCaller}{msgs[0]}\n", 64);

            var fmt = $"{{0,{header.Length}}}{{1}}\n";
            for (var i = 1; i < msgs.Length; i++)
                buff.AppendFormat(fmt, "", msgs[i]);

            buff.Length--;
            return buff.ToString();
        }
    }
}
