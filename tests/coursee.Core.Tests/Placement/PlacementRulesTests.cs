namespace Coursee.Core.Tests.Placement;

public class PlacementRulesTests
{
    // Primary 1920x1080 with a 40 px taskbar at the bottom; a second 1280x1024 monitor on its right.
    private static readonly MonitorInfo Primary = new(@"\\.\DISPLAY1", new ScreenRect(0, 0, 1920, 1040), IsPrimary: true);
    private static readonly MonitorInfo Second = new(@"\\.\DISPLAY2", new ScreenRect(1920, 0, 1280, 984), IsPrimary: false);
    private static readonly MonitorInfo[] Both = [Primary, Second];

    [Fact]
    public void Capture_IsRelativeToTheMonitorShowingTheWindow()
    {
        var placement = PlacementRules.Capture(new ScreenRect(2020, 50, 640, 520), Both);

        Assert.Equal(new WindowPlacement(@"\\.\DISPLAY2", new ScreenRect(100, 50, 640, 520)), placement);
    }

    [Fact]
    public void Capture_AcrossTwoMonitors_TakesTheOneHoldingTheCenter()
    {
        // 1700 + 640 / 2 = 2020: the center is on the second monitor.
        var placement = PlacementRules.Capture(new ScreenRect(1700, 100, 640, 520), Both);

        Assert.Equal(@"\\.\DISPLAY2", placement!.Monitor);
        Assert.Equal(-220, placement.Bounds.Left);
    }

    [Fact]
    public void Capture_CenterOffScreen_TakesTheMonitorItOverlapsMost()
    {
        // Mostly below the second monitor, but its top still overlaps it.
        var placement = PlacementRules.Capture(new ScreenRect(2000, 900, 400, 400), Both);

        Assert.Equal(@"\\.\DISPLAY2", placement!.Monitor);
    }

    [Fact]
    public void Capture_KeepsTheMaximizedFlag()
    {
        Assert.True(PlacementRules.Capture(new ScreenRect(10, 10, 800, 600), Both, maximized: true)!.Maximized);
    }

    [Fact]
    public void Capture_WithoutMonitor_IsNull()
    {
        Assert.Null(PlacementRules.Capture(new ScreenRect(0, 0, 100, 100), []));
    }

    [Fact]
    public void Fit_SameMonitor_GivesTheSavedPositionBack()
    {
        var placement = new WindowPlacement(@"\\.\DISPLAY2", new ScreenRect(100, 50, 640, 520));

        Assert.Equal(new ScreenRect(2020, 50, 640, 520), PlacementRules.Fit(placement, Both));
    }

    [Fact]
    public void Fit_FollowsAMonitorThatWasRearranged()
    {
        var movedLeft = Second with { WorkArea = new ScreenRect(-1280, 0, 1280, 984) };
        var placement = new WindowPlacement(@"\\.\DISPLAY2", new ScreenRect(100, 50, 640, 520));

        Assert.Equal(new ScreenRect(-1180, 50, 640, 520), PlacementRules.Fit(placement, [Primary, movedLeft]));
    }

    [Fact]
    public void Fit_MonitorGone_UsesTheSamePlaceOnThePrimary()
    {
        var placement = new WindowPlacement(@"\\.\DISPLAY2", new ScreenRect(100, 50, 640, 520));

        Assert.Equal(new ScreenRect(100, 50, 640, 520), PlacementRules.Fit(placement, [Primary]));
    }

    [Fact]
    public void Fit_MovesAWindowBackInsideTheWorkArea()
    {
        // Saved near the right edge of a larger screen: it must not hang off the smaller one.
        var placement = new WindowPlacement(@"\\.\DISPLAY2", new ScreenRect(1200, 900, 640, 520));

        Assert.Equal(new ScreenRect(2560, 464, 640, 520), PlacementRules.Fit(placement, Both));
    }

    [Fact]
    public void Fit_MovesAWindowBackFromAboveAndLeft()
    {
        var placement = new WindowPlacement(@"\\.\DISPLAY1", new ScreenRect(-300, -50, 640, 520));

        Assert.Equal(new ScreenRect(0, 0, 640, 520), PlacementRules.Fit(placement, Both));
    }

    [Fact]
    public void Fit_ShrinksAWindowLargerThanTheWorkArea()
    {
        var placement = new WindowPlacement(@"\\.\DISPLAY2", new ScreenRect(0, 0, 1900, 1100));

        Assert.Equal(new ScreenRect(1920, 0, 1280, 984), PlacementRules.Fit(placement, Both));
    }

    [Fact]
    public void Fit_MonitorNamesIgnoreCase()
    {
        var placement = new WindowPlacement(@"\\.\display2", new ScreenRect(0, 0, 100, 100));

        Assert.Equal(1920, PlacementRules.Fit(placement, Both)!.Left);
    }

    [Fact]
    public void Fit_WithoutMonitor_IsNull()
    {
        Assert.Null(PlacementRules.Fit(new WindowPlacement("x", new ScreenRect(0, 0, 100, 100)), []));
    }

