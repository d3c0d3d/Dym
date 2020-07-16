using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Dym.Extensions
{
    public static class StringExtensions
    {
        public static bool ContainsAny(this string haystack, params string[] needles)
        {
            foreach (var needle in needles)
            {
                if (haystack != null && haystack.Contains(needle))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Decode array bytes to string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToStr(this byte[] bytes)
        {
            var str = Encoding.Default.GetString(bytes).Replace("\0", "");
            return str;
        }

        /// <summary>
        /// Remove whitespace without exceptions
        /// </summary>
        /// <param name="str">string</param>
        /// <returns></returns>
        public static string TrimEx(this string str)
        {
            return str?.Trim();
        }

        /// <summary>
        /// Checks whether there is a value in the variable
        /// </summary>
        /// <param name="str">string</param>
        /// <returns></returns>
        public static bool IsNull(this string str)
        {
            return string.IsNullOrEmpty(str.TrimEx());
        }

        /// <summary>
        /// Test Base64 string
        /// </summary>
        /// <param name="str">string to test</param>
        /// <returns></returns>
        public static bool IsBase64String(this string str)
        {
            str = str.Trim();
            return !str.IsNull() && str.Length % 4 == 0 && Regex.IsMatch(str, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);

        }

        public static byte[] Base64StringToArray(this string base64String)
        {
            base64String = base64String.Replace("data:image/png;base64,", "");
            byte[] byteBuffer = Convert.FromBase64String(base64String);

            return byteBuffer;
        }

        public static string NormalizeWsHost(this string uri, bool ssl)
        {
            var wsPart = ssl ? "wss://" : "ws://";
            var hostPart = uri.Replace("ws://", "").Replace("wss://", "").Replace("ws:", "").Replace("wss:", "")
                .Replace("http://", "").Replace("https://", "");

            return $"{wsPart}{hostPart}";
        }

        public static string ParseWsHostToHttp(this string wsHost)
        {
            return wsHost.Replace("ws://", "http://").Replace("wss://", "https://");
        }

        public static string RemoveAccents(this string text)
        {
            var withAccents = "ÄÅÁÂÀÃäáâàãÉÊËÈéêëèÍÎÏÌíîïìÖÓÔÒÕöóôòõÜÚÛüúûùÇç";
            var withoutAccents = "AAAAAAaaaaaEEEEeeeeIIIIiiiiOOOOOoooooUUUuuuuCc";

            for (var i = 0; i < withAccents.Length; i++)
            {
                text = text.Replace(withAccents[i].ToString(), withoutAccents[i].ToString());
            }
            return text;
        }

        public static string FormatFileLength(this string str)
        {
            var length = Convert.ToInt64(str);

            if (length < 1024)
                return $"{length} B";
            if (length < 1024 * 1024)
                return $"{length / (double)1024:#,##0.00} KB";
            if (length < 1024 * 1024 * 1024)
                return $"{length / (double)(1024 * 1024):#,##0.00} MB";
            return $"{length / (double)(1024 * 1024 * 1024):#,##0.00} GB";
        }

        /// <summary>
        /// Replace string and remove ' ', '/' , '|', '-'
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string NormalizeStr(this string str)
        {
            str = str.Replace(" ", "_").Replace("/", "_").Replace("|", "_").Replace(":", "-").RemoveAccents();

            return str;
        }

        public static string ToShortStr(this string str, int counter, bool withEllipsis = true)
        {
            try
            {
                var strLeft = str.Substring(0, withEllipsis ? (counter % 2 == 0 ? counter / 2 - 1 : (counter + 1) / 2 - 1) : (counter % 2 == 0 ? counter / 2 : (counter + 1) / 2));
                var strRight = str.Substring(withEllipsis ? (str.Length - counter / 2) + 2 : (str.Length - counter / 2));

                return strLeft + (withEllipsis ? "..." : string.Empty) + strRight;
            }
            catch 
            {
                return str;
            }
            
        }

        public static string ToCamelCase(this string str, char delimiter = '-')
        {
            str = str.TrimEx();

            if (str.StartsWith(delimiter.ToString()))
                str = str.Substring(1);

            var strs = str.Split(delimiter);

            string strResult = string.Empty; 
            foreach (var s in strs)
            {                
                var strUp = char.ToUpper(s[0]) + s.Substring(1);
                strResult += strUp;
            }

            return strResult;          
        }

        public static IntPtr StringToInitPtr(this string str)
        {
            var b = new byte[str.Length + 1];
            int i;
            for (i = 0; i < str.Length; i++)
                b[i] = (byte)str.ToCharArray()[i];
            b[str.Length] = 0;
            var p = Marshal.AllocCoTaskMem(str.Length + 1);
            Marshal.Copy(b, 0, p, str.Length + 1);
            return p;
        }
    }
}
