using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
namespace PatchDesk.Api.Services;

public interface IBuildIdentity
{
    string Commit { get; }
    string AssemblySha256 { get; }
    bool IsClean { get; }
}
public class BuildIdentity : IBuildIdentity
{
    private static readonly Assembly Assembly = typeof(BuildIdentity).Assembly;
    public string Commit { get; } = Regex.Match(Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "", @"\+([0-9a-f]{40})").Groups[1].Value;
    public bool IsClean { get; } = Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Any(m => m.Key == "SourceTreeClean" && m.Value == "true");
    public string AssemblySha256 { get; } = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Assembly.Location)));
}
