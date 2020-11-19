using System;

namespace Core.ExceptionHandler
{
    public class FilterException
    {
        //---------------------------------------------------------------------------------------------------
        private static bool DoFilter(Exception ex, Action<Exception> filter)
        {
            filter(ex);
            return true;
        }

        //---------------------------------------------------------------------------------------------------
        public static void Filter(Action body, Action<Exception> filter)
        {
            try
            {
                body();
            }
            catch (Exception ex) when (DoFilter(ex, filter))
            {
            }
        }

        //---------------------------------------------------------------------------------------------------
        public static void Filter(Action body, Action<Exception> filter, Action<Exception> handler)
        {
            try
            {
                body();
            }
            catch (Exception ex) when (DoFilter(ex, filter))
            {
                handler?.Invoke(ex);
            }
        }
    }
}
