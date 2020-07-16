using System;

namespace ModuleFramework
{
    public class ModuleInfo
    {
        public AppDomain Domain { get; set; }
        public ModuleLoader Loader { get; set; }
    }
}
