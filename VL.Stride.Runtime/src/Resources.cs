using System;
using System.Linq;
using VL.Core;
using VL.Lib.Basics.Resources;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Core.Annotations;
using Stride.Input;

namespace VL.Stride
{
    public static class Resources
    {
        // Game
        [Obsolete("Use the the IServiceProvider overload")]
        public static IResourceProvider<Game> GetGameProvider(this NodeContext nodeContext)
        {
            return ServiceRegistry.Current.GetGameProvider();
        }

        [Obsolete("Use the the IServiceProvider overload")]
        public static IResourceHandle<Game> GetGameHandle(this NodeContext nodeContext)
        {
            return ServiceRegistry.Current.GetGameProvider().GetHandle();
        }

        // Graphics device
        [Obsolete("Use the the IServiceProvider overload")]
        public static IResourceProvider<GraphicsDevice> GetDeviceProvider(this NodeContext nodeContext)
        {
            return ServiceRegistry.Current.GetDeviceProvider();
        }

        [Obsolete("Use the the IServiceProvider overload")]
        public static IResourceHandle<GraphicsDevice> GetDeviceHandle(this NodeContext nodeContext)
        {
            return ServiceRegistry.Current.GetDeviceProvider().GetHandle();
        }

        // Graphics context
        [Obsolete("Use the the IServiceProvider overload")]
        public static IResourceProvider<GraphicsContext> GetGraphicsContextProvider(this NodeContext nodeContext)
        {
            return ServiceRegistry.Current.GetGraphicsContextProvider();
        }

        [Obsolete("Use the the IServiceProvider overload")]
        public static IResourceHandle<GraphicsContext> GetGraphicsContextHandle(this NodeContext nodeContext)
        {
            return ServiceRegistry.Current.GetGraphicsContextProvider().GetHandle();
        }

        // Input manager
        [Obsolete("Use the the IServiceProvider overload")]
        public static IResourceProvider<InputManager> GetInputManagerProvider(this NodeContext nodeContext)
        {
            return ServiceRegistry.Current.GetInputManagerProvider();
        }

        [Obsolete("Use the the IServiceProvider overload")]
        public static IResourceHandle<InputManager> GetInputManagerHandle(this NodeContext nodeContext)
        {
            return ServiceRegistry.Current.GetInputManagerProvider().GetHandle();
        }

        // Game
        public static IResourceProvider<Game> GetGameProvider(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IResourceProvider<Game>>();
        }

        public static IResourceHandle<Game> GetGameHandle(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetGameProvider().GetHandle();
        }

        // Graphics device
        public static IResourceProvider<GraphicsDevice> GetDeviceProvider(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IResourceProvider<GraphicsDevice>>();
        }

        public static IResourceHandle<GraphicsDevice> GetDeviceHandle(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetDeviceProvider().GetHandle();
        }

        // Graphics context
        public static IResourceProvider<GraphicsContext> GetGraphicsContextProvider(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IResourceProvider<GraphicsContext>>();
        }

        public static IResourceHandle<GraphicsContext> GetGraphicsContextHandle(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetGraphicsContextProvider().GetHandle();
        }

        // Input manager
        public static IResourceProvider<InputManager> GetInputManagerProvider(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<IResourceProvider<InputManager>>();
        }

        public static IResourceHandle<InputManager> GetInputManagerHandle(this IServiceProvider serviceProvider)
        {
            return serviceProvider.GetInputManagerProvider().GetHandle();
        }
    }

    
}
