using System;
using VL.Core;
using MathLib = System.Math;

namespace VL.Lib.Animation
{
    delegate void Sampler<TDomain, TCodomain>(TDomain input, out TCodomain output);
    delegate void Sampler2<TDomain, TCodomain>(TDomain input, out TCodomain output, out TCodomain derivative);
    delegate void Sampler3<TDomain, TCodomain>(TDomain input, out TCodomain output, out TCodomain derivative, out TCodomain secondDerivative);

    public interface IFunctionSampler<TDomain, TCodomain>
    {
        void Sample(TDomain input, out TCodomain output);
    }
    public interface IFunctionDerivativeSampler<TDomain, TCodomain>
    {
        void SampleDerivative(TDomain input, out TCodomain derivative);
    }
    public interface IFunctionSecondDerivativeSampler<TDomain, TCodomain>
    {
        void SampleSecondDerivative(TDomain input, out TCodomain secondDerivative);
    }

    public interface IFunctionAndDerivativeSampler<TDomain, TCodomain>
        : IFunctionSampler<TDomain, TCodomain>
    {
        void Sample(TDomain input, out TCodomain output, out TCodomain derivative);
    }

    public interface IFunctionAndTwoDerivativesSampler<TDomain, TCodomain>
        : IFunctionAndDerivativeSampler<TDomain, TCodomain>
    {
        void Sample(TDomain input, out TCodomain output, out TCodomain derivative, out TCodomain secondDerivative);
    }

    public interface IFunctionTwoDerivativesSampler<TDomain, TCodomain> :
        IFunctionDerivativeSampler<TDomain, TCodomain>
    {
        void SampleDerivative(TDomain input, out TCodomain derivative, out TCodomain secondDerivative);
    }


    public class FunctionWithAddedSeparateDerivativeSampler<TDomain, TCodomain> : 
        IFunctionAndDerivativeSampler<TDomain, TCodomain>, IFunctionDerivativeSampler<TDomain, TCodomain>
    {
        public TDomain Domain { get; set; }

        Sampler<TDomain, TCodomain> FSampler;
        Sampler<TDomain, TCodomain> FDerivativeSampler;
        Sampler2<TDomain, TCodomain> FFunctionAndDerivativeSampler;

        public FunctionWithAddedSeparateDerivativeSampler(IFunctionAndDerivativeSampler<TDomain, TCodomain> sampler)
        {
            FSampler = sampler.Sample;
            FFunctionAndDerivativeSampler = sampler.Sample;
            FDerivativeSampler = SampleDerivativeOnly;
        }

        void SampleDerivativeOnly(TDomain input, out TCodomain derivative)
        {
            FFunctionAndDerivativeSampler(input, out _, out derivative);
        }     

        public void Sample(TDomain input, out TCodomain output)
        {
            FSampler(input, out output);
        }

        public void Sample(TDomain input, out TCodomain output, out TCodomain derivative)
        {
            FFunctionAndDerivativeSampler(input, out output, out derivative);
        }

        public void SampleDerivative(TDomain input, out TCodomain derivative)
        {
            FDerivativeSampler(input, out derivative);
        }
    }


    public class FunctionWithAddedDerivative<TDomain, TCodomain> : 
        IFunctionAndDerivativeSampler<TDomain, TCodomain>, IFunctionDerivativeSampler<TDomain, TCodomain>
    {
        public TDomain Domain { get; set; }

        Sampler<TDomain, TCodomain> FSampler;
        Sampler<TDomain, TCodomain> FDerivativeSampler;
        Sampler2<TDomain, TCodomain> FFunctionAndDerivativeSampler;

        public FunctionWithAddedDerivative(IFunctionSampler<TDomain, TCodomain> sampler, Func<TDomain, TCodomain> derivativeFunc = null)
        {
            void SampleDerivativeFunc(TDomain input, out TCodomain derivative)
            {
                derivative = derivativeFunc(input);
            }

            FSampler = sampler.Sample;

            if (derivativeFunc != null)
            {
                FDerivativeSampler = SampleDerivativeFunc;
                FFunctionAndDerivativeSampler = SampleSeparately;
            }
            else
            if (sampler is IFunctionDerivativeSampler<TDomain, TCodomain> sd)
            {
                FDerivativeSampler = sd.SampleDerivative;
                FFunctionAndDerivativeSampler = SampleSeparately;
            }
            else
                throw new Exception("provide a derivative function or a sampler that also implements IFunctionDerivativeSampler");
        }

        void SampleSeparately(TDomain input, out TCodomain output, out TCodomain derivative)
        {
            FSampler(input, out output);
            FDerivativeSampler(input, out derivative);
        }

        public void Sample(TDomain input, out TCodomain output)
        {
            FSampler(input, out output);
        }

        public void Sample(TDomain input, out TCodomain output, out TCodomain derivative)
        {
            FFunctionAndDerivativeSampler(input, out output, out derivative);
        }

        public void SampleDerivative(TDomain input, out TCodomain derivative)
        {
            FDerivativeSampler(input, out derivative);
        }
    }




