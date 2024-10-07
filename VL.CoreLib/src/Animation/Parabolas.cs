//----------------------------------------------------------------------------//
//
//  Newton & DeNiro:
//  Time Based Filter algorithms based on accelereation and decceleration
//  DeNiro also can drive with constant speed
//
//  Sebastian Gregor
//  February 2001
//  gregor@vvvv.org
//
//  developed in delphi for vvvv
//
//  licensed under LGPL
//
//----------------------------------------------------------------------------//

using TMValue = double;
using TMTime = double;
using System;
using System.Collections.Generic;
using VL.Core;
using Microsoft.Extensions.Logging;



namespace xyz

{

    public static class Helpers
    {
        public const double EPSILON_DEFAULT = 1E-50;

        public static int STrunc(TMValue value)
        {
            int result = (int)Math.Truncate(value);
            if (value < 0)
            {
                result -= 1;
            }
            return result;
        }
        public static double Clip(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        internal static double Frac(double v) 
        {
            return v - Math.Floor(v);
        }

        internal static double SFrac(double value)
        {
            long trunced = (long)Math.Truncate(value);
            if (value < 0 && trunced != value)
            {
                trunced -= 1;
            }
            return value - trunced;
        }
    }



    public enum TMLinearFilterMode
    {
        VelocityBased,
        TimeBased
    }

    public enum TMUpdateFilterGraph
    {
        OnlyIfForced,
        OnNewGoToPosition,
        OnAnyConditionsChanged
    }

    public class TMFilterData
    {
        public TMValue P0, P1, V0;
        public TMTime T0;

        public TMFilterData()
        {
            P0 = 0;
            P1 = 0;
            V0 = 0;
            T0 = GClock.Time; // Assuming GClock is a valid class with a static property Time
        }
    }

    public class TMSimpleFilterState 
    {
        protected TMValue CurrentPosition, CurrentVelocity, CurrentAcceleration;
        protected TMTime CurrentTime, CurrentDeltaTime;
        protected TMValue FGoToPosition;
        protected bool FilterChanged;
        protected TMFilterData FilterData;
        protected bool FirstFrame;
        protected ILogger Logger;

        public TMSimpleFilterState(NodeContext nodeContext)
        {
            Logger = nodeContext.GetLogger();
        }

        protected virtual void SetGoToPosition(TMValue value)
        {
            FilterChanged = FilterChanged || (FGoToPosition != value);
            FGoToPosition = value;
        }

        public TMSimpleFilterState()
        {
            // FilterData has to be created and initialized by descendants
            FilterChanged = true;
            CurrentPosition = 0;
            CurrentVelocity = 0;
            CurrentAcceleration = 0;
            FirstFrame = true;
            CurrentTime = GClock.Time; // Assuming GClock is a valid class with a static property Time
            CurrentDeltaTime = 0;
        }

        ~TMSimpleFilterState()
        {
            FilterData = null; // Assuming FilterData is a managed resource
        }

        public TMValue GoToPosition
        {
            get { return FGoToPosition; }
            set { SetGoToPosition(value); }
        }
    }


    public class TMAdvFilterState : TMSimpleFilterState
    {
        protected bool Go;
        protected TMUpdateFilterGraph UpdateFilterGraph;
        protected bool Pause;
        protected TMTime Tpause;

        protected override void SetGoToPosition(TMValue value)
        {
            FilterChanged = FilterChanged ||
                ((FGoToPosition != value) && (UpdateFilterGraph == TMUpdateFilterGraph.OnNewGoToPosition || UpdateFilterGraph == TMUpdateFilterGraph.OnAnyConditionsChanged));
            FGoToPosition = value;
        }

        public TMAdvFilterState()
        {
            Go = false;
            Pause = false;
            Tpause = -1;
            UpdateFilterGraph = TMUpdateFilterGraph.OnNewGoToPosition;
        }
    }




    public enum TMParabolasPlayMode
    {
        npbActive,
        npbRecording,
        npbPlayBack
    }


    public class TMParabolaData : TMFilterData
    {
        public TMValue A0;

        public TMParabolaData(TMValue p0, TMValue v0, TMValue a0, TMTime t0)
        {
            P0 = p0;
            V0 = v0;
            A0 = a0;
            T0 = t0;
        }

