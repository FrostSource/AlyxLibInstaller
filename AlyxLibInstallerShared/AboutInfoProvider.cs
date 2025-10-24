using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared;
public static class AboutInfoProvider
{
    //private static string GetMetadata(Assembly asm,string key, string defaultValue = "") => asm.GetCustomAttributes<AssemblyMetadataAttribute>()
    //                                                                .FirstOrDefault(a => a.Key == key)?.Value
    //                                                                ?? defaultValue;

    public static AboutInfo Generate()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        string GetMetadata(string key) =>
            asm.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key)?.Value ?? $"Add {key} metadata to project file";

        var title = asm.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
            ?? asm.GetName().Name
            ?? "Add <Title> to project file";

        var version = asm.GetName().Version?.ToString() ?? "?.?.?";

        var description = asm.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "Add <Description> to project file";

        var company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company
            ?? "Add <Company> to project file";

        string[] builtwith = asm
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => a.Key == "BuiltWith" && a.Value is not null)
            .Select(a => a.Value!)
            .ToArray();


        return new AboutInfo
        {
            AppName = title,
            Version = version,
            Description = description,
            Author = GetMetadata("Authors"),
            Copyright = $"© {DateTime.Now.Year} {company}",
            Website = GetMetadata("RepositoryUrl"),
            License = GetMetadata("License"),
            BuiltWith = builtwith
        };

    }
}
