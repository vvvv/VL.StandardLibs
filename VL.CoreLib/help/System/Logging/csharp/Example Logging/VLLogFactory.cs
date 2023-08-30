using Microsoft.Extensions.Logging;
using VL.Core;

namespace VL.System.Logging
{
    /// <summary>
    /// Publishes all logged messages via a message broker (<see cref="Broker"/>) and allows to create loggers bound to
    /// a specific <see cref="NodeContext"/>. Those loggers will automatically attach their node context to each log message
    /// which in turn can later be accessed via the <see cref="LogMessage.NodeContext"/> property.
    /// </summary>
    public sealed class VLLogFactory : ILoggerFactory
    {
        public static VLLogFactory GetForApp(AppHost appHost)
        {
            return appHost.Services.GetOrAddService(_ =>
            {

                return new VLLogFactory(appHost);
            });
        }

        private readonly AppHost _appHost;
        private readonly ILoggerFactory _loggerFactory;

        private VLLogFactory(AppHost appHost)
        {
            _appHost = appHost;
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                // Only for testing - the .Console package can be removed as well
                builder.AddSimpleConsole();
                builder.AddProvider(Broker);
            });
        }

        public LogMessageBroker Broker { get; } = new LogMessageBroker();

        public void AddProvider(ILoggerProvider provider)
        {
            _loggerFactory.AddProvider(provider);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }

        public ILogger CreateLogger(NodeContext nodeContext)
        {
            var category = InferCategory(nodeContext) ?? "";
            // Let MEL build a logger for the infered category
            var logger = CreateLogger(category);
            // Wrap that logger with one which will always attach the node context to the message
            return new VLLogger(logger, nodeContext);

            string? InferCategory(NodeContext nodeContext)
            {
                // First one is the node itself in which we're not interested here
                return GetTypes(nodeContext).Skip(1).FirstOrDefault()?.FullName;
            }

            IEnumerable<IVLTypeInfo> GetTypes(NodeContext nodeContext)
            {
                var stack = nodeContext.Path.Stack;
                while (!stack.IsEmpty)
                {
                    var typeInfo = _appHost.TypeRegistry.GetTypeById(stack.Peek());
                    if (typeInfo != null)
                        yield return typeInfo;

                    stack = stack.Pop();
                }
            }
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }

        sealed class VLLogger : ILogger
        {
            private readonly NodeContext _nodeContext;
            private readonly ILogger _logger;

            public VLLogger(ILogger logger, NodeContext nodeContext)
            {
                _nodeContext = nodeContext;
                _logger = logger;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return _logger.BeginScope(state);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return _logger.IsEnabled(logLevel);
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) 
                    return;

                var message = new Message<TState>(_nodeContext, state, formatter);
                _logger.Log(logLevel, eventId, message, exception, Message<TState>.s_Formatter);
            }

            record struct Message<TState>(NodeContext NodeContext, TState State, Func<TState, Exception?, string> Formatter) : ILogMessageWithNodeContext
            {
                public static readonly Func<Message<TState>, Exception?, string> s_Formatter = Format;

                private static string Format(Message<TState> message, Exception? exception)
                {
                    return message.Formatter(message.State, exception);
                }
            }
        }
    }

    interface ILogMessageWithNodeContext
    {
        NodeContext NodeContext { get; }
    }
}