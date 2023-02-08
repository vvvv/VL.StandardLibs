using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using VL.Stride.Assets;
using Stride.Core.Assets;
using Stride.Engine;
using System.Linq;
using System;

namespace VL.Stride.Assets
{
    /// <summary>
    /// Custom vl script that sets MSBuild
    /// </summary>
    public class AssetBuilderServiceScript : AsyncScript
    {
        public RuntimeContentLoader ContentLoader;
        ConcurrentQueue<AssetItem> workQueue = new ConcurrentQueue<AssetItem>();

        public AssetBuilderServiceScript()
        {
            try
            {
                //set msbuild
                PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            }
            catch (Exception e)
            {
                Log.Warning("MSBuild not found", e);
            }
        }

        public void PushWork(IEnumerable<AssetItem> items)
        {
            foreach(var item in items)
                workQueue.Enqueue(item);
        }

        public void PushWork(AssetItem item)
        {
            workQueue.Enqueue(item);
        }


        public override async Task Execute()
        {
            while (true)
            {
                await Script.NextFrame();
                if (!workQueue.IsEmpty)
                {
                    var assetList = DequeueItems().ToList();
                    if (assetList.Count == 0)
                        return;

                    await ContentLoader?.BuildAndReloadAssetsInternal(assetList);
                }
            }
        }

        private IEnumerable<AssetItem> DequeueItems()
        {
            while (workQueue.TryDequeue(out var item))
                yield return item;
        }
    }
}