        public void Seek(TMTime aTime, out TMValue Pos, out TMValue Vel, out TMValue Acc)
        {
            var dt = aTime - T0;
            Pos = P0 + V0 * dt + A0 * Math.Pow(dt, 2) / 2;
            Vel = V0 + A0 * dt;
            Acc = A0;
        }

        public void Assign(TMParabolaData otherParabola)
        {
            P0 = otherParabola.P0;
            V0 = otherParabola.V0;
            A0 = otherParabola.A0;
            T0 = otherParabola.T0;
        }
    }


    public class TMParabolasState : TMAdvFilterState
    {
        private List<TMParabolaData> FParabolas;
        private int FCurrentParabola;
        private TMParabolasPlayMode FPlayBack;
        protected TMParabolaData FCP;
        protected TMValue FAcceleration;

        public TMParabolasState()
        {
            FParabolas = new List<TMParabolaData>();
            FPlayBack = TMParabolasPlayMode.npbActive;
            FCurrentParabola = -1;
        }

        public int ParabolaCount => FParabolas.Count;

        public bool IsChanging => Math.Abs(CurrentVelocity) > 1E-8 || Math.Abs(FAcceleration) > 1E-10;

        public TMValue Acceleration
        {
            get => FAcceleration;
            set
            {
                FAcceleration = value;
                ChangeFuture();
            }
        }

        public TMValue ForcePosition
        {
            set
            {
                CurrentPosition = value;
                ChangeFuture();
            }
        }

        public TMValue ForceVelocity
        {
            set
            {
                CurrentVelocity = value;
                ChangeFuture();
            }
        }

        public TMParabolasPlayMode PlayBack
        {
            get => FPlayBack;
            set => FPlayBack = value;
        }

        public void AddParabola(TMParabolaData aParabola)
        {
            FParabolas.Add(aParabola);
        }

        public void AddParabola(TMValue p0, TMValue v0, TMValue a0, TMTime t0)
        {
            AddParabola(new TMParabolaData(p0, v0, a0, t0));
        }

