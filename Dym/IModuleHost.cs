using System;

namespace Dym
{
    public interface IModuleHost
    {
        /// <summary>
        /// Modules Uids
        /// </summary>
        Guid[] ModulesUid { get; }
        /// <summary>
        /// Load Assembly by bytes
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="mbytes"></param>        
        void LoadAssembly(Guid uid, byte[] mbytes);
        /// <summary>
        /// Load Assembly´s by full path´s
        /// </summary>
        /// <param name="paths">assembly´s with full path</param>
        void LoadAssemblies(params string[] paths);
        /// <summary>
        /// Load Assembly´s by uid´s
        /// </summary>
        /// <param name="uids"></param>
        void LoadAssembliesFromStorage(params Guid[] uids);
        /// <summary>
        /// Start module
        /// </summary>
        /// <param name="uid">unique id</param>
        /// <param name="parms">custom params</param>
        void LoadModule(Guid uid, object parms = null);
        /// <summary>
        /// Unload module with (optinal) <see cref="DisposeModule(Guid)"/>
        /// </summary>
        /// <param name="uid">unique id</param>
        /// <param name="performDisposeBefore">perform disposable method before?</param>
        void UnloadModule(Guid uid, bool performDisposeBefore = false);
        /// <summary>
        /// Perform disposable operations inside module before unloading on host
        /// </summary>
        /// <param name="uid">unique id</param>
        /// <param name="parms">custom params</param>
        void DisposeModule(Guid uid, object parms);
        /// <summary>
        /// Send messages to modules through host
        /// </summary>
        /// <param name="uidFrom">unique id of origem</param>
        /// <param name="uidTo">unique id of destination</param>
        /// <param name="name">name of message</param>
        /// <param name="message">an or many messages</param>
        void SendMessagesToModule(Guid uidFrom, Guid uidTo, string name, object message);
        /// <summary>
        /// Returns if module exists on host
        /// </summary>
        /// <param name="uid">unique id</param>
        /// <returns></returns>
        bool ContainsModule(Guid uid);
        /// <summary>
        /// Returns loaded module information 
        /// (<see cref="ModuleLoader.Uid"/>, 
        /// <see cref="ModuleLoader.SessionKey"/>, 
        /// <see cref="ModuleLoader.FriendlyName"/>, 
        /// <see cref="ModuleLoader.Version"/>, 
        /// <see cref="ModuleLoader.IsStarted"/>, 
        /// <see cref="ModuleLoader.ModuleType"/>, 
        /// <see cref="ModuleLoader.AssemblyHash"/>)
        /// </summary>
        /// <param name="uid">unique id</param>
        /// <returns></returns>
        (Guid Uid, string SessionKey, string FriendlyName, Version Version, bool IsStarted, ModuleType ModuleType, string AssemblyHash)? GetModuleLoadedInfos(Guid uid);
        /// <summary>
        /// Returns loaded module information by friendlyName
        /// (<see cref="ModuleLoader.Uid"/>, 
        /// <see cref="ModuleLoader.SessionKey"/>, 
        /// <see cref="ModuleLoader.FriendlyName"/>, 
        /// <see cref="ModuleLoader.Version"/>, 
        /// <see cref="ModuleLoader.IsStarted"/>, 
        /// <see cref="ModuleLoader.ModuleType"/>, 
        /// <see cref="ModuleLoader.AssemblyHash"/>)
        /// </summary>
        /// <param name="searchFriendlyName">unique id</param>
        /// <returns></returns>
        (Guid Uid, string SessionKey, string FriendlyName, Version Version, bool IsStarted, ModuleType ModuleType, string AssemblyHash)? GetModuleLoadedInfos(string searchFriendlyName);
        /// <summary>
        /// Returns loaded module information by <see cref="ModuleType"/>
        /// (<see cref="ModuleLoader.Uid"/>, 
        /// <see cref="ModuleLoader.SessionKey"/>, 
        /// <see cref="ModuleLoader.FriendlyName"/>, 
        /// <see cref="ModuleLoader.Version"/>, 
        /// <see cref="ModuleLoader.IsStarted"/>, 
        /// <see cref="ModuleLoader.ModuleType"/>, 
        /// <see cref="ModuleLoader.AssemblyHash"/>)
        /// </summary>
        /// <param name="moduleType"></param>
        /// <returns></returns>
        (Guid Uid, string SessionKey, string FriendlyName, Version Version, bool IsStarted, ModuleType ModuleType, string AssemblyHash)? GetModuleLoadedInfos(ModuleType moduleType);
    }
}