    public enum ADSRStage
    {
        Off = 0,
        Attack,
        Decay,
        Sustain,
        Release
    }

    public static class AnimationUtils
    {
        public static T SwitchADSRStage<T>(ADSRStage stage, Func<T> off, Func<T> attack, Func<T> decay, Func<T> sustain, Func<T> release)
        {
            switch (stage)
            {
                case ADSRStage.Off:
                    return off();
                case ADSRStage.Attack:
                    return attack();
                case ADSRStage.Decay:
                    return decay();
                case ADSRStage.Sustain:
                    return sustain();
                case ADSRStage.Release:
                    return release();
            }

            return default(T);
        }
    }

    //public interface ICurve<TOutRoom, TSampleRoom>
    //{
    //    TOutRoom Sample(TSampleRoom samplePos);
    //}

    //public class Linear : ICurve<float, float>
    //{
    //    protected float Ft0, Ft1;
    //    protected float Fp0, Fp1;

    //    public Linear(float t0, float p0, float t1, float p1)
    //    {
    //        Ft0 = t0;
    //        Fp0 = p0;
    //        Ft1 = t1;
    //        Fp1 = p1;
    //    }

    //    public Linear(float startingpos = 0, float goal = 1, float filterTime = 1, Clock clock = null)
    //    {
    //        clock = clock ?? FrameClock.GlobalClock;
    //        Ft0 = (float)clock.Now;
    //        Ft1 = (float)clock.FromNow(filterTime);
    //        Fp0 = startingpos;
    //        Fp1 = goal;            
    //    }

    //    public float Sample(float samplePos)
    //    {
    //        var x = (samplePos - Ft0) / (Ft1 - Ft0);
    //        x = System.Math.Min(System.Math.Max(x, 0), 1);
    //        var y = Fp0 + x * (Fp1 - Fp0);
    //        return y;
    //    }
    //}

    public class Oscillator
    {
        private const int OT_HYP = 1;
        private const int OT_OSC = 2;
        private const int OT_DMP = 3;

        private readonly bool FIsDummy;
        private bool FFirstFrame = true;
        private readonly double FStartVelocity;
        private readonly double FFilterTime;
        private readonly double FStartTime;
        private readonly double FCycles;
        private readonly bool FCyclic;
        private readonly double FDamping;
        private readonly double FGoal;
        private readonly int FOscType;
        private readonly double FDiffPos, FDiffV, FOptMinus, FOptPlus, FOptP, FOptV, FOptV2, FPRoot;

        private readonly IClock FClock;

        public Oscillator WithClock(IClock clock) => new Oscillator(FGoal, clock, FFilterTime, FCycles);

        public Oscillator(NodeContext nodeContext) 
            : this(0d, Clocks.FrameClock)
        {
            FIsDummy = true;
        }

        public Oscillator(double goal, IClock clock, double filterTime = 1d, double cycles = 0d, bool cyclic = false)
            : this(goal, 0d, goal, clock, filterTime, cycles, cyclic) // start from goal
        {
        }

