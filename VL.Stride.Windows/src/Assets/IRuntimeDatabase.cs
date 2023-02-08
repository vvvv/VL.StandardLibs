// Copyright (c) Stride contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;

namespace VL.Stride.Assets
{
    public interface IRuntimeDatabase : IDisposable
    {
        Task<ISyncLockable> ReserveSyncLock();
        Task<IDisposable> LockAsync();
        Task Build(AssetItem x, BuildDependencyType dependencyType);
        Task<IDisposable> MountInCurrentMicroThread();

        void ResetDependencyCompiler();
    }
}