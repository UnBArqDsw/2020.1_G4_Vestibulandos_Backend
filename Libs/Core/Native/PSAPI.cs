using System;
using System.Runtime.InteropServices;

namespace Core.Native
{
    public class PSAPI
    {
        //---------------------------------------------------------------------------------------------------
        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);
    }
}
