using System.Globalization;
using System.Text;

namespace Coursee.Core.Search;

/// <summary>
/// Removes accents so that searching ignores them: "Événements" becomes "Evenements".
/// Letters that are not a base letter plus an accent (such as "ß" or "œ") are kept as they are.
/// </summary>
public static class TextFolding
{
    public static string Fold(string text)
    {
        // Most names, paths and URLs: nothing to do.
        if (Ascii.IsValid(text))
        {
            return text;
        }

        // Split each accented letter into its base letter and its accents, then drop the accents.
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>True when both texts are equal, ignoring case and accents.</summary>
    public static bool AreEqual(string a, string b) =>
        string.Equals(Fold(a), Fold(b), StringComparison.OrdinalIgnoreCase);
}
