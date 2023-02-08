using System;
using System.Collections.Generic;
using System.Text;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;

namespace VL.Stride.Shaders.ShaderFX.Control
{
    public class IfThenRegionBuilder
    {
        public void Build()
        {
            var linksIntoRegion = new List<IComputeNode>();
            var linksToBorderCP = new List<IComputeNode>();
            var linksToInputPins = new List<IComputeNode>();

            //init accum1
            var accum1InitValue = ShaderFXUtils.Constant<float>(-1);

            var getAccum1InitValue = ShaderFXUtils.GetVarValue(accum1InitValue);
            var accum1 = DeclAndSetVar("Accum", getAccum1InitValue);

            //body patch begin ----

            var bodyConstant1 = ShaderFXUtils.Constant<float>(1);
            var bodyConstant2 = ShaderFXUtils.Constant<float>(2);

            //plus node
            var getPlusIn1 = ShaderFXUtils.GetVarValue(bodyConstant1);
            var getPlusIn2 = ShaderFXUtils.GetVarValue(bodyConstant2);
            var plusExpression = new BinaryOperation<float>("Plus", getPlusIn1, getPlusIn2);
            var plusResult = DeclAndSetVar("PlusResult", plusExpression);

            //re-assign accumulator 1
            var getPlusResult = ShaderFXUtils.GetVarValue(plusResult);
            var accum1ReAssign = accum1.SetVar(getPlusResult);

            //body patch end ----

            //build body
            var bodyStatements = ShaderGraph.BuildFinalShaderGraph(accum1ReAssign, excludes: linksIntoRegion);

            //actual if expression
            //condition
            var conditionValue = new GetConstant<bool>(true);

            var getConditionValue = conditionValue;
            var ifThenRegion = new IfThenRegion(bodyStatements, getConditionValue, GenerateShaderSource, GetChildren);

            //make sure accums are initialized before the region is called
            var finalStatements = new ComputeOrder(accum1, ifThenRegion);

            //outputs
            var regionBorderOutputs = new[] { accum1 };

        }

        public ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var shaderClassSource = new ShaderClassSource("IfThen");

            var mixin = shaderClassSource.CreateMixin();

            //mixin.AddComposition(then, "Then", context, baseKeys);
            //mixin.AddComposition(condtion, "Condition", context, baseKeys);

            return mixin;
        }

        public IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return GetInputLinks();
        }

        private IEnumerable<IComputeNode> GetInputLinks()
        {
            throw new NotImplementedException();
        }
    }

    public class IfThenRegion : ComputeVoid
    {
        public IfThenRegion(IComputeVoid then, 
            IComputeValue<bool> condition,
            Func<ShaderGeneratorContext, MaterialComputeColorKeys, ShaderSource> generateShader,
            Func<object, IEnumerable<IComputeNode>> generateChildren)
        {
            Then = then;
            Condtion = condition;
            GenerateShader = generateShader;
            GenerateChildren = generateChildren;
        }

        Func<ShaderGeneratorContext, MaterialComputeColorKeys, ShaderSource> GenerateShader { get; }
        Func<object, IEnumerable<IComputeNode>> GenerateChildren { get; }

        public IComputeVoid Then { get; }
        public IComputeValue<bool> Condtion { get; }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            //var shaderClassSource = new ShaderClassSource("IfThen");

            //var mixin = shaderClassSource.CreateMixin();

            //mixin.AddComposition(Then, "Then", context, baseKeys);
            //mixin.AddComposition(Condtion, "Condition", context, baseKeys);

            //return mixin;
            return GenerateShader(context, baseKeys);
        }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return GenerateChildren(context);
        }
    }
}
