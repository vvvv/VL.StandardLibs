#nullable enable
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Subjects;

namespace VL.Core.Logging
{
    /// <summary>
    /// Loggers created by this factory will publish all their messages through the <see cref="Messages"/> property.
    /// </summary>
    public abstract class LoggerFactory : ILoggerFactory
    {
        protected static readonly Subject<LogMessage> s_allMessages = new();

        /// <summary>
        /// The log messages from loggers created by all factories (from all apps).
        /// </summary>
        public static IObservable<LogMessage> AllMessages => s_allMessages;
        
        /// <summary>
        /// The log messages from loggers created by this factory (from this app).
        /// </summary>
        public abstract IObservable<LogMessage> Messages { get; }

        public abstract void AddProvider(ILoggerProvider provider);

        public abstract ILogger CreateLogger(string categoryName);

        /// <summary>
        /// Creates a new <see cref="ILogger"/>. The logger's category is infered from the node context if not set.
        /// </summary>
        /// <param name="categoryName">The category of the logger. If not set it will be infered from the node context.</param>
        /// <param name="nodeContext">The node context. Will be used to infer the category (if not specified) and can later be used to locate the patch associated with a logged message.</param>
        public abstract ILogger CreateLogger(string? categoryName, NodeContext nodeContext);

        public abstract void Dispose();
    }
}