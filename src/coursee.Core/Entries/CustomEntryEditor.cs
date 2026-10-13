using Coursee.Core.Recents;
using Coursee.Core.Resources;

namespace Coursee.Core.Entries;

/// <summary>
/// Makes and changes custom entries. A changed entry keeps what the user set on it: its pin,
/// alias and hidden state (<see cref="EntryStore.Move"/>), and its recent history (<see cref="RecentStore.Move"/>).
/// </summary>
/// <param name="toResource">The resource of an entry; <see cref="CustomEntry.ToResource"/> by default (tests replace it).</param>
public sealed class CustomEntryEditor(
    CustomEntryStore custom,
    EntryStore entries,
    RecentStore recents,
    Func<CustomEntry, Resource>? toResource = null)
{
    private readonly Func<CustomEntry, Resource> _toResource = toResource ?? (entry => entry.ToResource());

    /// <summary>A custom entry made from typed text (the fallback row): its target is what Enter would act on.</summary>
    /// <exception cref="ArgumentException">The resource is not typed text.</exception>
    public CustomEntry Create(Resource typed, string name)
    {
        if (typed.Source != FallbackClassifier.SourceName)
        {
            throw new ArgumentException("Only typed text makes a custom entry.", nameof(typed));
        }

        return custom.Add(name, typed.Target);
    }

    /// <summary>
    /// Gives <paramref name="entry"/> a new name and target. The target is stored as it is opened
    /// (for example <c>www.example.com</c> becomes <c>https://www.example.com</c>). Returns the new
    /// entry, or null when the entry is gone or another custom entry has that target.
    /// </summary>
    /// <exception cref="ArgumentException">The target is empty.</exception>
    public CustomEntry? Edit(CustomEntry entry, string name, string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("A custom entry needs a target.", nameof(target));
        }

        var opened = _toResource(new CustomEntry(name, target)).Target;
        var before = _toResource(entry);
        if (custom.Update(entry, name, opened) is not { } updated)
        {
            return null;
        }

        var after = _toResource(updated);
        entries.Move(before, after);
        recents.Move(before.Target, after);
        return updated;
    }
}
