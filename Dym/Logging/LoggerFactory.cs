namespace ModuleFramework.Logging
{
    public static class LoggerFactory
    {
        private static Logger _loggerInstance;

        public static Logger CreateLogger(LogLevel level, string file)
        {
            return _loggerInstance ?? (_loggerInstance = new Logger(level, file));
        }

        public static Logger CreateLogger()
        {
            return _loggerInstance ?? (_loggerInstance = new Logger());
        }
    }
}
