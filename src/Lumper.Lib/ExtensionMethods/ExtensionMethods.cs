namespace Lumper.Lib.ExtensionMethods;

using System.IO.Enumeration;

public static class ExtensionMethods
{
    public static bool MatchesSimpleExpression(this string str, string expr, bool wildcardWrapping)
    {
        if (string.IsNullOrEmpty(expr))
            return false;

        // Match all characters before and after the given string if wildcardWrapping is enabled.
        if (wildcardWrapping && !expr.Contains('*'))
            expr = $"*{expr}*";

        // Algorithm here references a bunch of Windows filename crap but implementation without useExtendedWildcards
        // is exactly what we want.
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/Enumeration/FileSystemName.cs#L82
        return FileSystemName.MatchesSimpleExpression(expr, str);
    }
}
