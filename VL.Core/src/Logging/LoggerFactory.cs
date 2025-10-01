#nullable enable
using Microsoft.Extensions.Logging;
using System;

namespace VL.Core.Logging
{
    /// <summary>
    /// Logged messages get recorded by the <see cref="GlobalRecorder"/>.
    /// The recorder can be configured with the <see cref="LogRecorderOptions"/> during <see cref="IStartup.SetupConfiguration(AppHost, Microsoft.Extensions.Configuration.IConfigurationBuilder)"/>.
    /// </summary>
    public abstract class LoggerFactory : ILoggerFactory
    {
        /// <summary>
        /// Holds all the log messages from all apps. To configure use <see cref="IStartup.SetupConfiguration(AppHost, Microsoft.Extensions.Configuration.IConfigurationBuilder)"/> and <see cref="LogRecorderOptions"/>.
        /// </summary>
        public static LogRecorder? GlobalRecorder => AppHost.Global.LoggerFactory.Recorder;

        protected abstract LogRecorder? Recorder { get; }

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