        private Oscillator(double startPosition, double startVelocity, double goal, IClock clock, double filterTime = 1d, double cycles = 0d, bool cyclic = false)
        {
            FClock = clock;
            var currentTime = clock.Time.Seconds;

            FStartTime = currentTime;
            FStartVelocity = startVelocity;
            FFilterTime = filterTime;
            FCycles = cycles;
            FCyclic = cyclic;

            cycles = cycles * 2 * MathLib.PI;
            FDamping = 12 * (1 / MathLib.Max(1E-20, filterTime));

            var energy = FDamping * FDamping + cycles * cycles * MathLib.Sign(cycles);
            if (cyclic)
            {
                //  no accelaration necessary
                var optimalEndPosition = startPosition + 2 * FDamping * startVelocity / energy;
                FGoal = goal + Math.Round(optimalEndPosition - goal);
            }
            else
                FGoal = goal;

            FDiffPos = startPosition - FGoal;
            FDiffV = FDamping * FDiffPos + FStartVelocity;
            var uRoot = (FDamping * FDamping) - energy;

            if (Math.Abs(uRoot) < 1E-20)
            {
                //third case
                FOscType = OT_DMP;
                FOptV2 = FDamping * FDiffV;
            }
            else
            {
                if (uRoot > 0)
                {
                    //first case
                    FOscType = OT_HYP;
                    FPRoot = Math.Sqrt(uRoot);
                    FOptMinus = -FDamping - FPRoot;
                    FOptPlus = -FDamping + FPRoot;
                    FOptV = (energy * FDiffPos + FDamping * FStartVelocity) / FPRoot;
                }
                else
                {
                    //second case
                    FOscType = OT_OSC;
                    FPRoot = Math.Sqrt(-uRoot);
                    FOptV = (energy * FDiffPos + FDamping * FStartVelocity) / FPRoot;
                }
                FOptP = FDiffV / FPRoot;
            }
        }

        public void Sample(out double position, out double velocity)
        {
            var currentTime = FClock.Time.Seconds;
            var deltaTime = MathLib.Max(0, currentTime - FStartTime);

            double eMinus, ePlus, eMinusPlus, eMinusMinus, alpha, cosA, sinA, expDamp;

            if (FOscType == OT_HYP)
            {
                eMinus = MathLib.Exp(FOptMinus * deltaTime);
                ePlus = MathLib.Exp(FOptPlus * deltaTime);
                eMinusPlus = eMinus + ePlus;
                eMinusMinus = eMinus - ePlus;
                position = (FGoal + 0.5 * (eMinusPlus * FDiffPos - FOptP * eMinusMinus));
                velocity = (0.5 * (eMinusPlus * FStartVelocity + FOptV * eMinusMinus));
            }
            else if (FOscType == OT_OSC)
            {
                expDamp = MathLib.Exp(-FDamping * deltaTime);
                alpha = FPRoot * deltaTime;
                cosA = MathLib.Cos(alpha);
                sinA = MathLib.Sin(alpha);
                position = (FGoal + expDamp * (FDiffPos * cosA + FOptP * sinA));
                velocity = (expDamp * (FStartVelocity * cosA - FOptV * sinA));
            }
            else if (FOscType == OT_DMP)
            {
                expDamp = MathLib.Exp(-FDamping * deltaTime);
                position = (FGoal + expDamp * (FDiffPos + FDiffV * deltaTime));
                velocity = (expDamp * (FStartVelocity - FOptV2 * deltaTime));
            }
            else
                throw new NotImplementedException();
        }

        public Oscillator AttachNewFilter(double goal, out double position, out double velocity, double filterTime = 1d, double cycles = 0d, bool cyclic = false)
        {
            if (FIsDummy)
            {
                position = goal;
                velocity = 0;
            }
            else
                Sample(out position, out velocity);

            return new Oscillator(position, velocity, goal, FClock, filterTime, cycles, cyclic);
        }

        public Oscillator Update(float goal, out float position, out float velocity, float filterTime = 1f, float cycles = 0f, bool cyclic = false)
        {
            double p;
            double v;
            if (goal != FGoal || filterTime != FFilterTime || cycles != FCycles || FFirstFrame || FCyclic != cyclic) 
            {
                FFirstFrame = false;
                var o = AttachNewFilter(goal, out p, out v, filterTime, cycles, cyclic);
                position = (float)p;
                velocity = (float)v;
                return o;
            }
            else
            {
                Sample(out p, out v);
                position = (float)p;
                velocity = (float)v;
                return this;
            }
        }
    }
}
