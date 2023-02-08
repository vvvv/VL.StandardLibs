
// Do not modify, this is generated code!

using System;
using System.Diagnostics;
using VL.Core;

namespace VL.Lib.Control
{
    partial class TryCatchUtils
    {
    /// ---------------------------------------------------------------------------------------------------------
    /// TRY 2
    /// ---------------------------------------------------------------------------------------------------------
    
	///<summary>
	///Runs the given stateless patch and returns whether it has been successful or not. Supports 2 regular outputs
	///</summary> 
        public static void Try2<TOutput1, TOutput2>(
        Func<Tuple< TOutput1, TOutput2 >> @try, 
        TOutput1 defaultOutput1, TOutput2 defaultOutput2, 
        out TOutput1 output1, out TOutput2 output2, 
        out bool success, 
        out string errorMessage)
        {
            success = true;
            errorMessage = "";
            try
            {
                var result = @try();
                output1 = result.Item1;
                output2 = result.Item2;
            }
            catch (Exception e)
            {
                success = false;
                errorMessage = e.InnermostException().Message;

                output1 = defaultOutput1;
                output2 = defaultOutput2;
            }
        }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCH 2
    /// ---------------------------------------------------------------------------------------------------------

	///<summary>
	///Runs the given stateless patch, runs Catch instead if it has been unsuccessful. Supports 2 regular outputs
	///</summary> 
    public static void TryCatch2<TOutput1, TOutput2>(
        Func<Tuple< TOutput1, TOutput2 >> @try,
        Func<Exception, Tuple< TOutput1, TOutput2 >> @catch, 
        out TOutput1 output1, out TOutput2 output2)
        {
            try
            {
                var result = @try();
                output1 = result.Item1;
                output2 = result.Item2;
            }
            catch (Exception e)
            {
                var result = @catch(e);
                output1 = result.Item1;
                output2 = result.Item2;
            }
        }

/// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCHFINALLY 2
    /// ---------------------------------------------------------------------------------------------------------

	///<summary>
	///Runs the given stateless patch, runs Catch instead if it has been unsuccessful. Guarantees to run Finally afterwards. Supports 2 regular outputs
	///</summary>
    public static void TryCatchFinally2< TOutput1, TOutput2, TData1, TData2 >(
        Func<Tuple< TData1, TData2 >> @try,
        Func<Exception, Tuple< TData1, TData2 >> @catch, 
        Func<Tuple< TData1, TData2 >, Tuple< TOutput1, TOutput2 >> @finally,
        out TOutput1 output1, out TOutput2 output2)
        {

            Tuple< TData1, TData2 > inter = null;
            Tuple< TOutput1, TOutput2 > final;
            try
            {
                inter = @try();
            }
            catch (Exception e)
            {
                inter = @catch(e);
            }
            finally
            {
                final = @finally(inter);
            }
            output1 = final.Item1;
            output2 = final.Item2;
        }


    /// ---------------------------------------------------------------------------------------------------------
    /// TRY 3
    /// ---------------------------------------------------------------------------------------------------------
    
	///<summary>
	///Runs the given stateless patch and returns whether it has been successful or not. Supports 3 regular outputs
	///</summary> 
        public static void Try3<TOutput1, TOutput2, TOutput3>(
        Func<Tuple< TOutput1, TOutput2, TOutput3 >> @try, 
        TOutput1 defaultOutput1, TOutput2 defaultOutput2, TOutput3 defaultOutput3, 
        out TOutput1 output1, out TOutput2 output2, out TOutput3 output3, 
        out bool success, 
        out string errorMessage)
        {
            success = true;
            errorMessage = "";
            try
            {
                var result = @try();
                output1 = result.Item1;
                output2 = result.Item2;
                output3 = result.Item3;
            }
            catch (Exception e)
            {
                success = false;
                errorMessage = e.InnermostException().Message;

                output1 = defaultOutput1;
                output2 = defaultOutput2;
                output3 = defaultOutput3;
            }
        }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCH 3
    /// ---------------------------------------------------------------------------------------------------------

