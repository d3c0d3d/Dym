using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ModuleFramework
{
    public class ModuleLoader : MarshalByRefObject
    {
        private Assembly _moduleAssemby;
        private IModule _moduleInstance;
        public ModuleType ModuleType { get; private set; } = ModuleType.Undefined;
        public Guid Uid { get; private set; } = new Guid(Constants.ModuleUid);
        public string SessionKey { get; private set; }
        public string FriendlyName { get; private set; } = Constants.ModuleFriendlyName;
        public Version Version { get; private set; } = Constants.ModuleVersion;
        public bool IsStarted { get; private set; }
        public string AssemblyHash { get; private set; }

        public void Startup(IModuleHost moduleHost, byte[] asmRaw)
        {
            if (_moduleAssemby == null)
            {                   
                _moduleAssemby = AppDomain.CurrentDomain.Load(asmRaw);

                AssemblyHash = Utilities.GetSHA256ChecksumFromString(asmRaw.ToHex(false));
            }

            Startup(moduleHost, string.Empty);
        }

        public void Startup(IModuleHost moduleHost, string assemblyPath)
        {
            if(_moduleAssemby == null)
            {
                var fileBytes = File.ReadAllBytes(assemblyPath);
                _moduleAssemby = AppDomain.CurrentDomain.Load(fileBytes);

                AssemblyHash = Utilities.GetSHA256ChecksumFromString(fileBytes.ToHex(false));
            }

            Type[] types;
            try
            {
                types = _moduleAssemby.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }

            var type = types.FirstOrDefault(t => t.GetInterface(nameof(IModule)) != null);
            if (type != null && _moduleInstance == null)
            {
                _moduleInstance = (IModule)Activator.CreateInstance(type, null, null);                
                _moduleInstance.Startup(moduleHost,AssemblyHash);

                ModuleType = _moduleInstance.ModuleType;
                Uid = new Guid(_moduleInstance.Uid);
                SessionKey = _moduleInstance.SessionKey;
                FriendlyName = _moduleInstance.FriendlyName;
                Version = _moduleInstance.Version;                
            }
        }

        public void Load(params string[] parms)
        {
            IsStarted = true;
            if (_moduleInstance == null)
            {
                return;
            }
            _moduleInstance.Load(parms);            
        }        

        public void Dispose(params string[] parms)
        {
            if (_moduleInstance == null)
            {
                return;
            }
            _moduleInstance.Dispose(parms);
            IsStarted = false;
        }

        public void SendMessages(Guid uidFrom,string name, params string[] messages)
        {
            if (_moduleInstance == null)
            {
                return;
            }
            _moduleInstance.MessageCallback(uidFrom, name, messages);
        }
    }
}
