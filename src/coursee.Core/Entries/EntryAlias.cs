namespace Coursee.Core.Entries;

/// <summary>An alias the user gave, and the target of the entry it belongs to.</summary>
public sealed record EntryAlias(string Alias, string Target);
