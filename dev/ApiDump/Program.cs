using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;

if (args.Length != 1) throw new ArgumentException("Usage: ApiDump <actual-server-root>");
var root = Path.GetFullPath(args[0]);
var dirs = new[] { Path.Combine(root, "Unturned_Headless_Data/Managed"), Path.Combine(root, "Modules/Rocket.Unturned") };
AssemblyLoadContext.Default.Resolving += (context, name) => {
    foreach (var dir in dirs) {
        var path = Path.Combine(dir, name.Name + ".dll");
        if (File.Exists(path)) return context.LoadFromAssemblyPath(path);
    }
    return null;
};
var game = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dirs[0], "Assembly-CSharp.dll"));
var rocket = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dirs[1], "Rocket.Unturned.dll"));
foreach (var assembly in new[] { game, rocket }) {
    Console.WriteLine($"ASSEMBLY {assembly.FullName} PATH {assembly.Location} SHA256 {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location)))}");
}
var types = new Dictionary<string, string[]> {
    ["SDG.Unturned.DamageTool"] = new[] { "playerDamaged" },
    ["SDG.Unturned.BarricadeManager"] = new[] { "onDamageBarricadeRequested", "onDeployBarricadeRequested", "regions", "vehicleRegions" },
    ["SDG.Unturned.StructureManager"] = new[] { "onDamageStructureRequested", "onDeployStructureRequested", "regions" },
    ["SDG.Unturned.BarricadeDrop"] = new[] { "GetServersideData", "asset" },
    ["SDG.Unturned.StructureDrop"] = new[] { "GetServersideData" },
    ["SDG.Unturned.EffectManager"] = new[] { "onEffectButtonClicked", "sendUIEffect", "sendUIEffectText", "sendUIEffectVisibility", "askEffectClearByID" },
    ["Rocket.Unturned.Events.UnturnedPlayerEvents"] = new[] { "OnPlayerRevive", "OnPlayerDeath", "OnPlayerUpdateStat" },
    ["SDG.Unturned.VehicleManager"] = new[] { "onEnterVehicleRequested", "onDamageVehicleRequested" },
    ["SDG.Unturned.LevelManager"] = new[] { "airdrop" },
    ["SDG.Unturned.SteamBlacklist"] = Array.Empty<string>(),
    ["Rocket.Unturned.Player.UnturnedPlayer"] = new[] { "Ban", "Kick", "CurrentVehicle" },
    ["SDG.Unturned.Player"] = new[] { "teleportToLocation" },
    ["SDG.Unturned.EBuild"] = Array.Empty<string>()
};
foreach (var (name, wanted) in types) {
    var type = game.GetType(name) ?? rocket.GetType(name) ?? throw new TypeLoadException(name);
    Console.WriteLine("TYPE " + type.FullName);
    if (type.IsEnum) { Console.WriteLine(string.Join(", ", Enum.GetNames(type))); continue; }
    foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => wanted.Length == 0 || wanted.Contains(m.Name))) {
        Console.WriteLine("  " + member);
        if (member is MethodInfo method) Console.WriteLine("    PARAMS " + string.Join(", ", method.GetParameters().Select(p => p.ParameterType + " " + p.Name)));
        if (member is FieldInfo constant && constant.IsLiteral) Console.WriteLine("    CONSTANT " + constant.GetRawConstantValue());
        var delegateType = member is EventInfo e ? e.EventHandlerType : member is FieldInfo f ? f.FieldType : null;
        if (delegateType != null && typeof(Delegate).IsAssignableFrom(delegateType)) Console.WriteLine("    DELEGATE " + delegateType.GetMethod("Invoke"));
    }
}

var api = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dirs[1], "Rocket.API.dll"));
foreach (var assembly in new[] { api, AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(dirs[1], "Rocket.Core.dll")) })
foreach (var type in assembly.GetTypes())
foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == "HasPermission"))
    Console.WriteLine($"PERMISSION {type.FullName} {method}");
