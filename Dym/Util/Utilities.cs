using System;
using System.IO;
using System.Reflection;
using System.Collections.Concurrent;
using Dym.Extensions;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Net;
using System.Runtime.Serialization.Json;

namespace Dym.Util
{
    public static class Utilities
    {
        private static readonly string[] manifestResources = Assembly.GetExecutingAssembly().GetManifestResourceNames();

        internal static byte[] GetEmbeddedResourceBytes(string resourceName)
        {
            var resourceFullName = manifestResources.FirstOrDefault(N => N.Contains(resourceName));

            if (resourceFullName != null)
            {
                return Path.GetExtension(manifestResources.FirstOrDefault(N => N.Contains(resourceName))) == ".comp"
                    ? Decompress(Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullName).ReadFully())
                    : Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullName).ReadFully();
            }

            return null;
        }

        public static byte[] ReadFully(this Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        internal static byte[] Compress(byte[] Bytes)
        {
            byte[] compressedBytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
                {
                    deflateStream.Write(Bytes, 0, Bytes.Length);
                }
                compressedBytes = memoryStream.ToArray();
            }
            return compressedBytes;
        }

        internal static byte[] Decompress(byte[] compressed)
        {
            using (MemoryStream inputStream = new MemoryStream(compressed.Length))
            {
                inputStream.Write(compressed, 0, compressed.Length);
                inputStream.Seek(0, SeekOrigin.Begin);
                using (MemoryStream outputStream = new MemoryStream())
                {
                    using (DeflateStream deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = deflateStream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            outputStream.Write(buffer, 0, bytesRead);
                        }
                    }
                    return outputStream.ToArray();
                }
            }
        }

        public static string GetEnvLoggerFile(string varName)
        {
            try
            {
                if (varName.IsNull())
                    throw new ArgumentNullException(varName);

                return Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Machine);
            }
            catch { }

            return null;
        }

        public static string AssemblyDirectory
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static ConcurrentDictionary<string, string> ParamsParse(string[] parms, char delimiter = ':')
        {
            var result = new ConcurrentDictionary<string, string>();
            foreach (var parm in parms)
            {
                if (parm.Contains(delimiter))
                {
                    var splitParm = parm.Split(delimiter);
                    result.TryAdd(splitParm[0], splitParm[1]);
                }
                else
                    result.TryAdd(parm, parm);

            }

            return result;
        }

        public static string GetFullError(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine(ex.Message);

            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                sb.AppendLine(ex.Message);
            }
            return sb.ToString();
        }

        public static string GetFullStackTraceError(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine(ex.StackTrace);

            if (ex.InnerException != null)
            {
                var fullStack = ex.InnerException.StackTrace;
                sb.AppendLine(fullStack);
            }
            return sb.ToString();
        }

        public static string ToHex(this byte[] bytes, bool upperCase)
        {
            var result = new StringBuilder(bytes.Length * 2);

            foreach (var t in bytes)
                result.Append(t.ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }

        public static string GetSHA256ChecksumFromString(string s)
        {
            var sha = new SHA256Managed();
            var enc = new ASCIIEncoding();
            var bt = enc.GetBytes(s);
            byte[] checksum = sha.ComputeHash(bt);
            var sb = new StringBuilder();
            foreach (var t in checksum)
            {
                sb.Append(t.ToString("x2"));
            }
            return sb.ToString();
        }

        public static bool PortInUse(int port)
        {
            bool inUse = false;

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    break;
                }
            }
            return inUse;
        }

        public static class JSONSerializer<TType> where TType : class
        {
            /// <summary>
            /// Serializes an object to JSON
            /// </summary>
            public static string Serialize(TType instance)
            {
                var serializer = new DataContractJsonSerializer(typeof(TType));
                using (var stream = new MemoryStream())
                {
                    serializer.WriteObject(stream, instance);
                    // todo: break with !@#$%¨&*() in default encoding
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
            }

            /// <summary>
            /// DeSerializes an object from JSON
            /// </summary>
            public static TType DeSerialize(string json)
            {
                // todo: break with !@#$%¨&*() in default encoding
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(TType));
                    return serializer.ReadObject(stream) as TType;
                }
            }
        }
    }
}