        public void ClearParabolaList(bool onlyFutureItems = false)
        {
            if (onlyFutureItems)
            {
                for (int i = FParabolas.Count - 1; i >= 0; i--)
                {
                    if (FParabolas[i].T0 >= CurrentTime)
                    {
                        FParabolas.RemoveAt(i);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                FParabolas.Clear();
            }
        }

        public void ChangeFuture(bool buildParabolaWithCurrentValues = true)
        {
            ClearParabolaList(FPlayBack == TMParabolasPlayMode.npbRecording || FPlayBack == TMParabolasPlayMode.npbPlayBack);
            FCurrentParabola = ParabolaCount - 1;
            if (buildParabolaWithCurrentValues)
            {
                AddParabola(CurrentPosition, CurrentVelocity, FAcceleration, CurrentTime);
            }
        }

        public void Fly()
        {
            FAcceleration = 0;
            ChangeFuture();
        }

        public void Stop()
        {
            FAcceleration = 0;
            CurrentVelocity = 0;
            ChangeFuture();
        }

        public void Force(TMValue P, TMValue V, TMValue A)
        {
            ChangeFuture(false);
            AddParabola(P, V, A, CurrentTime);
        }

        public void Seek(TMTime aTime)
        {
            if (ParabolaCount < 1)
            {
                FCurrentParabola = -1;
            }
            else
            {
                int old = FCurrentParabola;
                FCurrentParabola = Math.Clamp(FCurrentParabola, 0, ParabolaCount - 1);
                while (true)
                {
                    if (FParabolas[FCurrentParabola].T0 <= aTime)
                    {
                        if (FCurrentParabola >= ParabolaCount - 1 || FParabolas[FCurrentParabola + 1].T0 > aTime)
                        {
                            break;
                        }
                        else
                        {
                            FCurrentParabola++;
                        }
                    }
                    else
                    {
                        if (FCurrentParabola > 0)
                        {
                            FCurrentParabola--;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                if (old != FCurrentParabola)
                {
                    FCP = FParabolas[FCurrentParabola];
                }
            }
        }

        public void Seek(TMTime aTime, out TMValue Pos, out TMValue Vel, out TMValue Acc)
        {
            Seek(aTime);
            FCP.Seek(aTime, out Pos, out Vel, out Acc);
        }

        public void Tick()
        {
            Seek(CurrentTime, out CurrentPosition, out CurrentVelocity, out FAcceleration);
        }

        public TMParabolaData GetContinuousParabola(TMTime aTime)
        {
            Seek(aTime, out var Pos, out var Vel, out var Acc);
            return new TMParabolaData(Pos, Vel, Acc, aTime);
        }

        public TMParabolaData GetFlyParabola(TMTime aTime)
        {
            var result = GetContinuousParabola(aTime);
            result.A0 = 0;
            return result;
        }

        public TMParabolaData GetStopParabola(TMTime aTime)
        {
            var result = GetFlyParabola(aTime);
            result.V0 = 0;
            return result;
        }

        public TMParabolaData ContinuousAfterFilterTime(TMTime filterTime)
        {
            var result = GetContinuousParabola(CurrentTime + filterTime);
            AddParabola(result);
            return result;
        }

        public TMParabolaData FlyAfterFilterTime(TMTime filterTime)
        {
            var result = GetFlyParabola(CurrentTime + filterTime);
            AddParabola(result);
            return result;
        }
        public TMParabolaData StopAfterFilterTime(TMTime filterTime)
        {
            var result = GetStopParabola(CurrentTime + filterTime);
            AddParabola(result);
            return result;
        }

        public void GotoVelocityUserFilterTime(TMValue startv, TMValue endv, TMTime dt)
        {
            CurrentVelocity = startv;
            try
            {
                if (dt > 1E-20)
                {
                    Acceleration = (endv - startv) / dt;
                    FlyAfterFilterTime(dt);
                }
                else
                {
                    FAcceleration = 0;
                    ForceVelocity = endv;
                }
            }
            catch (Exception)
            {
                Logger.LogWarning("parabolas: couldn't go from velocity {0} to velocity {1} in {2} seconds", startv, endv, dt);
            }
        }

        public void GotoVelocityUseAcceleration(TMValue startv, TMValue endv, TMValue absa)
        {
            TMValue dt;
            CurrentVelocity = startv;
            absa = Math.Abs(absa);
            try
            {
                if (absa > 1E-20)
                {
                    dt = (endv - startv) / absa;
                    if (dt < 0) absa = -absa;
                    Acceleration = absa;
                    FlyAfterFilterTime(Math.Abs(dt));
                }
                else
                {
                    Fly();
                    Logger.LogWarning("parabolas: couldn't go from velocity {0} to velocity {1} without acceleration (+-{2})", startv, endv, absa);
                }
            }
            catch (Exception)
            {
                Logger.LogWarning("parabolas: couldn't go from velocity {0} to velocity {1} with acceleration +-{2}", startv, endv, absa);
            }
        }

        public void GotoPosKeepVelocity(TMValue startp, TMValue startv, TMValue endp, TMTime dt)
        {
            CurrentPosition = startp;
            try
            {
                if (dt > 1E-20)
                {
                    GotoVelocityUserFilterTime(startv, 2 * (endp - startp) / dt - startv, dt);
                }
                else
                {
                    Force(endp, startv, 0);
                }
            }
            catch (Exception)
            {
                Logger.LogWarning("parabolas: couldn't go to position {0} keeping velocity {1} in {2} seconds", endp, CurrentVelocity, dt);
            }
        }

        public void GotoPosSetEndVelocity(TMValue startp, TMValue startv, TMValue endp, TMValue endv, TMTime dt)
        {
            CurrentPosition = startp;
            try
            {
                if (dt > 1E-20)
                {
                    GotoVelocityUserFilterTime(2 * (endp - startp) / dt - endv, endv, dt);
                }
                else
                {
                    Force(endp, endv, 0);
                }
            }
            catch (Exception)
            {
                Logger.LogWarning("parabolas: couldn't go to position {0}, velocity {1} in {2} seconds", endp, endv, dt);
            }
        }

        public void ApplyPause()
        {
            for (int i = 0; i < FParabolas.Count; i++)
            {
                FParabolas[i].T0 = CurrentTime - Tpause + FParabolas[i].T0;
            }
        }

    }


    public enum TMDeNiroInputSelect
    {
        nisInput,
        nisVelocity,
        nisAcceleration
    }

    public enum TMDeNiroMode
    {
        dnmTimeBased,
        dnmAccelerationBased
    }



    public class TMDeNiroState : TMParabolasState
    {
        private bool FForceBang;
        private TMValue FConstantDrive;
        private TMValue FMaxVelocity;
        private TMTime FFilterTime;
        private TMDeNiroMode FMode;
        private TMValue FGoToVelocity;
        private TMValue FAccelerationIn;
        private bool FCyclic;
        private TMDeNiroInputSelect FInputSelect;

        public TMTime FilterTime
        {
            get { return FFilterTime; }
            set { FFilterTime = value; }
        }

        public TMValue MaxVelocity
        {
            get { return FMaxVelocity; }
            set { SetMaxVelocity(value); }
        }

        public TMDeNiroMode Mode
        {
            get { return FMode; }
            set { FMode = value; }
        }

        public TMDeNiroInputSelect InputSelect
        {
            get { return FInputSelect; }
            set { FInputSelect = value; }
        }

        public TMValue ConstantDrive
        {
            get { return FConstantDrive; }
            set { SetConstantDrive(value); }
        }

        public TMValue GoToVelocity
        {
            get { return FGoToVelocity; }
            set { SetGoToVelocity(value); }
        }

        public TMValue AccelerationIn
        {
            get { return FAccelerationIn; }
            set { SetAccelerationIn(value); }
        }

        public bool Cyclic
        {
            get { return FCyclic; }
            set { FCyclic = value; }
        }

        public bool ForceBang
        {
            get { return FForceBang; }
            set { FForceBang = value; }
        }


        public TMDeNiroState()
        {
            FMode = TMDeNiroMode.dnmAccelerationBased;
            FInputSelect = TMDeNiroInputSelect.nisInput;
            FCyclic = false;
            FFilterTime = 1;
            FConstantDrive = 0;
            FMaxVelocity = double.MaxValue;
            FilterChanged = true;
            FForceBang = false;
        }

        public void DriveToGoToPosition()
        {
            switch (FMode)
            {
                case TMDeNiroMode.dnmTimeBased:
                    GotoPosThreeSteps(CurrentPosition, CurrentVelocity, FGoToPosition, FGoToVelocity, FConstantDrive, FFilterTime);
                    break;
                case TMDeNiroMode.dnmAccelerationBased:
                    GotoPosThreeStepsUseAcceleration(CurrentPosition, CurrentVelocity, FGoToPosition, FGoToVelocity, FConstantDrive, FAccelerationIn);
                    break;
            }
        }

        public void DriveToGoToVelocity()
        {
            switch (FMode)
            {
                case TMDeNiroMode.dnmTimeBased:
                    GotoVelocityUserFilterTime(CurrentVelocity, FGoToVelocity, FFilterTime);
                    break;
                case TMDeNiroMode.dnmAccelerationBased:
                    GotoVelocityUseAcceleration(CurrentVelocity, FGoToVelocity, FAccelerationIn);
                    break;
            }
        }

        public void GotoPosThreeSteps(TMValue startp, TMValue startv, TMValue endp, TMValue endv, TMValue beta, TMTime dt)
        {
            // TODO -cMM: default body inserted
        }


        struct tmsolution
        {
            public TMValue a0, a2, v1;
            public TMTime t0, t1, dt;

            public tmsolution(TMValue a0_, TMValue a2_, TMValue v1_, TMTime t0_, TMTime t1_, TMTime dt_)
            {
                a0 = a0_;
                a2 = a2_;
                v1 = v1_;
                t0 = t0_;
                t1 = t1_;
                dt = dt_;
            }
        }

        void GotoPosThreeStepsUseAcceleration(TMValue startp, TMValue startv, TMValue endp,
            TMValue endv, TMValue beta, TMValue absa)
        {
            TMTime dtv, dtmin;
            TMValue vadd, dv, dp, gamma, gamma2;
            tmsolution solution;
            bool solutionfound;

            void Core(TMValue a0)
            {
                void Core2(TMValue a0, TMTime dt)
                {
                    TMValue v1old, t0old, t0add;
                    TMValue a2, v1;
                    TMTime t0, t1, t2;
                    bool bettersolution;

                    t0 = gamma * dt / 2 + dv / (2 * a0);
                    v1 = startv + a0 * t0;
                    t1 = beta * dt;
                    a2 = -a0;
                    bettersolution = (dt >= t0 + t1 - 1E-6) && (t0 >= -1E-6) && (t1 >= -1E-6) && (!solutionfound || (dt < solution.dt));

                    if (bettersolution)
                    {
                        t0 = Math.Max(0, t0);
                        t1 = Math.Max(0, t1);
                        dt = Math.Max(t0 + t1, dt);

                        if (Math.Abs(v1) > FMaxVelocity)
                        {
                            if ((Math.Abs(startv) > FMaxVelocity) && (Math.Abs(v1) > Math.Abs(startv)) && ((startv > 0) == (v1 > 0)))
                            {
                                a0 = a2;
                                t2 = dt - t0 - t1;
                                t0old = t0;
                                v1old = v1;
                                v1 = Math.Sign(v1) * FMaxVelocity;
                                t0 = (v1 - startv) / a0;
                                t0add = t0old + t0;
                                t1 = t1 + 2 * t0old + ((t1 + t0add) * (Math.Abs(v1old) - FMaxVelocity) - t0 * (Math.Abs(startv) - FMaxVelocity)) / FMaxVelocity;
                                t2 = t2 - t0add;
                                dt = t0 + t1 + t2;
                            }
                            else
                            {
                                t2 = dt - t0 - t1;
                                t0old = t0;
                                v1old = v1;
                                v1 = Math.Sign(v1) * FMaxVelocity;
                                t0 = (v1 - startv) / a0;
                                t0add = t0old - t0;
                                t1 = t1 + 2 * t0add + (t1 + t0add) * (Math.Abs(v1old) - FMaxVelocity) / FMaxVelocity;
                                t2 = t2 - t0add;
                                dt = t0 + t1 + t2;
                            }

                            // check if still a better solution (dt has become bigger)
                            if (t0 < -1E-5 || t1 < -1E-5 || t2 < -1E-5)
                                Logger.LogWarning("De Niro: internal error on clipping vmax; solution becomes invalid." + Environment.NewLine +
                                    "p0 %g, p3 %g, v0 %g, v1 %g, v3 %g, a0 %g, a2 %g, ß %g, vmax %g" + Environment.NewLine + "calculated times: t0 %g, t1 %g, t2 %g, dt %g, dtmin %g",
                                    new object[] { startp, endp, startv, v1, endv, a0, a2, beta, FMaxVelocity, t0, t1, t2, dt, dtmin });
                            bettersolution = !solutionfound || (dt < solution.dt);
                        }

                        // now we definitely know if we have a better solution!
                        if (bettersolution)
                        {
                            solutionfound = true;
                            solution = new tmsolution(a0, a2, v1, t0, t1, dt); // just use functionality of constructor
                        }
                    }
                }
                TMValue xmid, x12, D;


                {
                    D = Math.Pow(vadd, 2) + gamma2 * (4 * a0 * dp + Math.Pow(dv, 2));
                    if (D < 0) return;
                    D = Math.Sqrt(D);
                    x12 = gamma2 * a0;
                    xmid = -vadd / x12;
                    x12 = D / x12;
                    Core2(a0, xmid + x12);
                    Core2(a0, xmid - x12);
                }
            }


            int[] cp = new int[4];
            int cpcount = 0;

            void AddCyclicPoint(int acp)
            {
                for (int i = 0; i < cpcount; i++)
                    if (cp[i] == acp) return;
                cp[cpcount] = acp;
                cpcount++;
            }

            void DoMiddle(TMValue a, TMTime t0, TMTime t1, TMTime dt)
            {
                TMValue v1old, t0old, v1;
                TMTime t2;

                v1 = startv + a * t0;
                if (Math.Abs(v1) > FMaxVelocity)
                {
                    t2 = dt - t0 - t1;
                    t0old = t0;
                    v1old = v1;
                    v1 = Math.Sign(v1) * FMaxVelocity;
                    t0 = (v1 - startv) / a;
                    t1 = t1 + t1 * (Math.Abs(v1old) - FMaxVelocity) / FMaxVelocity;
                    t2 = t2 - t0 + t0old;
                    dt = t0 + t1 + t2;
                }

                solutionfound = true;
                solution = new tmsolution(a, a, v1, t0, t1, dt); // just use functionality of constructor
            }

            TMValue middle, dpc, x, a, dp1, dp2;
            int i;

            CurrentPosition = startp;
            CurrentVelocity = startv;
            absa = Math.Abs(absa);

            try
            {
                if (absa > 1E-20 && beta < 1 - 1E-6)
                {
                    gamma = 1 - beta;
                    gamma2 = 1 - Math.Pow(beta, 2);
                    dv = endv - startv;
                    vadd = startv + endv;

                    a = ((dv >= 0 ? 1 : -1) * 2 - 1) * absa;
                    dtv = dv / a;
                    dtmin = dtv / gamma;

                    x = a * Math.Pow(dtv, 2) / 2;
                    dp1 = endv * dtmin - x;
                    dp2 = startv * dtmin + x;

                    dp = endp - startp;
                    if (!FCyclic)
                    {
                        middle = (dp - dp1) / (dp2 - dp1);
                    }
                    else
                    {
                        dp = Helpers.SFrac(dp);
                        dpc = Helpers.STrunc(dp1 - dp + (dp2 >= dp1 ? 1 : 0)) + dp;
                        middle = (dpc - dp1) / (dp2 - dp1);
                        if (middle >= 0 && middle <= 1) dp = dpc;
                    }

                    solution = new tmsolution(0, 0, 0, double.MaxValue, double.MaxValue, double.MaxValue);
                    solutionfound = false;
                    try
                    {
                        if (middle >= 0 && middle <= 1)
                        {
                            DoMiddle(a, (1 - middle) * dtv, beta * dtmin, dtmin);
                        }
                        else
                        {
                            if (!FCyclic)
                            {
                                Core(-a);
                                Core(a);
                            }
                            else
                            {
                                dpc = Helpers.SFrac(dp);
                                cpcount = 0;
                                AddCyclicPoint(Helpers.STrunc(dp1 - dp));
                                AddCyclicPoint(Helpers.STrunc(dp1 - dp) + 1);
                                AddCyclicPoint(Helpers.STrunc(dp2 - dp));
                                AddCyclicPoint(Helpers.STrunc(dp2 - dp) + 1);

                                for (i = 0; i < cpcount; i++)
                                {
                                    dp = cp[i] + dpc;
                                    Core(-a);
                                    Core(a);
                                }
                            }
                        }

                        if (solutionfound)
                        {
                            solution.t0 = Math.Max(0, solution.t0);
                            solution.t1 = Math.Max(0, solution.t1);
                            solution.dt = Math.Max(solution.t0 + solution.t1, solution.dt);
                            Acceleration = solution.a0;
                            FlyAfterFilterTime(solution.t0).V0 = solution.v1;
                            ContinuousAfterFilterTime(solution.t0 + solution.t1).A0 = solution.a2;
                            FlyAfterFilterTime(solution.dt).P0 = endp;
                            FlyAfterFilterTime(solution.dt).V0 = endv;
                        }
                        else
                        {
                            Fly();
                            Logger.LogWarning("De Niro: no solution found for: " + Environment.NewLine +
                            "p0 {0}, p3 {1}, v0 {2}, v3 {3}, abs(a) {4}, ß {5}, vmax {6}", startp, endp, startv, endv, absa, beta, FMaxVelocity);
                        }
                    }
                    finally
                    {
                        //solution.Dispose();
                    }
                }
                else
                {
                    Fly();
                }
            }
            catch
            {
                Logger.LogWarning("De Niro: couldn't go to position {0}, velocity {1} with acceleration +-{2}", endp, endv, absa);
            }
        }


        public void SetConstantDrive(TMValue value)
        {
            value = Helpers.Clip(value, 0, 1);
            if (FConstantDrive != value)
            {
                FConstantDrive = value;
                FilterChanged = FilterChanged || (UpdateFilterGraph == TMUpdateFilterGraph.OnAnyConditionsChanged);
            }
        }

        public void Tick()
        {
            // inherited Tick; // Assuming this is a call to a base class method

            if (FInputSelect == TMDeNiroInputSelect.nisAcceleration && Math.Abs(CurrentVelocity) > FMaxVelocity)
            {
                CurrentVelocity = Helpers.Clip(CurrentVelocity, -FMaxVelocity, FMaxVelocity);
                Fly();
            }

            // if (!FCyclic && FClip && !Between(CurrentPosition, 0, 1))
            // {
            //     CurrentPosition = Clip(CurrentPosition, 0, 1);
            //     Stop();
            // }
            CurrentAcceleration = FAcceleration;
        }

        public void SetMaxVelocity(TMValue value)
        {
            value = Math.Abs(value);
            value = Math.Max(value, 1E-6);
            if (FMaxVelocity != value)
            {
                FMaxVelocity = value;
                FGoToVelocity = FGoToVelocity;
                FilterChanged = FilterChanged || (UpdateFilterGraph == TMUpdateFilterGraph.OnAnyConditionsChanged);
            }
        }

        public void SetGoToPosition(TMValue value)
        {
            FilterChanged = FilterChanged ||
                (((Math.Abs(FGoToPosition - value) > Helpers.EPSILON_DEFAULT) && FCyclic) || ((FGoToPosition != value) && !FCyclic)) &&
                (FInputSelect == TMDeNiroInputSelect.nisInput) && (UpdateFilterGraph == TMUpdateFilterGraph.OnNewGoToPosition || UpdateFilterGraph == TMUpdateFilterGraph.OnAnyConditionsChanged);
            FGoToPosition = value;
        }

        public void SetGoToVelocity(TMValue value)
        {
            value = Helpers.Clip(value, -FMaxVelocity, FMaxVelocity);
            FilterChanged = FilterChanged ||
                ((FGoToVelocity != value) &&
                 ((FInputSelect == TMDeNiroInputSelect.nisInput) && (UpdateFilterGraph == TMUpdateFilterGraph.OnAnyConditionsChanged)) ||
                 ((FInputSelect == TMDeNiroInputSelect.nisVelocity) && (UpdateFilterGraph == TMUpdateFilterGraph.OnNewGoToPosition || UpdateFilterGraph == TMUpdateFilterGraph.OnAnyConditionsChanged)));
            FGoToVelocity = value;
        }

        public void SetAccelerationIn(TMValue value)
        {
            FilterChanged = FilterChanged ||
                ((FAccelerationIn != value) &&
                 ((FInputSelect == TMDeNiroInputSelect.nisInput || FInputSelect == TMDeNiroInputSelect.nisVelocity) && (UpdateFilterGraph == TMUpdateFilterGraph.OnAnyConditionsChanged)) ||
                 ((FInputSelect == TMDeNiroInputSelect.nisAcceleration) && (UpdateFilterGraph == TMUpdateFilterGraph.OnNewGoToPosition || UpdateFilterGraph == TMUpdateFilterGraph.OnAnyConditionsChanged)));
            FAccelerationIn = value;
        }

        public void UpdateFilter()
        {
            if (FirstFrame || FForceBang)
            {
                FilterChanged = true;
                switch (FInputSelect)
                {
                    case TMDeNiroInputSelect.nisInput:
                    case TMDeNiroInputSelect.nisVelocity:
                        Force(FGoToPosition, FGoToVelocity, 0);
                        break;
                    case TMDeNiroInputSelect.nisAcceleration:
                        Force(FGoToPosition, FGoToVelocity, FAccelerationIn);
                        break;
                }
            }
            else
            {
                if (FilterChanged)
                {
                    switch (FInputSelect)
                    {
                        case TMDeNiroInputSelect.nisInput:
                            DriveToGoToPosition();
                            break;
                        case TMDeNiroInputSelect.nisVelocity:
                            DriveToGoToVelocity();
                            break;
                        case TMDeNiroInputSelect.nisAcceleration:
                            CurrentAcceleration = FAccelerationIn;
                            break;
                    }
                }
            }

        }
    }
}


