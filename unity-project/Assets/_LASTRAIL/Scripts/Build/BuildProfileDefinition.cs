using System;
using LastRail.Client;

namespace LastRail.Build
{
    public readonly struct BuildProfileDefinition
    {
        public BuildProfileDefinition(string name, ClientEnvironment environment, string environmentDefine, bool developmentBuild, string outputExtension)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Environment = environment;
            EnvironmentDefine = environmentDefine ?? throw new ArgumentNullException(nameof(environmentDefine));
            DevelopmentBuild = developmentBuild;
            OutputExtension = outputExtension ?? throw new ArgumentNullException(nameof(outputExtension));
        }

        public string Name { get; }
        public ClientEnvironment Environment { get; }
        public string EnvironmentDefine { get; }
        public bool DevelopmentBuild { get; }
        public string OutputExtension { get; }
    }
}