	///<summary>
	///Runs the given stateless patch, runs Catch instead if it has been unsuccessful. Supports 3 regular outputs
	///</summary> 
    public static void TryCatch3<TOutput1, TOutput2, TOutput3>(
        Func<Tuple< TOutput1, TOutput2, TOutput3 >> @try,
        Func<Exception, Tuple< TOutput1, TOutput2, TOutput3 >> @catch, 
        out TOutput1 output1, out TOutput2 output2, out TOutput3 output3)
        {
            try
            {
                var result = @try();
                output1 = result.Item1;
                output2 = result.Item2;
                output3 = result.Item3;
            }
            catch (Exception e)
            {
                var result = @catch(e);
                output1 = result.Item1;
                output2 = result.Item2;
                output3 = result.Item3;
            }
        }

/// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCHFINALLY 3
    /// ---------------------------------------------------------------------------------------------------------

	///<summary>
	///Runs the given stateless patch, runs Catch instead if it has been unsuccessful. Guarantees to run Finally afterwards. Supports 3 regular outputs
	///</summary>
    public static void TryCatchFinally3< TOutput1, TOutput2, TOutput3, TData1, TData2, TData3 >(
        Func<Tuple< TData1, TData2, TData3 >> @try,
        Func<Exception, Tuple< TData1, TData2, TData3 >> @catch, 
        Func<Tuple< TData1, TData2, TData3 >, Tuple< TOutput1, TOutput2, TOutput3 >> @finally,
        out TOutput1 output1, out TOutput2 output2, out TOutput3 output3)
        {

            Tuple< TData1, TData2, TData3 > inter = null;
            Tuple< TOutput1, TOutput2, TOutput3 > final;
            try
            {
                inter = @try();
            }
            catch (Exception e)
            {
                inter = @catch(e);
            }
            finally
            {
                final = @finally(inter);
            }
            output1 = final.Item1;
            output2 = final.Item2;
            output3 = final.Item3;
        }


    /// ---------------------------------------------------------------------------------------------------------
    /// TRY 4
    /// ---------------------------------------------------------------------------------------------------------
    
	///<summary>
	///Runs the given stateless patch and returns whether it has been successful or not. Supports 4 regular outputs
	///</summary> 
        public static void Try4<TOutput1, TOutput2, TOutput3, TOutput4>(
        Func<Tuple< TOutput1, TOutput2, TOutput3, TOutput4 >> @try, 
        TOutput1 defaultOutput1, TOutput2 defaultOutput2, TOutput3 defaultOutput3, TOutput4 defaultOutput4, 
        out TOutput1 output1, out TOutput2 output2, out TOutput3 output3, out TOutput4 output4, 
        out bool success, 
        out string errorMessage)
        {
            success = true;
            errorMessage = "";
            try
            {
                var result = @try();
                output1 = result.Item1;
                output2 = result.Item2;
                output3 = result.Item3;
                output4 = result.Item4;
            }
            catch (Exception e)
            {
                success = false;
                errorMessage = e.InnermostException().Message;

                output1 = defaultOutput1;
                output2 = defaultOutput2;
                output3 = defaultOutput3;
                output4 = defaultOutput4;
            }
        }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCH 4
    /// ---------------------------------------------------------------------------------------------------------

	///<summary>
	///Runs the given stateless patch, runs Catch instead if it has been unsuccessful. Supports 4 regular outputs
	///</summary> 
    public static void TryCatch4<TOutput1, TOutput2, TOutput3, TOutput4>(
        Func<Tuple< TOutput1, TOutput2, TOutput3, TOutput4 >> @try,
        Func<Exception, Tuple< TOutput1, TOutput2, TOutput3, TOutput4 >> @catch, 
        out TOutput1 output1, out TOutput2 output2, out TOutput3 output3, out TOutput4 output4)
        {
            try
            {
                var result = @try();
                output1 = result.Item1;
                output2 = result.Item2;
                output3 = result.Item3;
                output4 = result.Item4;
            }
            catch (Exception e)
            {
                var result = @catch(e);
                output1 = result.Item1;
                output2 = result.Item2;
                output3 = result.Item3;
                output4 = result.Item4;
            }
        }

/// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCHFINALLY 4
    /// ---------------------------------------------------------------------------------------------------------

