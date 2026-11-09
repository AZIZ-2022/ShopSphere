using System.Globalization;
using System.Text;

namespace ECommerce.Application.Common;

public static class SlugGenerator
{
    /// <summary>Turns arbitrary text into a URL-safe, lowercase, hyphenated slug.</summary>
    public static string Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
            else if (ch is ' ' or '-' or '_' or '/' or '.' or ',')
                sb.Append('-');
        }

        var slug = sb.ToString().Normalize(NormalizationForm.FormC);
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        return slug.Trim('-');
    }
}
