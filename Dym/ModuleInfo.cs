using System;

namespace Dym
{
    public class ModuleInfo
    {
        public AppDomain Domain { get; set; }
        public ModuleLoader Loader { get; set; }
    }
}
