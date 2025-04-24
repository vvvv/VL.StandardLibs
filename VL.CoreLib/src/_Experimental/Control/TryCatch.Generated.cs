
// Do not modify, this is generated code!

using System;
using System.Diagnostics;
using VL.Core;
using VL.Core.Import;

namespace VL.Lib.Control
{


    /// ---------------------------------------------------------------------------------------------------------
    /// TRY 2
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch and returns whether it has been successful or not. Supports 2 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryStateful2<TState> : TryStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TOutput1, TOutput2>> @try,
            TOutput1 defaultOutput1,
            out TOutput1 output1,
            TOutput2 defaultOutput2,
            out TOutput2 output2,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out bool success,
            out string errorMessage)
        {
            Func<TState, Tuple<TState, Tuple<TOutput1, TOutput2>>> packedTry = (state) =>
            {
                var unpackedTry = @try(state);
                return Tuple.Create(unpackedTry.Item1, Tuple.Create(
                unpackedTry.Item2,
                unpackedTry.Item3
                ));
            };
            var output = base.Update(create, packedTry, Tuple.Create(
                defaultOutput1,
                defaultOutput2
            ), reInitialize, out success, out errorMessage);

            output1=output.Item1; 
            output2=output.Item2; 
        }
    }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCH 2
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch, runs catch instead if it has been unsuccessful. Supports 2 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryCatchStateful2<TState> : TryCatchStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TOutput1, TOutput2>> @try,
            Func<TState, Exception, Tuple<TState, TOutput1, TOutput2>> @catch,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out TOutput1 output1,
            out TOutput2 output2
            )
        {
            Func<TState, Tuple<TState, Tuple< TOutput1, TOutput2 >>> packedTry = (state) =>
            {
                var unpackedUpdate = @try(state);
                return Tuple.Create(unpackedUpdate.Item1, Tuple.Create(
 
                unpackedUpdate.Item2,
                unpackedUpdate.Item3

                ));
            };

            Func<TState, Exception, Tuple<TState, Tuple< TOutput1, TOutput2 >>> packedCatch = (state, ex) =>
            {
                var unpackedUpdate = @catch(state, ex);
                return Tuple.Create(unpackedUpdate.Item1, Tuple.Create(
 
                unpackedUpdate.Item2,
                unpackedUpdate.Item3

                ));
            };

            var output = base.Update(create, packedTry, packedCatch, reInitialize);

            output1=output.Item1; 
            output2=output.Item2; 
        }
    }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCHFINALLY 2
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch, runs catch instead if it has been unsuccessful. Supports 2 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryCatchFinallyStateful2<TState> : TryCatchFinallyStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2, TData1, TData2 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TData1, TData2>> @try,
            Func<TState, Exception, Tuple<TState, TData1, TData2>> @catch,
            Func<TState, TData1, TData2, Tuple<TState, TOutput1, TOutput2 >> @finally,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out TOutput1 output1,
            out TOutput2 output2
            )
        {
            Func<TState, Tuple<TState, Tuple< TData1, TData2 >>> packedTry = (state) =>
            {
                var unpackedTry = @try(state);
                return Tuple.Create(unpackedTry.Item1, Tuple.Create(
 
                unpackedTry.Item2,
                unpackedTry.Item3

                ));
            };

            Func<TState, Exception, Tuple<TState, Tuple< TData1, TData2 >>> packedCatch = (state, ex) =>
            {
                var unpackedCatch = @catch(state, ex);
                return Tuple.Create(unpackedCatch.Item1, Tuple.Create(
 
                unpackedCatch.Item2,
                unpackedCatch.Item3

                ));
            };

            Func<TState, Tuple< TData1, TData2 >, Tuple<TState, Tuple< TOutput1, TOutput2 >>> packedFinally =         
            (state, dataTuple ) =>
            {
                var unpackedFinally = @finally
                (state, dataTuple.Item1, dataTuple.Item2);
                return Tuple.Create(unpackedFinally.Item1, Tuple.Create(
 
                unpackedFinally.Item2,
                unpackedFinally.Item3

                ));
            };

            var output = base.Update(create, packedTry, packedCatch, packedFinally, reInitialize);

            output1=output.Item1; 
            output2=output.Item2; 
        }
    }



    /// ---------------------------------------------------------------------------------------------------------
    /// TRY 3
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch and returns whether it has been successful or not. Supports 3 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryStateful3<TState> : TryStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2, TOutput3 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TOutput1, TOutput2, TOutput3>> @try,
            TOutput1 defaultOutput1,
            out TOutput1 output1,
            TOutput2 defaultOutput2,
            out TOutput2 output2,
            TOutput3 defaultOutput3,
            out TOutput3 output3,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out bool success,
            out string errorMessage)
        {
            Func<TState, Tuple<TState, Tuple<TOutput1, TOutput2, TOutput3>>> packedTry = (state) =>
            {
                var unpackedTry = @try(state);
                return Tuple.Create(unpackedTry.Item1, Tuple.Create(
                unpackedTry.Item2,
                unpackedTry.Item3,
                unpackedTry.Item4
                ));
            };
            var output = base.Update(create, packedTry, Tuple.Create(
                defaultOutput1,
                defaultOutput2,
                defaultOutput3
            ), reInitialize, out success, out errorMessage);

            output1=output.Item1; 
            output2=output.Item2; 
            output3=output.Item3; 
        }
    }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCH 3
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch, runs catch instead if it has been unsuccessful. Supports 3 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryCatchStateful3<TState> : TryCatchStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2, TOutput3 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TOutput1, TOutput2, TOutput3>> @try,
            Func<TState, Exception, Tuple<TState, TOutput1, TOutput2, TOutput3>> @catch,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out TOutput1 output1,
            out TOutput2 output2,
            out TOutput3 output3
            )
        {
            Func<TState, Tuple<TState, Tuple< TOutput1, TOutput2, TOutput3 >>> packedTry = (state) =>
            {
                var unpackedUpdate = @try(state);
                return Tuple.Create(unpackedUpdate.Item1, Tuple.Create(
 
                unpackedUpdate.Item2,
 
                unpackedUpdate.Item3,
                unpackedUpdate.Item4

                ));
            };

            Func<TState, Exception, Tuple<TState, Tuple< TOutput1, TOutput2, TOutput3 >>> packedCatch = (state, ex) =>
            {
                var unpackedUpdate = @catch(state, ex);
                return Tuple.Create(unpackedUpdate.Item1, Tuple.Create(
 
                unpackedUpdate.Item2,
 
                unpackedUpdate.Item3,
                unpackedUpdate.Item4

                ));
            };

            var output = base.Update(create, packedTry, packedCatch, reInitialize);

            output1=output.Item1; 
            output2=output.Item2; 
            output3=output.Item3; 
        }
    }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCHFINALLY 3
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch, runs catch instead if it has been unsuccessful. Supports 3 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryCatchFinallyStateful3<TState> : TryCatchFinallyStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2, TOutput3, TData1, TData2, TData3 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TData1, TData2, TData3>> @try,
            Func<TState, Exception, Tuple<TState, TData1, TData2, TData3>> @catch,
            Func<TState, TData1, TData2, TData3, Tuple<TState, TOutput1, TOutput2, TOutput3 >> @finally,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out TOutput1 output1,
            out TOutput2 output2,
            out TOutput3 output3
            )
        {
            Func<TState, Tuple<TState, Tuple< TData1, TData2, TData3 >>> packedTry = (state) =>
            {
                var unpackedTry = @try(state);
                return Tuple.Create(unpackedTry.Item1, Tuple.Create(
 
                unpackedTry.Item2,
 
                unpackedTry.Item3,
                unpackedTry.Item4

                ));
            };

            Func<TState, Exception, Tuple<TState, Tuple< TData1, TData2, TData3 >>> packedCatch = (state, ex) =>
            {
                var unpackedCatch = @catch(state, ex);
                return Tuple.Create(unpackedCatch.Item1, Tuple.Create(
 
                unpackedCatch.Item2,
 
                unpackedCatch.Item3,
                unpackedCatch.Item4

                ));
            };

            Func<TState, Tuple< TData1, TData2, TData3 >, Tuple<TState, Tuple< TOutput1, TOutput2, TOutput3 >>> packedFinally =         
            (state, dataTuple ) =>
            {
                var unpackedFinally = @finally
                (state, dataTuple.Item1, dataTuple.Item2, dataTuple.Item3);
                return Tuple.Create(unpackedFinally.Item1, Tuple.Create(
 
                unpackedFinally.Item2,
 
                unpackedFinally.Item3,
                unpackedFinally.Item4

                ));
            };

            var output = base.Update(create, packedTry, packedCatch, packedFinally, reInitialize);

            output1=output.Item1; 
            output2=output.Item2; 
            output3=output.Item3; 
        }
    }



    /// ---------------------------------------------------------------------------------------------------------
    /// TRY 4
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch and returns whether it has been successful or not. Supports 4 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryStateful4<TState> : TryStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2, TOutput3, TOutput4 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TOutput1, TOutput2, TOutput3, TOutput4>> @try,
            TOutput1 defaultOutput1,
            out TOutput1 output1,
            TOutput2 defaultOutput2,
            out TOutput2 output2,
            TOutput3 defaultOutput3,
            out TOutput3 output3,
            TOutput4 defaultOutput4,
            out TOutput4 output4,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out bool success,
            out string errorMessage)
        {
            Func<TState, Tuple<TState, Tuple<TOutput1, TOutput2, TOutput3, TOutput4>>> packedTry = (state) =>
            {
                var unpackedTry = @try(state);
                return Tuple.Create(unpackedTry.Item1, Tuple.Create(
                unpackedTry.Item2,
                unpackedTry.Item3,
                unpackedTry.Item4,
                unpackedTry.Item5
                ));
            };
            var output = base.Update(create, packedTry, Tuple.Create(
                defaultOutput1,
                defaultOutput2,
                defaultOutput3,
                defaultOutput4
            ), reInitialize, out success, out errorMessage);

            output1=output.Item1; 
            output2=output.Item2; 
            output3=output.Item3; 
            output4=output.Item4; 
        }
    }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCH 4
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch, runs catch instead if it has been unsuccessful. Supports 4 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryCatchStateful4<TState> : TryCatchStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2, TOutput3, TOutput4 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TOutput1, TOutput2, TOutput3, TOutput4>> @try,
            Func<TState, Exception, Tuple<TState, TOutput1, TOutput2, TOutput3, TOutput4>> @catch,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out TOutput1 output1,
            out TOutput2 output2,
            out TOutput3 output3,
            out TOutput4 output4
            )
        {
            Func<TState, Tuple<TState, Tuple< TOutput1, TOutput2, TOutput3, TOutput4 >>> packedTry = (state) =>
            {
                var unpackedUpdate = @try(state);
                return Tuple.Create(unpackedUpdate.Item1, Tuple.Create(
 
                unpackedUpdate.Item2,
 
                unpackedUpdate.Item3,
 
                unpackedUpdate.Item4,
                unpackedUpdate.Item5

                ));
            };

            Func<TState, Exception, Tuple<TState, Tuple< TOutput1, TOutput2, TOutput3, TOutput4 >>> packedCatch = (state, ex) =>
            {
                var unpackedUpdate = @catch(state, ex);
                return Tuple.Create(unpackedUpdate.Item1, Tuple.Create(
 
                unpackedUpdate.Item2,
 
                unpackedUpdate.Item3,
 
                unpackedUpdate.Item4,
                unpackedUpdate.Item5

                ));
            };

            var output = base.Update(create, packedTry, packedCatch, reInitialize);

            output1=output.Item1; 
            output2=output.Item2; 
            output3=output.Item3; 
            output4=output.Item4; 
        }
    }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCHFINALLY 4
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch, runs catch instead if it has been unsuccessful. Supports 4 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryCatchFinallyStateful4<TState> : TryCatchFinallyStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2, TOutput3, TOutput4, TData1, TData2, TData3, TData4 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TData1, TData2, TData3, TData4>> @try,
            Func<TState, Exception, Tuple<TState, TData1, TData2, TData3, TData4>> @catch,
            Func<TState, TData1, TData2, TData3, TData4, Tuple<TState, TOutput1, TOutput2, TOutput3, TOutput4 >> @finally,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out TOutput1 output1,
            out TOutput2 output2,
            out TOutput3 output3,
            out TOutput4 output4
            )
        {
            Func<TState, Tuple<TState, Tuple< TData1, TData2, TData3, TData4 >>> packedTry = (state) =>
            {
                var unpackedTry = @try(state);
                return Tuple.Create(unpackedTry.Item1, Tuple.Create(
 
                unpackedTry.Item2,
 
                unpackedTry.Item3,
 
                unpackedTry.Item4,
                unpackedTry.Item5

                ));
            };

            Func<TState, Exception, Tuple<TState, Tuple< TData1, TData2, TData3, TData4 >>> packedCatch = (state, ex) =>
            {
                var unpackedCatch = @catch(state, ex);
                return Tuple.Create(unpackedCatch.Item1, Tuple.Create(
 
                unpackedCatch.Item2,
 
                unpackedCatch.Item3,
 
                unpackedCatch.Item4,
                unpackedCatch.Item5

                ));
            };

            Func<TState, Tuple< TData1, TData2, TData3, TData4 >, Tuple<TState, Tuple< TOutput1, TOutput2, TOutput3, TOutput4 >>> packedFinally =         
            (state, dataTuple ) =>
            {
                var unpackedFinally = @finally
                (state, dataTuple.Item1, dataTuple.Item2, dataTuple.Item3, dataTuple.Item4);
                return Tuple.Create(unpackedFinally.Item1, Tuple.Create(
 
                unpackedFinally.Item2,
 
                unpackedFinally.Item3,
 
                unpackedFinally.Item4,
                unpackedFinally.Item5

                ));
            };

            var output = base.Update(create, packedTry, packedCatch, packedFinally, reInitialize);

            output1=output.Item1; 
            output2=output.Item2; 
            output3=output.Item3; 
            output4=output.Item4; 
        }
    }



    /// ---------------------------------------------------------------------------------------------------------
    /// TRY 5
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch and returns whether it has been successful or not. Supports 5 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryStateful5<TState> : TryStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TOutput1, TOutput2, TOutput3, TOutput4, TOutput5>> @try,
            TOutput1 defaultOutput1,
            out TOutput1 output1,
            TOutput2 defaultOutput2,
            out TOutput2 output2,
            TOutput3 defaultOutput3,
            out TOutput3 output3,
            TOutput4 defaultOutput4,
            out TOutput4 output4,
            TOutput5 defaultOutput5,
            out TOutput5 output5,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out bool success,
            out string errorMessage)
        {
            Func<TState, Tuple<TState, Tuple<TOutput1, TOutput2, TOutput3, TOutput4, TOutput5>>> packedTry = (state) =>
            {
                var unpackedTry = @try(state);
                return Tuple.Create(unpackedTry.Item1, Tuple.Create(
                unpackedTry.Item2,
                unpackedTry.Item3,
                unpackedTry.Item4,
                unpackedTry.Item5,
                unpackedTry.Item6
                ));
            };
            var output = base.Update(create, packedTry, Tuple.Create(
                defaultOutput1,
                defaultOutput2,
                defaultOutput3,
                defaultOutput4,
                defaultOutput5
            ), reInitialize, out success, out errorMessage);

            output1=output.Item1; 
            output2=output.Item2; 
            output3=output.Item3; 
            output4=output.Item4; 
            output5=output.Item5; 
        }
    }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCH 5
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch, runs catch instead if it has been unsuccessful. Supports 5 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryCatchStateful5<TState> : TryCatchStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TOutput1, TOutput2, TOutput3, TOutput4, TOutput5>> @try,
            Func<TState, Exception, Tuple<TState, TOutput1, TOutput2, TOutput3, TOutput4, TOutput5>> @catch,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out TOutput1 output1,
            out TOutput2 output2,
            out TOutput3 output3,
            out TOutput4 output4,
            out TOutput5 output5
            )
        {
            Func<TState, Tuple<TState, Tuple< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5 >>> packedTry = (state) =>
            {
                var unpackedUpdate = @try(state);
                return Tuple.Create(unpackedUpdate.Item1, Tuple.Create(
 
                unpackedUpdate.Item2,
 
                unpackedUpdate.Item3,
 
                unpackedUpdate.Item4,
 
                unpackedUpdate.Item5,
                unpackedUpdate.Item6

                ));
            };

            Func<TState, Exception, Tuple<TState, Tuple< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5 >>> packedCatch = (state, ex) =>
            {
                var unpackedUpdate = @catch(state, ex);
                return Tuple.Create(unpackedUpdate.Item1, Tuple.Create(
 
                unpackedUpdate.Item2,
 
                unpackedUpdate.Item3,
 
                unpackedUpdate.Item4,
 
                unpackedUpdate.Item5,
                unpackedUpdate.Item6

                ));
            };

            var output = base.Update(create, packedTry, packedCatch, reInitialize);

            output1=output.Item1; 
            output2=output.Item2; 
            output3=output.Item3; 
            output4=output.Item4; 
            output5=output.Item5; 
        }
    }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCHFINALLY 5
    /// ---------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Runs the given patch, runs catch instead if it has been unsuccessful. Supports 5 regular output pins
	/// </summary>
    [ProcessNode]
    public class TryCatchFinallyStateful5<TState> : TryCatchFinallyStateful<TState>
        where TState : class
    {
        public void Update< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5, TData1, TData2, TData3, TData4, TData5 >(
            Func<TState> create,
            Func<TState, Tuple<TState, TData1, TData2, TData3, TData4, TData5>> @try,
            Func<TState, Exception, Tuple<TState, TData1, TData2, TData3, TData4, TData5>> @catch,
            Func<TState, TData1, TData2, TData3, TData4, TData5, Tuple<TState, TOutput1, TOutput2, TOutput3, TOutput4, TOutput5 >> @finally,
            [Pin(Visibility = Model.PinVisibility.Optional)] bool reInitialize,
            out TOutput1 output1,
            out TOutput2 output2,
            out TOutput3 output3,
            out TOutput4 output4,
            out TOutput5 output5
            )
        {
            Func<TState, Tuple<TState, Tuple< TData1, TData2, TData3, TData4, TData5 >>> packedTry = (state) =>
            {
                var unpackedTry = @try(state);
                return Tuple.Create(unpackedTry.Item1, Tuple.Create(
 
                unpackedTry.Item2,
 
                unpackedTry.Item3,
 
                unpackedTry.Item4,
 
                unpackedTry.Item5,
                unpackedTry.Item6

                ));
            };

            Func<TState, Exception, Tuple<TState, Tuple< TData1, TData2, TData3, TData4, TData5 >>> packedCatch = (state, ex) =>
            {
                var unpackedCatch = @catch(state, ex);
                return Tuple.Create(unpackedCatch.Item1, Tuple.Create(
 
                unpackedCatch.Item2,
 
                unpackedCatch.Item3,
 
                unpackedCatch.Item4,
 
                unpackedCatch.Item5,
                unpackedCatch.Item6

                ));
            };

            Func<TState, Tuple< TData1, TData2, TData3, TData4, TData5 >, Tuple<TState, Tuple< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5 >>> packedFinally =         
            (state, dataTuple ) =>
            {
                var unpackedFinally = @finally
                (state, dataTuple.Item1, dataTuple.Item2, dataTuple.Item3, dataTuple.Item4, dataTuple.Item5);
                return Tuple.Create(unpackedFinally.Item1, Tuple.Create(
 
                unpackedFinally.Item2,
 
                unpackedFinally.Item3,
 
                unpackedFinally.Item4,
 
                unpackedFinally.Item5,
                unpackedFinally.Item6

                ));
            };

            var output = base.Update(create, packedTry, packedCatch, packedFinally, reInitialize);

            output1=output.Item1; 
            output2=output.Item2; 
            output3=output.Item3; 
            output4=output.Item4; 
            output5=output.Item5; 
        }
    }


}
