using System.Text.RegularExpressions;

namespace ZModeler3LodManager.Core.Naming;

public static class LodNaming
{
    public static string BuildSuffix(string suffixFormat, int level) =>
        suffixFormat.Replace("{n}", level.ToString());

    public static string BuildLodName(string baseName, LodNamingOptions options, int level) =>
        baseName + BuildSuffix(options.SuffixFormat, level);

    /// <summary>
    /// Strips one trailing "variant marker" (an existing LOD suffix, "_high", ".001", etc.) if
    /// present, repeatedly, so "Bumper_high_L0" and "Bumper" both resolve to the same base name
    /// "Bumper" for grouping purposes.
    /// </summary>
    public static string GetBaseName(string name, LodNamingOptions options)
    {
        var current = name;
        bool changedThisPass;
        do
        {
            changedThisPass = false;
            foreach (var pattern in options.VariantMarkerPatterns)
            {
                var match = Regex.Match(current, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Index > 0)
                {
                    current = current[..match.Index];
                    changedThisPass = true;
                }
            }
        } while (changedThisPass);

        return current.Length > 0 ? current : name;
    }
}
