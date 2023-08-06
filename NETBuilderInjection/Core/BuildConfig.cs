using System;

namespace NETBuilderInjection.Core
{
    [AttributeUsage(AttributeTargets.Method)]
    public class InjectionEntryPoint : Attribute
    {
        public bool CreateThread { get; set; } = true;
        public string BuildTarget { get; set; } = ".dll";
        public bool MergeLibs { get; set; } = false;
        public bool ILoader { get; set; } = false;
        public string ProtectionRules { get; set; } = string.Empty;
        public string PreCompiler { get; set; } = string.Empty;
        public string CloneTo { get; set; } = string.Empty;
        public string ILoaderProtectionRules { get; set; } = string.Empty;
        public bool BasicILoaderProtection { get; set; } = false;
    }
}
