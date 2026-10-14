namespace Coursee.Core.Resources;

/// <summary>Splits a typed command line into program and arguments, like the Windows Run dialog.</summary>
public static class CommandLine
{
    /// <summary>
    /// A leading double-quoted part is the program (so paths with spaces work when quoted).
    /// Otherwise, with <paramref name="isProgram"/>, the program is the shortest part up to a
    /// space that is an existing program, the whole text included (<c>kill rds.cmd</c> runs that
    /// file, as in Win+R); without it, or when none exists, everything up to the first space.
    /// </summary>
    public static (string FileName, string Arguments) Split(string commandLine, Func<string, bool>? isProgram = null)
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

        if (isProgram is not null && ExistingProgramEnd(text, isProgram) is int programEnd)
        {
            return (text[..programEnd], text[programEnd..].Trim());
        }

        var space = text.IndexOf(' ');
        return space < 0
            ? (text, string.Empty)
            : (text[..space], text[(space + 1)..].Trim());
    }

    /// <summary>Where the first existing program ends: at a space, or at the end of the text. Null when none exists.</summary>
    private static int? ExistingProgramEnd(string text, Func<string, bool> isProgram)
    {
        for (var space = text.IndexOf(' '); space >= 0; space = text.IndexOf(' ', space + 1))
        {
            if (isProgram(text[..space]))
            {
                return space;
            }
        }

        return isProgram(text) ? text.Length : null;
    }
}
