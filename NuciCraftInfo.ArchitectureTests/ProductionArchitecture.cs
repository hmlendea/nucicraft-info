using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

using Assembly = System.Reflection.Assembly;

namespace NuciCraftInfo.ArchitectureTests
{
    internal static class ProductionArchitecture
    {
        internal static Assembly Assembly { get; } = typeof(Program).Assembly;

        internal static Architecture Model { get; } = new ArchLoader()
            .LoadAssemblies(Assembly)
            .Build();

        internal static IObjectProvider<IType> ProductionTypes { get; } = Types()
            .That()
            .ResideInAssembly(Assembly)
            .As("NuciCraftInfo production types");
    }
}