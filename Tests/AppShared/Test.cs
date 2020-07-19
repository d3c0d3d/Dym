using Dym;
using Dym.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace AppShared
{
    public static class Test
    {

        private static IModuleHost Host;
        private static readonly string ModulesPath = Path.Combine(AppContext.BaseDirectory, "modules", "netstandard2.0");
        private static readonly string ModuleTester1_FileName = "ModuleTester1.dll";
        private static readonly string ModuleTester1_Path = Path.Combine(ModulesPath, ModuleTester1_FileName);

        private static readonly string ModuleTester2_FileName = "ModuleTester2.dll";
        private static readonly string ModuleTester2_Path = Path.Combine(ModulesPath, ModuleTester2_FileName);

        public static void SighTesterModules()
        {
            var module1Bytes = File.ReadAllBytes(ModuleTester1_Path);

            var module1Hash = Utilities.GetSHA256ChecksumFromString(module1Bytes.ToHex(false));
            Console.WriteLine($"{ModuleTester1_FileName}: {module1Hash}");
            Console.WriteLine($"{new string('-', 80)}");
        }

        public static void LoadTesterModules()
        {
            var paramsList = new List<TransportMessageBase>()
            {
                new TransportMessageBase(TransportType.Send,"type1Name","type1Value"),
                new TransportMessageBase(TransportType.Receipt,"type2Name","type2Value"),
                new TransportMessageBase(TransportType.Param,"type3Name","type3Value"),
            };

            //var jsonparms = Utilities.JSONSerializer<List<MessageBase>>.Serialize(paramsList);

            Host = new ModuleHost();
            Host.LoadAssemblies(ModuleTester1_Path, ModuleTester2_Path);
            Host.LoadModule(Host.ModulesUid[0], paramsList);
            Host.LoadModule(Host.ModulesUid[1]/*, jsonparms*/);

            PrintAllLoadedModulesInfos();

            //Host.UnloadModule(Host.ModulesUid[0]);

            //Host.SendMessagesToModule(Host.ModulesUid[0], Host.ModulesUid[1], "unsupported-message-test" , paramsList);
            Host.SendMessagesToModule(Host.ModulesUid[1] ,Host.ModulesUid[0], "print-message", paramsList);            
            
        }

        private static void PrintAllLoadedModulesInfos()
        {
            foreach (var uid in Host.ModulesUid)
            {
                var plug = Host.GetModuleLoadedInfos(uid);
                if (!plug.HasValue)
                    continue;
                Console.WriteLine($"{nameof(plug.Value.Uid)}: {plug.Value.Uid}");
                Console.WriteLine($"{nameof(plug.Value.SessionKey)}: {plug.Value.SessionKey}");
                Console.WriteLine($"{nameof(plug.Value.FriendlyName)}: {plug.Value.FriendlyName}");
                Console.WriteLine($"{nameof(plug.Value.Version)}: {plug.Value.Version}");
                Console.WriteLine($"{nameof(plug.Value.IsStarted)}: {plug.Value.IsStarted}");
                Console.WriteLine($"{nameof(plug.Value.ModuleType)}: {plug.Value.ModuleType}");
                Console.WriteLine($"{nameof(plug.Value.AssemblyHash)}: {plug.Value.AssemblyHash}");
                Console.WriteLine($"{new string('-', 80)}");
            }
        }
    }
}
