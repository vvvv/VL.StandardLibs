using System.Collections.Generic;
using Stride.Core;
using Stride.Rendering.Materials;
using Stride.Shaders;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;

namespace VL.Stride.Shaders.ShaderFX
{
    public class GetVar<T> : VarBase<T>, IComputeValue<T>
        where T : unmanaged
    {
        public GetVar(DeclVar<T> declaration)
            : base(declaration)
        {
            Var = null;
            EvaluateChildren = false;
        }

        public GetVar(SetVar<T> var, bool evaluateChildren = true)
            : base(var.Declaration)
        {
            Var = var;
            EvaluateChildren = evaluateChildren;
        }

        public SetVar<T> Var { get; }

        bool EvaluateChildren { get; }

        public override IEnumerable<IComputeNode> GetChildren(object context = null)
        {
            return EvaluateChildren ? ReturnIfNotNull(Var) : ReturnIfNotNull();
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            ShaderClassSource shaderSource;

            if (Declaration is DeclConstant<T> constant)
                shaderSource = GetShaderSourceForType<T>("Constant", GetAsShaderString((dynamic)constant.ConstantValue));
            else if (Declaration is DeclSemantic<T> semantic)
                shaderSource = GetShaderSourceForType<T>("GetSemantic", semantic.GetNameForContext(context), semantic.SemanticName);
            else
                shaderSource = Declaration != null ? GetShaderSourceForType<T>("GetVar", Declaration.GetNameForContext(context)) : GetShaderSourceForType<T>("Compute");

            return shaderSource;
        }

        public override string ToString()
        {
            if (Declaration is DeclConstant<T> constant)
                return string.Format("Constant {0}", constant.VarName);
            else if (Declaration is DeclSemantic<T> semantic)
                return string.Format("Get Semantic {0}", semantic.SemanticName);
            else
                return string.Format("Get Var {0}", Declaration.VarName);
        }      
    }
}
