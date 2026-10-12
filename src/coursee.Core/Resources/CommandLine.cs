namespace Coursee.Core.Resources;

/// <summary>Splits a typed command line into program and arguments, like the Windows Run dialog.</summary>
public static class CommandLine
{
    /// <summary>
    /// A leading double-quoted part is the program (so paths with spaces work when quoted);
    /// otherwise the program is everything up to the first space.
    /// </summary>
    public static (string FileName, string Arguments) Split(string commandLine)
    {
        var text = commandLine.Trim();
        if (text.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        if (text[0] == '"')
        {
            var end = text.IndexOf('"', 1);
            return end > 0
                ? (text[1..end], text[(end + 1)..].Trim())
                : (text[1..], string.Empty);
        }

        var space = text.IndexOf(' ');
        return space < 0
            ? (text, string.Empty)
            : (text[..space], text[(space + 1)..].Trim());
    }
}
