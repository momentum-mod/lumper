namespace Lumper.Lib.ExtensionMethods;

using System.IO.Enumeration;

public static class ExtensionMethods
{
    public static bool MatchesSimpleExpression(this string str, string expr)
    {
        if (string.IsNullOrEmpty(expr))
            return false;

        // Match to the end of the string if no wildcard is present. Not quite as greedy as string.Contains (`*expr*`),
        // which to match stuff you don't want.
        if (!expr.Contains('*'))
            expr += "*";

        // Algorithm here references a bunch of Windows filename crap but implementation without useExtendedWildcards
        // is exactly what we want.
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/Enumeration/FileSystemName.cs#L82
        return FileSystemName.MatchesSimpleExpression(expr, str);
    }
}
