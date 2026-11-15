using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Umbraco.Community.AIProvenance.UmbracoAI.Proposals;

/// <summary>
/// Turns property values into plain text, so a value an AI proposed can be compared with the value an editor saved.
/// </summary>
/// <remarks>
/// The backoffice and Umbraco reshape some values between the AI's proposal and the save. Rich text gets wrapped in
/// <c>{ markup, blocks }</c> and its markup tidied, and a value inside a block is stored in the block's JSON. The
/// comparison therefore uses the text alone: no tags, decoded entities and collapsed whitespace.
/// </remarks>
internal static partial class ValueText
{
    /// <summary>Normalises a value for comparison. Returns an empty string for an empty value.</summary>
    public static string Normalize(object? value) => value switch
    {
        null => string.Empty,
        JsonElement element => Normalize(element),
        string text => NormalizeString(text),
        _ => NormalizeString(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty),
    };

    /// <summary>
    /// Finds the value of a property inside a block, in the JSON of the property that holds the block.
    /// Returns null when the block or property isn't there.
    /// </summary>
    public static string? FindBlockValue(object? rootValue, Guid elementKey, string propertyAlias)
    {
        if (TryParse(rootValue, out JsonElement root) is false)
        {
            return null;
        }

        JsonElement? element = FindElement(root, elementKey);
        if (element is null || element.Value.TryGetProperty("values", out JsonElement values) is false || values.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement item in values.EnumerateArray())
        {
            if (item.TryGetProperty("alias", out JsonElement alias)
                && string.Equals(alias.GetString(), propertyAlias, StringComparison.OrdinalIgnoreCase)
                && item.TryGetProperty("value", out JsonElement value))
            {
                return Normalize(value);
            }
        }

        return null;
    }

    private static string Normalize(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => NormalizeString(element.GetString() ?? string.Empty),

        // Rich text: compare its markup as text.
        JsonValueKind.Object when element.TryGetProperty("markup", out JsonElement markup) => Normalize(markup),
        JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
        JsonValueKind.Object or JsonValueKind.Array => CollapseWhitespace(element.GetRawText()),
        _ => element.GetRawText(),
    };

    private static string NormalizeString(string text)
    {
        // A stored rich text or block value is a JSON string. Compare what it holds.
        string trimmed = text.TrimStart();
        if ((trimmed.StartsWith('{') || trimmed.StartsWith('[')) && TryParse(text, out JsonElement json))
        {
            return Normalize(json);
        }

        // Block tags separate words. Inline tags, such as <em>, sit inside words and sentences.
        string withoutTags = TagPattern().Replace(BlockTagPattern().Replace(text, " "), string.Empty);
        return CollapseWhitespace(WebUtility.HtmlDecode(withoutTags));
    }

    private static string CollapseWhitespace(string text) => WhitespacePattern().Replace(text, " ").Trim();

    private static bool TryParse(object? value, out JsonElement element)
    {
        element = default;
        string? json = value switch
        {
            JsonElement existing => existing.GetRawText(),
            string text => text,
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            element = JsonDocument.Parse(json).RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Finds the block element with the given key anywhere in a block value, including nested blocks.</summary>
    private static JsonElement? FindElement(JsonElement node, Guid elementKey)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.Object:
                if (node.TryGetProperty("key", out JsonElement key)
                    && Guid.TryParse(key.GetString(), out Guid parsed)
                    && parsed == elementKey
                    && node.TryGetProperty("values", out _))
                {
                    return node;
                }

                foreach (JsonProperty property in node.EnumerateObject())
                {
                    if (FindElement(property.Value, elementKey) is { } found)
                    {
                        return found;
                    }
                }

                break;

            case JsonValueKind.Array:
                foreach (JsonElement item in node.EnumerateArray())
                {
                    if (FindElement(item, elementKey) is { } found)
                    {
                        return found;
                    }
                }

                break;

            // A nested block property stores its own block JSON as a string.
            case JsonValueKind.String when TryParse(node.GetString(), out JsonElement nested) && nested.ValueKind is JsonValueKind.Object or JsonValueKind.Array:
                return FindElement(nested, elementKey);
        }

        return null;
    }

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"</?(p|br|div|li|ul|ol|h[1-6]|blockquote|tr|td|th|table|section|article|hr)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockTagPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
