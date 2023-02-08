// Copyright (c) Stride contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;

namespace VL.Stride.Assets
{
    public class RuntimeBuildUnit : AssetBuildUnit
    {
        private readonly AssetItem asset;
        private readonly AssetCompilerContext compilerContext;
        private readonly AssetDependenciesCompiler compiler;

        private static readonly Guid SceneBuildUnitContextId = Guid.NewGuid();

        public RuntimeBuildUnit(AssetItem asset, AssetCompilerContext compilerContext, AssetDependenciesCompiler assetDependenciesCompiler)
            : base(new AssetBuildUnitIdentifier(SceneBuildUnitContextId, asset.Id))
        {
            this.asset = asset;
            this.compilerContext = compilerContext;
            compiler = assetDependenciesCompiler;
        }

        protected override ListBuildStep Prepare()
        {
            var result = compiler.Prepare(compilerContext, asset);
            return result.BuildSteps;
        }
    }
}
