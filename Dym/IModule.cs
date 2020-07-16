using System;

namespace ModuleFramework
{
    public interface IModule
    {
        /// <summary>
        /// Set module type
        /// </summary>
        ModuleType ModuleType { get; }
        /// <summary>
        /// Return instance session key of module
        /// </summary>
        string SessionKey {get;}
        /// <summary>
        /// Return unique id defined in module
        /// </summary>
        byte[] Uid { get; }
        /// <summary>
        /// Return friendly name defined in module
        /// </summary>        
        string FriendlyName { get; }
        /// <summary>
        /// Return version defined in module
        /// </summary>        
        Version Version { get; }
        /// <summary>
        /// Perform boot of module
        /// </summary>
        /// <param name="moduleHost"></param>
        /// <param name="ownHash"></param>
        void Startup(IModuleHost moduleHost, string ownHash);        
        /// <summary>
        /// Start module with params (param1:value1, param2:value2...)
        /// </summary>
        /// <param name="parms">param1:value1, param2:value2</param>
        void Load(params string[] parms);        
        /// <summary>
        /// Perform disposable operations inside module before unloading (param1:value1, param2:value2...)
        /// </summary>
        /// <param name="parms">param1:value1, param2:value2</param>
        void Dispose(params string[] parms);
        /// <summary>
        /// Recover Messages of other modules
        /// </summary>
        /// <param name="uidFrom"></param>
        /// <param name="name"></param>
        /// <param name="messages"></param>
        void MessageCallback(Guid uidFrom, string name, params string[] messages);        
    }
}
