using Microsoft.Extensions.Logging;
using System;

namespace Lumper
{
    public class LumperLoggerFactory : ILoggerFactory
    {
        private static LumperLoggerFactory _instance;

        private ILoggerFactory _defaultLoggerFactory;

        static LumperLoggerFactory()
        {
            _instance = new LumperLoggerFactory();

        }

        private LumperLoggerFactory()
        {
            _defaultLoggerFactory = LoggerFactory.Create(
                    builder =>
                    {
                        builder.AddConsole();
                        builder.AddProvider(new LumperUILoggerProvider());
                    }
                );
        }

        public static ILoggerFactory GetInstance()
        {
            return _instance;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = _defaultLoggerFactory.CreateLogger(categoryName);

            return logger;
        }

        public void Dispose()
        {
            _defaultLoggerFactory?.Dispose();
        }
    }

    public class LumperUILoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return LumperUILogger.GetInstance();
        }

        public void Dispose()
        {
            
        }
    }

    public class LumperUILogger : ILogger
    {
        public event MyEventHandler? LogCreatedEvent;

        public delegate void LogCreated();  

        private static LumperUILogger _lumperUILogger;

        static LumperUILogger()
        {
            _lumperUILogger = new LumperUILogger();
        }

        private LumperUILogger()
        {

        }

        public static LumperUILogger GetInstance()
        {
            return _lumperUILogger;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogCreatedEvent?.Invoke(this, new LogEvent(logLevel, message: state.ToString()));
        }

        public delegate void MyEventHandler(object sender, LogEvent e);
    }

    public class LogEvent 
    {
        public DateTime DateTime { get; set; }
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; }

        public bool IsError => LogLevel == LogLevel.Error;
        public bool IsWarning => LogLevel == LogLevel.Warning;

        public LogEvent(LogLevel logLevel, string message)
        {
            DateTime = DateTime.Now;
            LogLevel = logLevel;
            Message = message;
        }


    }
}
