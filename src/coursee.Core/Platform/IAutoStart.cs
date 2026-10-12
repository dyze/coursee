namespace Coursee.Core.Platform;

/// <summary>Controls whether coursee starts automatically when the user signs in to Windows.</summary>
public interface IAutoStart
{
    /// <summary>True when auto start is on for this copy of the application.</summary>
    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}
