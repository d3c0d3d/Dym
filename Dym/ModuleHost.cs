using Dym.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dym
{
    public class ModuleHost : MarshalByRefObject, IModuleHost
    {
        private readonly Dictionary<Guid, ModuleInfo> _modules = new Dictionary<Guid, ModuleInfo>();
        public Guid[] ModulesUid => _modules.Keys.ToArray();

        public void LoadAssembly(string friendlyName, byte[] mbytes)
        {
            using (var storage = new ModuleStorage())
            {
                var modulebaseUid = new Guid(Constants.ModuleUid).ToString();

                var domain = AppDomain.CreateDomain(friendlyName);

                Type loaderType = null;
                ModuleLoader loader = null;

                if (!storage.FileExists(modulebaseUid))
                {
                    var mfResource = Utilities.GetEmbeddedResourceBytes(nameof(Dym) + ".comp");

                    // load framework from resource
                    loaderType = domain.Load(mfResource).GetType(typeof(ModuleLoader).FullName);

                    loader = Activator.CreateInstance(loaderType) as ModuleLoader;
                    var loaderVersion = loaderType.GetProperty(nameof(ModuleLoader.Version)).GetValue(loader) as Version;
                    var loaderUid = loaderType.GetProperty(nameof(ModuleLoader.Uid)).GetValue(loader).ToString();
                    var loaderName = loaderType.GetProperty(nameof(ModuleLoader.FriendlyName)).GetValue(loader).ToString();
                    var loaderHash = loaderType.GetProperty(nameof(ModuleLoader.AssemblyHash)).GetValue(loader).ToString();

                    // save in db                    
                    storage.SaveOrUpdateFile(loaderUid, loaderName, loaderHash, loaderVersion, new MemoryStream(mfResource));

                }
                else
                {
                    // loadermodule
                    byte[] frameworkBytes = storage.GetFile(new Guid(Constants.ModuleUid).ToString());
                    loaderType = domain.Load(frameworkBytes).GetType(typeof(ModuleLoader).FullName);
                    loader = Activator.CreateInstance(loaderType) as ModuleLoader;
                }

                loader.Startup(this, mbytes);

                _modules.Add(loader.Uid, new ModuleInfo
                {
                    Domain = domain,
                    Loader = loader
                });

                // storage module
                storage.SaveOrUpdateFile(
                    loader.Uid.ToString(),
                    loader.FriendlyName, loader.AssemblyHash, loader.Version,
                    new MemoryStream(mbytes));
            }
        }

        public void LoadAssemblies(params string[] paths)
        {
            using (var storage = new ModuleStorage())
            {
                foreach (var path in paths)
                {
                    if (!File.Exists(path))
                        continue;

                    var filename = Path.GetFileNameWithoutExtension(path);

                    var domain = AppDomain.CreateDomain(filename);
                    var assemblyPath = Path.Combine(Assembly.GetExecutingAssembly().Location);

                    var fileFramework = File.ReadAllBytes(assemblyPath);
                    // load framework.dll
                    var loaderType = domain.Load(fileFramework).GetType(typeof(ModuleLoader).FullName);

                    var loader = Activator.CreateInstance(loaderType) as ModuleLoader;
                    var loaderVersion = loaderType.GetProperty(nameof(ModuleLoader.Version)).GetValue(loader) as Version;
                    var loaderUid = loaderType.GetProperty(nameof(ModuleLoader.Uid)).GetValue(loader).ToString();
                    var loaderName = loaderType.GetProperty(nameof(ModuleLoader.FriendlyName)).GetValue(loader).ToString();
                    
                    loader.Startup(this, path);

                    // save in db                    
                    storage.SaveOrUpdateFile(loaderUid, loaderName, loader.AssemblyHash, loaderVersion, new MemoryStream(fileFramework));


                    _modules.Add(loader.Uid, new ModuleInfo
                    {
                        Domain = domain,
                        Loader = loader
                    });

                    // storage module
                    storage.SaveOrUpdateFile(
                        loader.Uid.ToString(),
                        loader.FriendlyName, loader.AssemblyHash, loader.Version,
                        new MemoryStream(File.ReadAllBytes(path)));
                }
            }
        }
        public void LoadAssembliesFromStorage(params Guid[] uids)
        {
            using (var storage = new ModuleStorage())
            {
                var modulebaseUid = new Guid(Constants.ModuleUid).ToString();
                foreach (var uid in uids)
                {
                    if (!storage.FileExists(modulebaseUid))
                        throw new FileNotFoundException($"{nameof(ModuleLoader)} not found in {nameof(ModuleStorage)}");

                    // test
                    //var version = storage.ExtractVersionInFile(modulebaseUid);

                    if (!storage.FileExists(uid.ToString()))
                        continue;

                    var domain = AppDomain.CreateDomain(uid.ToString());

                    // loadermodule
                    var frameworkBytes = storage.GetFile(new Guid(Constants.ModuleUid).ToString());

                    var loaderType = domain.Load(frameworkBytes).GetType(typeof(ModuleLoader).FullName);

                    var loader = Activator.CreateInstance(loaderType) as ModuleLoader;

                    // module uid
                    var asmRaw = storage.GetFile(uid.ToString());

                    loader.Startup(this, asmRaw);

                    _modules.Add(loader.Uid, new ModuleInfo
                    {
                        Domain = domain,
                        Loader = loader
                    });
                }
            }
        }

        public void LoadModule(Guid uid, object parms)
        {
            if (_modules.ContainsKey(uid))
            {
                var p = _modules[uid].Loader;
                if (!p.IsStarted)
                {
                    p.Load(parms);
                }
            }
        }

        public void UnloadModule(Guid uid, bool performDisposeBefore = false)
        {
            if (_modules.ContainsKey(uid))
            {
                if (performDisposeBefore)
                {
                    var p = _modules[uid].Loader;
                    if (p.IsStarted)
                    {
                        p.Dispose(null);
                    }
                }

                var domain = _modules[uid].Domain;
                AppDomain.Unload(domain);
                _modules.Remove(uid);
            }
        }

        public void DisposeModule(Guid uid, object parms)
        {
            if (_modules.ContainsKey(uid))
            {
                var p = _modules[uid].Loader;
                if (p.IsStarted)
                {
                    p.Dispose(parms);
                }
            }
        }

        public void SendMessagesToModule(Guid uidFrom, Guid uidTo, string name, object messages)
        {
            if (_modules.ContainsKey(uidTo))
            {
                var p = _modules[uidTo].Loader;
                if (p.IsStarted)
                {
                    p.SendMessages(uidFrom, name, messages);
                }
            }
        }

        public bool ContainsModule(Guid uid)
        {
            return _modules.ContainsKey(uid);
        }

        public (Guid Uid, string SessionKey, string FriendlyName, Version Version, bool IsStarted, ModuleType ModuleType, string AssemblyHash)? GetModuleLoadedInfos(Guid uid)
        {
            if (_modules.ContainsKey(uid))
            {
                var loader = _modules[uid].Loader;

                return (loader.Uid, loader.SessionKey, loader.FriendlyName, loader.Version, loader.IsStarted, loader.ModuleType, loader.AssemblyHash);
            }

            return null;
        }

        public (Guid Uid, string SessionKey, string FriendlyName, Version Version, bool IsStarted, ModuleType ModuleType, string AssemblyHash)? GetModuleLoadedInfos(string searchFriendlyName)
        {
            var module = _modules.FirstOrDefault(x => x.Value.Loader.FriendlyName.ToLower().Contains(searchFriendlyName.ToLower()));

            if (module.Key != null && module.Value != null)
            {
                var loader = module.Value.Loader;

                return (loader.Uid, loader.SessionKey, loader.FriendlyName, loader.Version, loader.IsStarted, loader.ModuleType, loader.AssemblyHash);
            }

            return null;
        }

        public (Guid Uid, string SessionKey, string FriendlyName, Version Version, bool IsStarted, ModuleType ModuleType, string AssemblyHash)? GetModuleLoadedInfos(ModuleType moduleType)
        {
            var module = _modules.FirstOrDefault(x => x.Value.Loader.ModuleType == moduleType);

            if (module.Key != null && module.Value != null)
            {
                var loader = module.Value.Loader;

                return (loader.Uid, loader.SessionKey, loader.FriendlyName, loader.Version, loader.IsStarted, loader.ModuleType, loader.AssemblyHash);
            }

            return null;
        }


    }
}
