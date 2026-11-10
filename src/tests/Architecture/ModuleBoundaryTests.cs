using ArchUnitNET.Loader;
using Type = System.Type;
using ArchUnitNET.xUnit;
using OtaiDesk.Modules.Tickets;
using OtaiDesk.Shared;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using static ArchUnitNET.Fluent.Slices.SliceRuleDefinition;

namespace OtaiDesk.Tests.Architecture;

public sealed class ModuleBoundaryTests
{
    private const string ModuleCode = @"^(OtaiDesk\.Modules\.|AspNetCoreGeneratedDocument\.Modules_)";
    private const string ModuleRegistration = @"^(Program|OtaiDesk\.OtaiDeskApp)([+/]|$)";
    private const string FrameworkOrLegacy = @"^(Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore|OtaiDesk\.(?!Domain(\.|$)|Modules\.Tickets\.Domain(\.|$)))";
    private const string DatabaseAccess = @"^(Microsoft\.EntityFrameworkCore|OtaiDesk\.Data|OtaiDesk\.Modules\.Tickets\.Infrastructure)(\.|$)";

    private static readonly System.Reflection.Assembly App = typeof(TicketsModule).Assembly;
    private static readonly ArchUnitNET.Domain.Architecture Desk = new ArchLoader().LoadAssemblies(App).Build();

    public static TheoryData<string> ModuleNames()
    {
        var names = new TheoryData<string>();
        foreach (var name in DeskModules().Select(ModuleName).Distinct())
        {
            names.Add(name);
        }
        return names;
    }

    [Fact]
    public void Each_module_has_exactly_one_desk_module()
    {
        var byModule = DeskModules().GroupBy(ModuleName).ToDictionary(group => group.Key, group => group.Count());
        Assert.Contains("Tickets", byModule.Keys);
        Assert.All(byModule, pair => Assert.Equal(1, pair.Value));
        Assert.All(DeskModules(), module => Assert.Equal($"OtaiDesk.Modules.{ModuleName(module)}", module.Namespace));
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void A_module_reaches_another_module_only_through_its_contracts(string module)
    {
        var own = $@"^OtaiDesk\.Modules\.{module}(\.|$)";
        var otherInternals = $@"^OtaiDesk\.Modules\.(?!{module}(\.|$))(?>[^.]+)(?!\.Contracts(\.|$))";
        Types().That().ResideInNamespaceMatching(own)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(otherInternals))
            .Because("LD-099 module rule 5")
            .Check(Desk);
    }

    [Fact]
    public void Module_dependencies_have_no_cycles()
    {
        Slices().Matching("OtaiDesk.Modules.(*)..").Should().BeFreeOfCycles().Check(Desk);
    }

    [Fact]
    public void Code_outside_modules_does_not_reference_a_module()
    {
        Types().That().DoNotHaveFullNameMatching(ModuleCode).And().DoNotHaveFullNameMatching(ModuleRegistration)
            .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(@"^OtaiDesk\.Modules(\.|$)"))
            .Because("LD-099 module rule 7")
            .Check(Desk);
    }

    [Fact]
    public void Ticket_domain_has_no_framework_or_legacy_imports()
    {
        Types().That().ResideInNamespaceMatching(@"^OtaiDesk\.Modules\.Tickets\.Domain(\.|$)")
            .Should().NotDependOnAny(Types(true).That().ResideInNamespaceMatching(FrameworkOrLegacy))
            .Because("LD-099 ticket model rule 2")
            .Check(Desk);
    }

    [Fact]
    public void Ticket_pages_and_api_do_not_touch_the_database()
    {
        Types().That().ResideInNamespaceMatching(@"^OtaiDesk\.Modules\.Tickets\.(Pages|Api)(\.|$)")
            .Should().NotDependOnAny(Types(true).That().ResideInNamespaceMatching(DatabaseAccess))
            .Because("LD-099 ticket model rule 1")
            .Check(Desk);
    }

    [Fact]
    public void The_forbidden_dependency_rule_catches_a_real_dependency()
    {
        var rule = Types().That().ResideInNamespace("OtaiDesk.Data")
            .Should().NotDependOnAny(Types(true).That().ResideInNamespaceMatching(@"^Microsoft\.EntityFrameworkCore(\.|$)"));
        Assert.False(rule.HasNoViolations(Desk));
    }

    private static IEnumerable<Type> DeskModules() =>
        App.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IDeskModule).IsAssignableFrom(type));

    private static string ModuleName(Type module)
    {
        var space = module.Namespace ?? throw new InvalidOperationException($"{module.Name} has no namespace");
        var parts = space.Split('.');
        Assert.True(parts.Length >= 3 && parts[0] == "OtaiDesk" && parts[1] == "Modules", $"{module.FullName} is not under OtaiDesk.Modules");
        return parts[2];
    }
}
