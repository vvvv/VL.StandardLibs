using Stride.Core;
using Stride.Core.Serialization.Contents;

namespace VL.Stride.Assets
{
    /// <summary>
    /// Represets a reference to a runtime asset
    /// </summary>
    public abstract class AssetWrapperBase
    {
        protected int LoadRequests;

        public bool Loading { get; set; }

        public bool Exists { get; set; }

        public string Name { get; set; }

        public void AddLoadRequest() => LoadRequests++;

        public abstract void SetAssetObject(object asset);

        public abstract void ProcessLoadRequests(ContentManager contentManager, string url);
    }

    public class AssetWrapper<T> : AssetWrapperBase where T : class
    {
        T Asset;

        public void SetAsset(T asset)
        {
            Asset = asset;
        }

        public void SetValues(T asset, bool exists)
        {
            Asset = asset;
            Exists = exists;
        }

        public void GetValues(out T asset, out bool exists, out bool loading)
        {
            asset = Asset;
            exists = Exists;
            loading = Loading;
        }

        public override void SetAssetObject(object asset)
        {
            Asset = (T)asset;

            if (Asset is ComponentBase componentBase && !string.IsNullOrWhiteSpace(Name))
                componentBase.Name = Name;

        }

        public override void ProcessLoadRequests(ContentManager contentManager, string url)
        {
            for (int i = 0; i < LoadRequests - 1; i++)
            {
                contentManager.Load<T>(url);
            }
            LoadRequests = 0;
        }
    }
}