    [Fact]
    public void KeepVisible_LeavesAVisibleWindowAlone()
    {
        var bounds = new ScreenRect(300, 200, 640, 520);

        Assert.Equal(bounds, PlacementRules.KeepVisible(bounds, Both));
    }

    [Fact]
    public void KeepVisible_PullsBackAWindowPastTheRightAndBottomEdges()
    {
        // Dragged into the bottom-right corner of the primary monitor, partly over the taskbar.
        var bounds = new ScreenRect(1500, 800, 640, 520);

        Assert.Equal(new ScreenRect(1280, 520, 640, 520), PlacementRules.KeepVisible(bounds, [Primary]));
    }

    [Fact]
    public void KeepVisible_KeepsTheWindowOnTheMonitorShowingMostOfIt()
    {
        // Center on the second monitor, top above the screens.
        var bounds = new ScreenRect(2000, -100, 640, 520);

        Assert.Equal(new ScreenRect(2000, 0, 640, 520), PlacementRules.KeepVisible(bounds, Both));
    }

    [Fact]
    public void KeepVisible_WithoutMonitor_IsNull()
    {
        Assert.Null(PlacementRules.KeepVisible(new ScreenRect(0, 0, 100, 100), []));
    }

    [Fact]
    public void CenterOn_PutsTheDialogInTheMiddleOfItsOwner()
    {
        Assert.Equal(new ScreenRect(140, 140, 560, 440), PlacementRules.CenterOn(new ScreenRect(100, 100, 640, 520), 560, 440));
    }

    [Fact]
    public void CenterOn_ALargerDialogOverlapsItsOwnerEvenly()
    {
        Assert.Equal(new ScreenRect(50, 50, 300, 300), PlacementRules.CenterOn(new ScreenRect(100, 100, 200, 200), 300, 300));
    }

    [Fact]
    public void CaptureThenFit_GivesTheSameRectangle()
    {
        var bounds = new ScreenRect(300, 200, 900, 500);

        Assert.Equal(bounds, PlacementRules.Fit(PlacementRules.Capture(bounds, Both)!, Both));
    }

    [Fact]
    public void MonitorAt_IsTheMonitorUnderThePoint()
    {
        Assert.Equal(Second, PlacementRules.MonitorAt(new ScreenPoint(2500, 300), Both));
        Assert.Equal(Primary, PlacementRules.MonitorAt(new ScreenPoint(10, 10), Both));
    }

    [Fact]
    public void MonitorAt_OnATaskbar_IsTheNearestMonitor()
    {
        // The primary taskbar: below the primary work area (1040 px high).
        Assert.Equal(Primary, PlacementRules.MonitorAt(new ScreenPoint(500, 1060), Both));
        Assert.Equal(Second, PlacementRules.MonitorAt(new ScreenPoint(2500, 1000), Both));
    }

    [Fact]
    public void MonitorAt_NoMonitor_IsNull() =>
        Assert.Null(PlacementRules.MonitorAt(new ScreenPoint(0, 0), []));

    [Fact]
    public void CenterOnMonitorAt_IsTheMiddleOfTheMonitorUnderThePointer()
    {
        Assert.Equal(new ScreenRect(640, 260, 640, 520), PlacementRules.CenterOnMonitorAt(new ScreenPoint(5, 5), 640, 520, Both));
        Assert.Equal(new ScreenRect(2240, 232, 640, 520), PlacementRules.CenterOnMonitorAt(new ScreenPoint(3000, 900), 640, 520, Both));
    }

    [Fact]
    public void CenterOnMonitorAt_LargerThanTheMonitor_IsShrunk() =>
        Assert.Equal(new ScreenRect(1920, 0, 1280, 984), PlacementRules.CenterOnMonitorAt(new ScreenPoint(2500, 300), 2000, 1200, Both));

    [Fact]
    public void CenterOnMonitorAt_NoMonitor_IsNull() =>
        Assert.Null(PlacementRules.CenterOnMonitorAt(new ScreenPoint(0, 0), 640, 520, []));

    [Fact]
    public void CenterOnPoint_IsCenteredOnThePointer() =>
        Assert.Equal(new ScreenRect(680, 240, 640, 520), PlacementRules.CenterOnPoint(new ScreenPoint(1000, 500), 640, 520, Both));

    [Fact]
    public void CenterOnPoint_NearAnEdge_StaysOnTheMonitorOfThePointer()
    {
        // Near the top-left corner of the primary monitor.
        Assert.Equal(new ScreenRect(0, 0, 640, 520), PlacementRules.CenterOnPoint(new ScreenPoint(20, 20), 640, 520, Both));

        // Near the edge between both monitors: kept whole on the primary one, not across.
        Assert.Equal(new ScreenRect(1280, 300, 640, 520), PlacementRules.CenterOnPoint(new ScreenPoint(1900, 560), 640, 520, Both));

        // Bottom right of the second monitor.
        Assert.Equal(new ScreenRect(2560, 464, 640, 520), PlacementRules.CenterOnPoint(new ScreenPoint(3190, 980), 640, 520, Both));
    }

    [Fact]
    public void CenterOnPoint_NoMonitor_IsNull() =>
        Assert.Null(PlacementRules.CenterOnPoint(new ScreenPoint(0, 0), 640, 520, []));
}
