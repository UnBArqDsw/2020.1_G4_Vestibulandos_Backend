using System;

namespace Common.Util
{
    /// <summary>
    /// Data Operation Assistance
    /// </summary>
    public class DataHelper
    {
        /// <summary>
        /// Byte data Copy
        /// </summary>
        /// <param name="copyTo">Target byte array</param>
        /// <param name="offsetTo">Copy offset of the target byte array</param>
        /// <param name="copyFrom">Source byte array</param>
        /// <param name="offsetFrom">Source offset of the byte array</param>
        /// <param name="count">Number of bytes copied</param>
        public static void CopyBytes(byte[] copyTo, int offsetTo, byte[] copyFrom, int offsetFrom, int count)
        {
            Array.Copy(copyFrom, offsetFrom, copyTo, offsetTo, count);
        }

        /// <summary>
        /// Byte data sort
        /// </summary>
        /// <param name="copyTo"></param>
        /// <param name="offsetTo"></param>
        /// <param name="count"></param>
        public static void SortBytes(byte[] bytesData, int offsetTo, int count, ulong ulKey)
        {
            byte bKey = (byte)ulKey;

            if (count <= 32)
            {
                int tc = offsetTo + count;
                for (int x = offsetTo; x < tc; x++)
                {
                    bytesData[x] ^= bKey;
                }
            }
            else
            {
                int t = count / 8;

                unsafe
                {
                    fixed (byte* p = &bytesData[offsetTo])
                    {
                        ulong* pl = (ulong*)p;
                        for (int n = 0; n < t; n++)
                        {
                            pl[n] ^= ulKey;
                        }
                    }
                }

                int tc = offsetTo + count;
                for (int x = offsetTo + t * 8; x < tc; x++)
                {

                    bytesData[x] ^= bKey;
                }
            }
        }

