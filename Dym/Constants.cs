using System;
using System.IO;
using Dym.Util;
using Dym.Extensions;

namespace Dym
{
    public class Constants
    {
        /// <summary>
        /// f326d185-dee7-4539-86c0-948d7ba7b223
        /// </summary>
        internal static byte[] ModuleUid = new byte[] { 0x85, 0xd1, 0x26, 0xf3, 0xe7, 0xde, 0x39, 0x45, 0x86, 0xc0, 0x94, 0x8d, 0x7b, 0xa7, 0xb2, 0x23 };
        internal static string ModuleFriendlyName = $"{nameof(ModuleLoader)}";
        internal static Version ModuleVersion = new Version(2020, 08, 02, 0);
        internal static string ModuleStorageConnStr = CONNPART.ToStr() + Path.Combine(Utilities.AssemblyDirectory, "_ms");

        // password=dbl33tdb;filename=
        internal static byte[] CONNPART
        {
            get
            {
                byte[] connpart =
                {
                    0x0, 0x18, 0xfa, 0xf4, 0xea, 0x4, 0xd0, 0x6,
                    0x5e, 0x12, 0xf8, 0xfe, 0x62, 0xbc, 0xfc, 0x6,
                    0xf4, 0x5c, 0x0, 0x0, 0x17, 0xe8, 0xf8, 0xf8,
                    0xfe, 0xf0, 0x5e, 0xa7
                };

                for (var m = 0; m < connpart.Length; ++m)
                {
                    var c = connpart[m];
                    c += (byte)m;
                    c = (byte)~c;
                    c -= 0xe3;
                    c ^= (byte)m;
                    c = (byte)(c >> 0x1 | c << 0x7);
                    c = (byte)-c;
                    c = (byte)~c;
                    c ^= 0x9d;
                    c = (byte)-c;
                    c = (byte)~c;
                    c ^= (byte)m;
                    c = (byte)~c;
                    c = (byte)-c;
                    c ^= (byte)m;
                    c = (byte)-c;
                    connpart[m] = c;
                }
                return connpart;
            }
        }

        // 1234567890abcdef
        public static byte[] NID
        {
            get
            {
                byte[] nid =
                {
                    0x76, 0x70, 0x72, 0x68, 0x6e, 0x58, 0x5a, 0x58,
                    0x46, 0xf0, 0x73, 0x89, 0x8f, 0x8d, 0x6b, 0x69,
                    0x58
                };

                for (var m = 0; m < nid.Length; ++m)
                {
                    var c = nid[m];
                    c ^= (byte)m;
                    c += (byte)m;
                    c ^= (byte)m;
                    c -= (byte)m;
                    c = (byte)-c;
                    c = (byte)(c >> 0x2 | c << 0x6);
                    c -= (byte)m;
                    c = (byte)~c;
                    c -= 0xc3;
                    c = (byte)(c >> 0x6 | c << 0x2);
                    c = (byte)-c;
                    c = (byte)(c >> 0x5 | c << 0x3);
                    c = (byte)-c;
                    c = (byte)(c >> 0x6 | c << 0x2);
                    c += (byte)m;
                    nid[m] = c;
                }
                return nid;
            }
        }

        // MODULE_FRAMEWORK_LOGGER
        public static byte[] MFL
        {
            get
            {
                byte[] mfl =
                {
                    0xb, 0x40, 0xd0, 0x7a, 0xef, 0x5, 0xc1, 0xfb,
                    0x2b, 0xc9, 0x1d, 0xe7, 0x4f, 0x2c, 0x31, 0xb,
                    0xb, 0x68, 0x75, 0x7f, 0xa6, 0xd5, 0x49, 0xd9
                };

                for (var m = 0; m < mfl.Length; ++m)
                {
                    var c = mfl[m];
                    c = (byte)-c;
                    c += (byte)m;
                    c ^= 0xe8;
                    c -= 0xc1;
                    c = (byte)-c;
                    c += 0xb6;
                    c ^= 0x66;
                    c += (byte)m;
                    c ^= (byte)m;
                    c = (byte)(c >> 0x7 | c << 0x1);
                    c ^= 0x68;
                    c = (byte)(c >> 0x3 | c << 0x5);
                    c ^= (byte)m;
                    c -= 0x68;
                    c = (byte)(c >> 0x1 | c << 0x7);
                    mfl[m] = c;
                }
                return mfl;
            }
        }
    }
}
