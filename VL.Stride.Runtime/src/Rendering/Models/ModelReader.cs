#nullable enable
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System.ComponentModel;
using VL.Core;
using Path = VL.Lib.IO.Path;
using StrideModel = Stride.Rendering.Model;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// Loads a 3D model directly and in a blocking fashion from disk without caching in the Stride asset database.
    /// Should do the same as the FileModel node but not tested properly yet.
    /// </summary>
    [ProcessNode(Category = "Stride.Models.Experimental")]
    public sealed class ModelReader : IDisposable
    {
        private readonly IGraphicsDeviceService graphicsDeviceService;
        private string? _previousPath;
        private float _previousImportScale;
        private Vector3 _previousPivotPosition;
        private bool _previousMergeMeshes;
        private StrideModel? _cachedModel;

        public ModelReader(NodeContext nodeContext)
        {
            graphicsDeviceService = nodeContext.AppHost.Services.GetRequiredService<Game>().GraphicsDeviceManager;
        }

        public void Dispose()
        {
            _cachedModel?.ReleaseGraphicsResources();
        }

        public void Update(
            Path? path, 
            [DefaultValue(1.0f)] float importScale, 
            Vector3 pivotPosition, 
            [DefaultValue(true)] bool mergeMeshes, 
            bool reload, 
            out StrideModel? model)
        {
            var currentPath = path?.Value;
            
            // Check if any parameter has changed or reload is requested
            bool hasChanged = _previousPath != currentPath ||
                              _previousImportScale != importScale ||
                              _previousPivotPosition != pivotPosition ||
                              _previousMergeMeshes != mergeMeshes ||
                              reload;

            if (hasChanged)
            {
                // Update cached parameters
                _previousPath = currentPath;
                _previousImportScale = importScale;
                _previousPivotPosition = pivotPosition;
                _previousMergeMeshes = mergeMeshes;

                _cachedModel?.ReleaseGraphicsResources();

                // Load the model if path is valid
                if (!string.IsNullOrWhiteSpace(currentPath))
                {
                    _cachedModel = ModelHelpers.LoadModel(currentPath, importScale, pivotPosition, mergeMeshes, graphicsDeviceService);
                }
                else
                {
                    _cachedModel = null;
                }
            }

            model = _cachedModel;
        }
    }
}
