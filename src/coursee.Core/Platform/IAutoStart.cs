namespace Coursee.Core.Platform;

/// <summary>Controls whether coursee starts automatically when the user signs in to Windows.</summary>
public interface IAutoStart
{
    /// <summary>What the auto start entry does, compared with this copy of the application.</summary>
    AutoStartStatus Status { get; }

    /// <summary>The program the auto start entry starts, or null when there is none.</summary>
    string? RegisteredProgram { get; }

    /// <summary>On: the entry starts this copy, hidden (replacing any other entry). Off: removes it.</summary>
    void SetEnabled(bool enabled);
}
