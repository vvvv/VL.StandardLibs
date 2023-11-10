using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VL.Core
{
    /// <summary>
    /// Implement to take part in the application startup process.
    /// </summary>
    public interface IStartup
    {
        /// <summary>
        /// Sets up the configuration of the app. This is called first when the app starts.
        /// </summary>
        /// <param name="appHost">The app host - not all properties can be accessed yet, as it is still getting initialized.</param>
        /// <param name="configurationBuilder">Used to build the configuration of the app which will later be accessible via the <see cref="AppHost.Configuration"/> property.</param>
        void SetupConfiguration(AppHost appHost, IConfigurationBuilder configurationBuilder);

        /// <summary>
        /// Sets up the logging of the app. This is called after the configuration has been initialized.
        /// </summary>
        /// <param name="appHost">The app host - not all properties can be accessed yet, as it is still getting initialized.</param>
        /// <param name="loggingBuilder">Used to configure the logging facilities of the app. Loggers can later be created via the <see cref="AppHost.LoggerFactory"/>.</param>
        void SetupLogging(AppHost appHost, ILoggingBuilder loggingBuilder);

        /// <summary>
        /// Configure any remaining services of the app. All base services (like logging) are available. Called last.
        /// </summary>
        /// <param name="appHost">The app host to configure.</param>
        void Configure(AppHost appHost);
    }
}
