using Stride.Rendering.Materials;
using System;

namespace VL.Stride.Rendering.Materials
{
    public enum MicrofacetEnvironmentFunction
    {
        GGXLUT,
        GGXPolynomial,
        ThinGlass
    }

    public enum MicrofacetFresnelFunction
    {
        None,
        Schlick,
        ThinGlass
    }

    public enum MicrofacetNormalDistributionFunction
    {
        Beckmann,
        BlinnPhong,
        GGX
    }

    public enum MicrofacetVisibilityFunction
    {
        CookTorrance,
        Implicit,
        Kelemen,
        SmithBeckmann,
        SmithGGXCorrelated,
        SmithSchlickBeckmann,
        SmithSchlickGGX
    }

    public static class EnumExtensions
    {
        public static IMaterialSpecularMicrofacetEnvironmentFunction ToFunction(this MicrofacetEnvironmentFunction value)
        {
            switch (value)
            {
                case MicrofacetEnvironmentFunction.GGXLUT:
                    return new MaterialSpecularMicrofacetEnvironmentGGXLUT();
                case MicrofacetEnvironmentFunction.GGXPolynomial:
                    return new MaterialSpecularMicrofacetEnvironmentGGXPolynomial();
                case MicrofacetEnvironmentFunction.ThinGlass:
                    return new MaterialSpecularMicrofacetEnvironmentThinGlass();
                default:
                    throw new NotImplementedException();
            }
        }

        public static MicrofacetEnvironmentFunction ToEnum(this IMaterialSpecularMicrofacetEnvironmentFunction value)
        {
            if (value is MaterialSpecularMicrofacetEnvironmentGGXLUT)
                return MicrofacetEnvironmentFunction.GGXLUT;
            if (value is MaterialSpecularMicrofacetEnvironmentGGXPolynomial)
                return MicrofacetEnvironmentFunction.GGXPolynomial;
            if (value is MaterialSpecularMicrofacetEnvironmentThinGlass)
                return MicrofacetEnvironmentFunction.ThinGlass;
            throw new NotImplementedException();
        }

        public static IMaterialSpecularMicrofacetFresnelFunction ToFunction(this MicrofacetFresnelFunction value)
        {
            switch (value)
            {
                case MicrofacetFresnelFunction.None:
                    return new MaterialSpecularMicrofacetFresnelNone();
                case MicrofacetFresnelFunction.Schlick:
                    return new MaterialSpecularMicrofacetFresnelSchlick();
                case MicrofacetFresnelFunction.ThinGlass:
                    return new MaterialSpecularMicrofacetFresnelThinGlass();
                default:
                    throw new NotImplementedException();
            }
        }

        public static MicrofacetFresnelFunction ToEnum(this IMaterialSpecularMicrofacetFresnelFunction value)
        {
            if (value is MaterialSpecularMicrofacetFresnelNone)
                return MicrofacetFresnelFunction.None;
            if (value is MaterialSpecularMicrofacetFresnelSchlick)
                return MicrofacetFresnelFunction.Schlick;
            if (value is MaterialSpecularMicrofacetFresnelThinGlass)
                return MicrofacetFresnelFunction.ThinGlass;
            throw new NotImplementedException();
        }

        public static IMaterialSpecularMicrofacetNormalDistributionFunction ToFunction(this MicrofacetNormalDistributionFunction value)
        {
            switch (value)
            {
                case MicrofacetNormalDistributionFunction.Beckmann:
                    return new MaterialSpecularMicrofacetNormalDistributionBeckmann();
                case MicrofacetNormalDistributionFunction.BlinnPhong:
                    return new MaterialSpecularMicrofacetNormalDistributionBlinnPhong();
                case MicrofacetNormalDistributionFunction.GGX:
                    return new MaterialSpecularMicrofacetNormalDistributionGGX();
                default:
                    throw new NotImplementedException();
            }
        }

        public static MicrofacetNormalDistributionFunction ToEnum(this IMaterialSpecularMicrofacetNormalDistributionFunction value)
        {
            if (value is MaterialSpecularMicrofacetNormalDistributionBeckmann)
                return MicrofacetNormalDistributionFunction.Beckmann;
            if (value is MaterialSpecularMicrofacetNormalDistributionBlinnPhong)
                return MicrofacetNormalDistributionFunction.BlinnPhong;
            if (value is MaterialSpecularMicrofacetNormalDistributionGGX)
                return MicrofacetNormalDistributionFunction.GGX;
            throw new NotImplementedException();
        }

        public static IMaterialSpecularMicrofacetVisibilityFunction ToFunction(this MicrofacetVisibilityFunction value)
        {
            switch (value)
            {
                case MicrofacetVisibilityFunction.CookTorrance:
                    return new MaterialSpecularMicrofacetVisibilityCookTorrance();
                case MicrofacetVisibilityFunction.Implicit:
                    return new MaterialSpecularMicrofacetVisibilityImplicit();
                case MicrofacetVisibilityFunction.Kelemen:
                    return new MaterialSpecularMicrofacetVisibilityKelemen();
                case MicrofacetVisibilityFunction.SmithBeckmann:
                    return new MaterialSpecularMicrofacetVisibilitySmithBeckmann();
                case MicrofacetVisibilityFunction.SmithGGXCorrelated:
                    return new MaterialSpecularMicrofacetVisibilitySmithGGXCorrelated();
                case MicrofacetVisibilityFunction.SmithSchlickBeckmann:
                    return new MaterialSpecularMicrofacetVisibilitySmithSchlickBeckmann();
                case MicrofacetVisibilityFunction.SmithSchlickGGX:
                    return new MaterialSpecularMicrofacetVisibilitySmithSchlickGGX();
                default:
                    throw new NotImplementedException();
            }
        }

        public static MicrofacetVisibilityFunction ToEnum(this IMaterialSpecularMicrofacetVisibilityFunction value)
        {
            if (value is MaterialSpecularMicrofacetVisibilityCookTorrance)
                return MicrofacetVisibilityFunction.CookTorrance;
            if (value is MaterialSpecularMicrofacetVisibilityImplicit)
                return MicrofacetVisibilityFunction.Implicit;
            if (value is MaterialSpecularMicrofacetVisibilityKelemen)
                return MicrofacetVisibilityFunction.Kelemen;
            if (value is MaterialSpecularMicrofacetVisibilitySmithBeckmann)
                return MicrofacetVisibilityFunction.SmithBeckmann;
            if (value is MaterialSpecularMicrofacetVisibilitySmithGGXCorrelated)
                return MicrofacetVisibilityFunction.SmithGGXCorrelated;
            if (value is MaterialSpecularMicrofacetVisibilitySmithSchlickBeckmann)
                return MicrofacetVisibilityFunction.SmithSchlickBeckmann;
            if (value is MaterialSpecularMicrofacetVisibilitySmithSchlickGGX)
                return MicrofacetVisibilityFunction.SmithSchlickGGX;
            throw new NotImplementedException();
        }
    }
}
