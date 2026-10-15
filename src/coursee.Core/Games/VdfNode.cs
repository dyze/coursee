using System.Text;

namespace Coursee.Core.Games;

/// <summary>
/// A node of Valve's text format (VDF / KeyValues), used by Steam's <c>libraryfolders.vdf</c> and
/// <c>appmanifest_*.acf</c>: a quoted key followed by a quoted value, or by <c>{ ... }</c> holding
/// more keys. Keys ignore case (Steam writes <c>LibraryFolders</c> or <c>libraryfolders</c>); on a
/// repeated key, the first one is kept.
/// </summary>
public sealed class VdfNode
{
    private readonly Dictionary<string, VdfNode> _children = new(StringComparer.OrdinalIgnoreCase);

    private VdfNode(string? value) => Value = value;

    /// <summary>The text of a key-value pair; null for a node holding keys.</summary>
    public string? Value { get; }

    /// <summary>The keys of a node, in file order.</summary>
    public IReadOnlyDictionary<string, VdfNode> Children => _children;

    /// <summary>The child with that key, or null.</summary>
    public VdfNode? this[string key] => _children.GetValueOrDefault(key);

    /// <summary>The text of the child with that key, or null.</summary>
    public string? ValueOf(string key) => this[key]?.Value;

    /// <summary>The whole file, as a node holding its top-level keys. Throws <see cref="FormatException"/> when it is malformed.</summary>
    public static VdfNode Parse(string text)
    {
        var reader = new Reader(text);
        var root = new VdfNode(null);
        reader.ReadChildren(root, topLevel: true);
        return root;
    }

    private sealed class Reader(string text)
    {
        private int _position;

        public void ReadChildren(VdfNode parent, bool topLevel)
        {
            while (true)
            {
                var key = NextToken();
                if (key is null)
                {
                    if (!topLevel)
                    {
                        throw new FormatException("A '{' is not closed");
                    }

                    return;
                }

                if (key == Close)
                {
                    if (topLevel)
                    {
                        throw new FormatException("A '}' has no matching '{'");
                    }

                    return;
                }

                if (key == Open)
                {
                    throw new FormatException("A '{' has no key");
                }

                var value = NextToken() ?? throw new FormatException($"The key \"{key.Text}\" has no value");
                VdfNode child;
                if (value == Open)
                {
                    child = new VdfNode(null);
                    ReadChildren(child, topLevel: false);
                }
                else if (value == Close)
                {
                    throw new FormatException($"The key \"{key.Text}\" has no value");
                }
                else
                {
                    child = new VdfNode(value.Text);
                }

                parent._children.TryAdd(key.Text, child);
            }
        }

        private static readonly Token Open = new("{");
        private static readonly Token Close = new("}");

        /// <summary>A quoted or bare string, a brace, or null at the end of the text.</summary>
        private Token? NextToken()
        {
            SkipSpaceAndComments();
            if (_position >= text.Length)
            {
                return null;
            }

            var current = text[_position];
            if (current == '{')
            {
                _position++;
                return Open;
            }

            if (current == '}')
            {
                _position++;
                return Close;
            }

            return current == '"' ? Quoted() : Bare();
        }

        private void SkipSpaceAndComments()
        {
            while (_position < text.Length)
            {
                if (char.IsWhiteSpace(text[_position]))
                {
                    _position++;
                }
                else if (text[_position] == '/' && _position + 1 < text.Length && text[_position + 1] == '/')
                {
                    var end = text.IndexOf('\n', _position);
                    _position = end < 0 ? text.Length : end + 1;
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>A string in double quotes, with the escapes Steam writes: <c>\\</c>, <c>\"</c>, <c>\n</c>, <c>\t</c>.</summary>
        private Token Quoted()
        {
            var builder = new StringBuilder();
            _position++;
            while (_position < text.Length)
            {
                var current = text[_position++];
                if (current == '"')
                {
                    return new Token(builder.ToString());
                }

                if (current == '\\' && _position < text.Length)
                {
                    var escaped = text[_position++];
                    builder.Append(escaped switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        _ => escaped,
                    });
                }
                else
                {
                    builder.Append(current);
                }
            }

            throw new FormatException("A quoted string is not closed");
        }

        private Token Bare()
        {
            var start = _position;
            while (_position < text.Length && !char.IsWhiteSpace(text[_position]) && text[_position] is not ('{' or '}' or '"'))
            {
                _position++;
            }

            return new Token(text[start.._position]);
        }
    }

    /// <summary>Braces are compared by reference, so a quoted "{" stays a string.</summary>
    private sealed class Token(string text)
    {
        public string Text { get; } = text;
    }
}
