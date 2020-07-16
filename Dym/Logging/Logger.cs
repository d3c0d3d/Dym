using System;
using System.Diagnostics;
using System.IO;

namespace Dym.Logging
{
    /// <summary>
    /// Provides a set of methods and properties for logging.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   If you output a log with lower than the value of the <see cref="Level"/> property,
    ///   it cannot be outputted.
    ///   </para>
    ///   <para>
    ///   The default output action writes a log to the standard output stream and the log file
    ///   if the <see cref="File"/> property has a valid path to it.
    ///   </para>
    ///   <para>
    ///   If you would like to use the custom output action, you should set
    ///   the <see cref="Output"/> property to any <c>Action&lt;LogData, string&gt;</c>
    ///   delegate.
    ///   </para>
    /// </remarks>
    public class Logger
    {
        private volatile string _file;
        private volatile LogLevel _level;
        private Action<LogData, string, bool> _output;
        private readonly object _sync;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor initializes the current logging level with <see cref="LogLevel.Error"/>.
        /// </remarks>
        public Logger()
          : this(LogLevel.Error, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class with
        /// the specified logging <paramref name="level"/>, path to the log <paramref name="file"/>,
        /// and <paramref name="output"/> action.
        /// </summary>
        /// <param name="level">
        /// One of the <see cref="LogLevel"/> enum values.
        /// </param>
        /// <param name="file">
        /// A <see cref="string"/> that represents the path to the log file.
        /// </param>
        /// <param name="output">
        /// An <c>Action&lt;LogData, string&gt;</c> delegate that references the method(s) used to
        /// output a log. A <see cref="string"/> parameter passed to this delegate is
        /// <paramref name="file"/>.
        /// </param>
        public Logger(LogLevel level, string file = null, Action<LogData, string, bool> output = null)
        {
            _level = level;
            _file = file;
            _output = output ?? defaultOutput;
            _sync = new object();
        }

        /// <summary>
        /// Gets or sets the current path to the log file.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the current path to the log file if any.
        /// </value>
        public string File
        {
            get
            {
                return _file;
            }

            set
            {
                lock (_sync)
                {
                    _file = value;
                    Warn($"The current path to the log file has been changed to {_file}.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the current logging level.
        /// </summary>
        /// <remarks>
        /// A log with lower than the value of this property cannot be outputted.
        /// </remarks>
        /// <value>
        /// One of the <see cref="LogLevel"/> enum values, specifies the current logging level.
        /// </value>
        public LogLevel Level
        {
            get
            {
                return _level;
            }

            set
            {
                lock (_sync)
                {
                    _level = value;
                    Warn($"The current logging level has been changed to {_level}.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the current output action used to output a log.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <c>Action&lt;LogData, string&gt;</c> delegate that references the method(s) used to
        ///   output a log. A <see cref="string"/> parameter passed to this delegate is the value of
        ///   the <see cref="File"/> property.
        ///   </para>
        ///   <para>
        ///   If the value to set is <see langword="null"/>, the current output action is changed to
        ///   the default output action.
        ///   </para>
        /// </value>
        public Action<LogData, string, bool> Output
        {
            get
            {
                return _output;
            }

            set
            {
                lock (_sync)
                {
                    _output = value ?? defaultOutput;
                    Warn("The current output action has been changed.");
                }
            }
        }

        private static void defaultOutput(LogData data, string path, bool printOnConsole)
        {
            var log = data.ToString();

            if (printOnConsole)
            {
                switch (data.Level)
                {
                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogLevel.Trace:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogLevel.Info:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                }

                Console.WriteLine(log);
                Console.ResetColor();
            }

            if (!string.IsNullOrEmpty(path))
                writeToFile(log, path);
        }

        private void output(string message, LogLevel level, bool printOnConsole)
        {
            lock (_sync)
            {
                if (_level > level)
                    return;
                if (level == LogLevel.None)
                    return;

                LogData data = null;
                try
                {
                    data = new LogData(level, new StackFrame(2, true), message);
                    _output(data, _file, printOnConsole);
                }
                catch (Exception ex)
                {
                    if (level == LogLevel.None)
                        return;

                    data = new LogData(LogLevel.Fatal, new StackFrame(0, true), ex.Message);
                    Console.WriteLine(data.ToString());
                }
            }
        }

        private static void writeToFile(string value, string path)
        {
            using (var writer = new StreamWriter(path, true))
            using (var syncWriter = TextWriter.Synchronized(writer))
                syncWriter.WriteLine(value);
        }

        /// <summary>
        /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Debug"/>.
        /// </summary>
        /// <remarks>
        /// If the current logging level is higher than <see cref="LogLevel.Debug"/>,
        /// this method doesn't output <paramref name="message"/> as a log.
        /// </remarks>
        /// <param name="message">
        /// A <see cref="string"/> that represents the message to output as a log.
        /// </param>
        /// <param name="printOnConsole">
        /// Indiques if message is printable in console (default is false)
        /// </param>
        public void Debug(string message, bool printOnConsole = false)
        {
            if (_level > LogLevel.Debug)
                return;

            output(message, LogLevel.Debug, printOnConsole);
        }

        /// <summary>
        /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Error"/>.
        /// </summary>
        /// <remarks>
        /// If the current logging level is higher than <see cref="LogLevel.Error"/>,
        /// this method doesn't output <paramref name="message"/> as a log.
        /// </remarks>
        /// <param name="message">
        /// A <see cref="string"/> that represents the message to output as a log.
        /// </param>
        /// <param name="printOnConsole">
        /// Indiques if message is printable in console (default is false)
        /// </param>
        public void Error(string message, bool printOnConsole = false)
        {
            if (_level > LogLevel.Error)
                return;

            output(message, LogLevel.Error, printOnConsole);
        }

        /// <summary>
        /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Fatal"/>.
        /// </summary>
        /// <param name="message">
        /// A <see cref="string"/> that represents the message to output as a log.
        /// </param>
        /// <param name="printOnConsole">
        /// Indiques if message is printable in console (default is false)
        /// </param>
        public void Fatal(string message, bool printOnConsole = false)
        {
            output(message, LogLevel.Fatal, printOnConsole);
        }

        /// <summary>
        /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Info"/>.
        /// </summary>
        /// <remarks>
        /// If the current logging level is higher than <see cref="LogLevel.Info"/>,
        /// this method doesn't output <paramref name="message"/> as a log.
        /// </remarks>
        /// <param name="message">
        /// A <see cref="string"/> that represents the message to output as a log.
        /// </param>
        /// <param name="printOnConsole">
        /// Indiques if message is printable in console (default is false)
        /// </param>
        public void Info(string message, bool printOnConsole = false)
        {
            if (_level > LogLevel.Info)
                return;

            output(message, LogLevel.Info, printOnConsole);
        }

        /// <summary>
        /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Trace"/>.
        /// </summary>
        /// <remarks>
        /// If the current logging level is higher than <see cref="LogLevel.Trace"/>,
        /// this method doesn't output <paramref name="message"/> as a log.
        /// </remarks>
        /// <param name="message">
        /// A <see cref="string"/> that represents the message to output as a log.
        /// </param>
        /// <param name="printOnConsole">
        /// Indiques if message is printable in console (default is false)
        /// </param>
        public void Trace(string message, bool printOnConsole = false)
        {
            if (_level > LogLevel.Trace)
                return;

            output(message, LogLevel.Trace, printOnConsole);
        }

        /// <summary>
        /// Outputs <paramref name="message"/> as a log with <see cref="LogLevel.Warn"/>.
        /// </summary>
        /// <remarks>
        /// If the current logging level is higher than <see cref="LogLevel.Warn"/>,
        /// this method doesn't output <paramref name="message"/> as a log.
        /// </remarks>
        /// <param name="message">
        /// A <see cref="string"/> that represents the message to output as a log.
        /// </param>
        /// <param name="printOnConsole">
        /// Indiques if message is printable in console (default is false)
        /// </param>
        public void Warn(string message, bool printOnConsole = false)
        {
            if (_level > LogLevel.Warn)
                return;

            output(message, LogLevel.Warn, printOnConsole);
        }
    }
}
