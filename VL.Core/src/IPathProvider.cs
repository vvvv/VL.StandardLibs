using System;
using System.Collections.Generic;
using System.Linq;

namespace VL.Core
{
    public interface IPathProvider
    {
        /// <summary>
        /// The base path of the currently running application.
        /// </summary>
        string AppBasePath { get; }

        /// <summary>
        /// The document base path. Running inside the editor this refers to the directory of the document otherwise the directory of the executable.
        /// </summary>
        string GetDocumentBasePath(UniqueId documentId);

        /// <summary>
        /// The path of the locally installed package. If the package is not installed or the app is running as standalone this will return null.
        /// </summary>
        string GetPackagePath(string packageId);
    }

    public static partial class PathProviderUtils
    {
        /// <summary>
        /// Retrieves the base path of the current application. In case of a running patch it's the base path of the entry point.
        /// </summary>
        /// <param name="nodeContext">The node context to retrieve the entry point from.</param>
        /// <returns>The base path of the application.</returns>
        [Obsolete("Please use GetApplicationBasePath without any arguments")]
        public static string GetApplicationBasePath(this NodeContext nodeContext)
        {
            var pathProvider = GetPathProvider();
            return pathProvider.AppBasePath;
        }

        /// <summary>
        /// Retrieves the base path of the current application. In case of a running patch it's the base path of the entry point.
        /// </summary>
        /// <returns>The base path of the application.</returns>
        public static string GetApplicationBasePath()
        {
            var pathProvider = GetPathProvider();
            return pathProvider.AppBasePath;
        }

        /// <summary>
        /// Retrieves the base path of the current document.
        /// </summary>
        /// <param name="nodeContext">The node context to retrieve the current document from.</param>
        /// <returns>The base path of the current document.</returns>
        public static string GetDocumentBasePath(this NodeContext nodeContext)
        {
            var pathProvider = GetPathProvider();
            var documentId = nodeContext.Path.Stack?.FirstOrDefault();
            if (documentId is null)
                return pathProvider.AppBasePath;

            return pathProvider.GetDocumentBasePath(documentId.Value);
        }

        private static IPathProvider GetPathProvider()
        {
            return IAppHost.CurrentOrGlobal.Services.GetService<IPathProvider>() 
                ?? throw new InvalidOperationException("A path provider must be registered in the application.");
        }
    }
}
