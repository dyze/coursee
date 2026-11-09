namespace Coursee.Core.Platform;

/// <summary>Controls the <i>Add to coursee</i> item of Explorer's right-click menu, on files and folders.</summary>
public interface IExplorerMenu
{
    /// <summary>What the item starts, compared with this copy of the application.</summary>
    ExplorerMenuStatus Status { get; }

    /// <summary>The program the item starts, or null when there is none.</summary>
    string? RegisteredProgram { get; }

    /// <summary>On: the item starts this copy (replacing any other one). Off: removes it.</summary>
    void SetEnabled(bool enabled);
}
