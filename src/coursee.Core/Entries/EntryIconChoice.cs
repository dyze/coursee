using Coursee.Core.Icons;

namespace Coursee.Core.Entries;

/// <summary>An icon the user chose, and the target of the entry it was chosen for.</summary>
public sealed record EntryIconChoice(string Target, EntryIcon Icon);
