using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.ImGui.Stride
{
    internal static class Helper
    {
        private static EventId EventId = new EventId(42, "VL.ImGui.Stride");

        public static void Log(string message)
        {
            try
            {
                AppHost.Current.DefaultLogger.LogInformation(Helper.EventId, message);
            }  
            catch (Exception e) 
            { 

            }
            
        }

    }
}