	///<summary>
	///Runs the given stateless patch, runs Catch instead if it has been unsuccessful. Guarantees to run Finally afterwards. Supports 4 regular outputs
	///</summary>
    public static void TryCatchFinally4< TOutput1, TOutput2, TOutput3, TOutput4, TData1, TData2, TData3, TData4 >(
        Func<Tuple< TData1, TData2, TData3, TData4 >> @try,
        Func<Exception, Tuple< TData1, TData2, TData3, TData4 >> @catch, 
        Func<Tuple< TData1, TData2, TData3, TData4 >, Tuple< TOutput1, TOutput2, TOutput3, TOutput4 >> @finally,
        out TOutput1 output1, out TOutput2 output2, out TOutput3 output3, out TOutput4 output4)
        {

            Tuple< TData1, TData2, TData3, TData4 > inter = null;
            Tuple< TOutput1, TOutput2, TOutput3, TOutput4 > final;
            try
            {
                inter = @try();
            }
            catch (Exception e)
            {
                inter = @catch(e);
            }
            finally
            {
                final = @finally(inter);
            }
            output1 = final.Item1;
            output2 = final.Item2;
            output3 = final.Item3;
            output4 = final.Item4;
        }


    /// ---------------------------------------------------------------------------------------------------------
    /// TRY 5
    /// ---------------------------------------------------------------------------------------------------------
    
	///<summary>
	///Runs the given stateless patch and returns whether it has been successful or not. Supports 5 regular outputs
	///</summary> 
        public static void Try5<TOutput1, TOutput2, TOutput3, TOutput4, TOutput5>(
        Func<Tuple< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5 >> @try, 
        TOutput1 defaultOutput1, TOutput2 defaultOutput2, TOutput3 defaultOutput3, TOutput4 defaultOutput4, TOutput5 defaultOutput5, 
        out TOutput1 output1, out TOutput2 output2, out TOutput3 output3, out TOutput4 output4, out TOutput5 output5, 
        out bool success, 
        out string errorMessage)
        {
            success = true;
            errorMessage = "";
            try
            {
                var result = @try();
                output1 = result.Item1;
                output2 = result.Item2;
                output3 = result.Item3;
                output4 = result.Item4;
                output5 = result.Item5;
            }
            catch (Exception e)
            {
                success = false;
                errorMessage = e.InnermostException().Message;

                output1 = defaultOutput1;
                output2 = defaultOutput2;
                output3 = defaultOutput3;
                output4 = defaultOutput4;
                output5 = defaultOutput5;
            }
        }

    /// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCH 5
    /// ---------------------------------------------------------------------------------------------------------

	///<summary>
	///Runs the given stateless patch, runs Catch instead if it has been unsuccessful. Supports 5 regular outputs
	///</summary> 
    public static void TryCatch5<TOutput1, TOutput2, TOutput3, TOutput4, TOutput5>(
        Func<Tuple< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5 >> @try,
        Func<Exception, Tuple< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5 >> @catch, 
        out TOutput1 output1, out TOutput2 output2, out TOutput3 output3, out TOutput4 output4, out TOutput5 output5)
        {
            try
            {
                var result = @try();
                output1 = result.Item1;
                output2 = result.Item2;
                output3 = result.Item3;
                output4 = result.Item4;
                output5 = result.Item5;
            }
            catch (Exception e)
            {
                var result = @catch(e);
                output1 = result.Item1;
                output2 = result.Item2;
                output3 = result.Item3;
                output4 = result.Item4;
                output5 = result.Item5;
            }
        }

/// ---------------------------------------------------------------------------------------------------------
    /// TRYCATCHFINALLY 5
    /// ---------------------------------------------------------------------------------------------------------

	///<summary>
	///Runs the given stateless patch, runs Catch instead if it has been unsuccessful. Guarantees to run Finally afterwards. Supports 5 regular outputs
	///</summary>
    public static void TryCatchFinally5< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5, TData1, TData2, TData3, TData4, TData5 >(
        Func<Tuple< TData1, TData2, TData3, TData4, TData5 >> @try,
        Func<Exception, Tuple< TData1, TData2, TData3, TData4, TData5 >> @catch, 
        Func<Tuple< TData1, TData2, TData3, TData4, TData5 >, Tuple< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5 >> @finally,
        out TOutput1 output1, out TOutput2 output2, out TOutput3 output3, out TOutput4 output4, out TOutput5 output5)
        {

            Tuple< TData1, TData2, TData3, TData4, TData5 > inter = null;
            Tuple< TOutput1, TOutput2, TOutput3, TOutput4, TOutput5 > final;
            try
            {
                inter = @try();
            }
            catch (Exception e)
            {
                inter = @catch(e);
            }
            finally
            {
                final = @finally(inter);
            }
            output1 = final.Item1;
            output2 = final.Item2;
            output3 = final.Item3;
            output4 = final.Item4;
            output5 = final.Item5;
        }


    }
}