        /// <summary>
        /// Compare two byte arrays are the same
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool CompBytes(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            bool ret = true;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Compare two byte arrays are the same
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool CompBytes(byte[] left, byte[] right, int count)
        {
            if (left.Length < count || right.Length < count)
            {
                return false;
            }

            bool ret = true;
            for (int i = 0; i < count; i++)
            {
                if (left[i] != right[i])
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Convert byte stream to Hex encoded string (without delimiters)
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string Bytes2HexString(byte[] b)
        {
            int ch = 0;
            string ret = "";
            for (int i = 0; i < b.Length; i++)
            {
                ch = (b[i] & 0xFF);
                ret += ch.ToString("X2").ToUpper();
            }

            return ret;
        }

        /// <summary>
        /// Convert a Hex-encoded string to a byte stream (no delimiters)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] HexString2Bytes(string s)
        {
            // Illegal string
            if (s.Length % 2 != 0)
            {
                return null;
            }

            int b = 0;
            string hexstr = "";
            byte[] bytesData = new byte[s.Length / 2];
            for (int i = 0; i < s.Length / 2; i++)
            {
                hexstr = s.Substring(i * 2, 2);
                b = Int32.Parse(hexstr, System.Globalization.NumberStyles.HexNumber) & 0xFF;
                bytesData[i] = (byte)b;
            }

            return bytesData;
        }

        /// <summary>
        /// If it is not "*", it will change to the specified value, otherwise the default value
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Int32 ConvertToInt32(string str, Int32 defVal)
        {
            try
            {
                if ("*" != str)
                {
                    return Convert.ToInt32(str);
                }

                return defVal;
            }
            catch (Exception)
            {
            }

            return defVal;
        }

        /// <summary>
        /// If it is not "*", it will change to the specified value, otherwise the default value
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ConvertToStr(string str, string defVal)
        {
            if ("*" != str)
            {
                return str;
            }

            return defVal;
        }

        /// <summary>
        /// Convert a datetime string to an integer representation
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static long ConvertToTicks(string str, long defVal)
        {
            if ("*" == str)
            {
                return defVal;
            }

            str = str.Replace('$', ':');

            try
            {
                DateTime dt;
                if (!DateTime.TryParse(str, out dt))
                {
                    return 0L;
                }

                return dt.Ticks / 10000;
            }
            catch (Exception)
            {
            }

            return 0L;
        }

        /// <summary>
        /// Convert a datetime string to an integer representation
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static long ConvertToTicks(string str)
        {
            try
            {
                if (!DateTime.TryParse(str, out DateTime dt))
                {
                    return 0L;
                }

                return dt.Ticks / 10000;
            }
            catch (Exception)
            {
            }

            return 0L;
        }

        #region Type conversion

        /// <summary>
        /// Safe conversion time
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static long SafeConvertToTicks(string str)
        {
            try
            {
                if (string.IsNullOrEmpty(str))
                    return 0;

                if (!DateTime.TryParse(str, out DateTime dt))
                {
                    return 0L;
                }

                return dt.Ticks / 10000;
            }
            catch (Exception)
            {
            }

            return 0L;
        }

        /// <summary>
        /// Secure string-to-integer conversion
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int SafeConvertToInt32(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return 0;
            }

            str = str.Trim();
            if (string.IsNullOrEmpty(str))
                return 0;

            try
            {
                return Convert.ToInt32(str);
            }
            catch (Exception)
            {
            }

            return 0;
        }

        /// <summary>
        /// Secure string-to-integer conversion
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static long SafeConvertToInt64(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return 0;
            }

            str = str.Trim();

            if (string.IsNullOrEmpty(str))
                return 0;

            try
            {
                return Convert.ToInt64(str);
            }
            catch (Exception)
            {
            }

            return 0;
        }

        /// <summary>
        /// Secure string to floating point conversion
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static double SafeConvertToDouble(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0.0;

            str = str.Trim();

            if (string.IsNullOrEmpty(str))
                return 0.0;

            try
            {
                return Convert.ToDouble(str);
            }
            catch (Exception)
            {
            }

            return 0.0;
        }

        /// <summary>
        /// Converts a string to a Double array
        /// </summary>
        /// <param name="ss">Array of strings</param>
        /// <returns></returns>
        public static double[] String2DoubleArray(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            string[] sa = str.Split(',');
            return StringArray2DoubleArray(sa);
        }

        /// <summary>
        /// Converts an array of strings to an array of type double
        /// </summary>
        /// <param name="ss">Array of strings</param>
        /// <returns></returns>
        public static double[] StringArray2DoubleArray(string[] sa)
        {
            double[] da = new double[sa.Length];
            try
            {
                for (int i = 0; i < sa.Length; i++)
                {
                    string str = sa[i].Trim();
                    str = string.IsNullOrEmpty(str) ? "0.0" : str;
                    da[i] = Convert.ToDouble(str);
                }
            }
            catch (System.Exception ex)
            {
                string msg = ex.ToString();
            }

            return da;
        }

        /// <summary>
        /// Convert a string to an Int array
        /// </summary>
        /// <param name="ss">Array of strings</param>
        /// <returns></returns>
        public static int[] String2IntArray(string str, char spliter = ',')
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            string[] sa = str.Split(spliter);
            return StringArray2IntArray(sa);
        }

        /// <summary>
        /// Convert a string to an Int array
        /// </summary>
        /// <param name="ss">Array of strings</param>
        /// <returns></returns>
        public static string[] String2StringArray(string str, char spliter = '|')
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }

            return str.Split(spliter);
        }

        /// <summary>
        /// Converts an array of strings to an Int array
        /// </summary>
        /// <param name="ss">Array of strings</param>
        /// <returns></returns>
        public static int[] StringArray2IntArray(string[] sa)
        {
            if (sa == null)
                return null;
            return StringArray2IntArray(sa, 0, sa.Length);
        }

        public static int[] StringArray2IntArray(string[] sa, int start, int count)
        {
            if (sa == null) return null;
            if (start < 0 || start >= sa.Length) return null;
            if (count <= 0) return null;
            if (sa.Length - start < count) return null;

            int[] result = new int[count];
            for (int i = 0; i < count; ++i)
            {
                string str = sa[start + i].Trim();
                str = string.IsNullOrEmpty(str) ? "0" : str;
                result[i] = Convert.ToInt32(str);
            }

            return result;
        }

        #endregion // type conversion

        /// <summary>
        /// Returns how many days the server time has elapsed with respect to "2011-11-11"
        /// You can avoid the use of DayOfYear produced by the New Year problem
        /// </summary>
        /// <returns></returns>
        public static int GetOffsetHour(DateTime now)
        {
            TimeSpan ts = now - DateTime.Parse("2011-11-11");

            // Milliseconds elapsed
            double temp = ts.TotalMilliseconds;
            int day = (int)(temp / 1000 / 60 / 60);
            return day;
        }
    }
}
