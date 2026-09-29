using System.Windows;
using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Tests;

public sealed class SelectionBoundsTests
{
    private static readonly Size A4 = new(AppConstants.PageWidth, AppConstants.PageHeight);
    private static readonly Rect Picture = new(100, 100, 400, 300);

    [Fact]
    public void KeepAspect_DraggingTheBottomRightCornerKeepsTheTopLeft()
    {
        var result = SelectionBounds.KeepAspect(Picture, new Rect(100, 100, 600, 350));

        Assert.Equal(new Rect(100, 100, 600, 450), result);
    }

    [Fact]
    public void KeepAspect_DraggingTheTopLeftCornerKeepsTheBottomRight()
    {
        var result = SelectionBounds.KeepAspect(Picture, new Rect(300, 160, 200, 240));

        Assert.Equal(new Rect(300, 250, 200, 150), result);
    }

    [Fact]
    public void KeepAspect_DraggingOneSideScalesTheOtherOneAlong()
    {
        var result = SelectionBounds.KeepAspect(Picture, new Rect(100, 100, 200, 300));

        Assert.Equal(new Rect(100, 100, 200, 150), result);
    }

    [Fact]
    public void KeepOnPage_PullsAMovedSelectionBackOntoThePage()
    {
        Assert.Equal(new Rect(0, 20, 300, 200), SelectionBounds.KeepOnPage(new Rect(-50, 20, 300, 200), A4, endless: false));
        Assert.Equal(new Rect(A4.Width - 300, A4.Height - 200, 300, 200), SelectionBounds.KeepOnPage(new Rect(700, 1100, 300, 200), A4, endless: false));
    }

    [Fact]
    public void KeepOnPage_ShrinksASelectionThatGrewLargerThanThePage()
    {
        var result = SelectionBounds.KeepOnPage(new Rect(0, 0, 2 * A4.Width, A4.Height), A4, endless: false);

        Assert.Equal(A4.Width, result.Width, 6);
        Assert.Equal(A4.Height / 2, result.Height, 6);
    }

    [Fact]
    public void KeepOnPage_OnTheEndlessSurfaceOnlyTheTopAndLeftEdgesCount()
    {
        Assert.Equal(new Rect(0, 0, 300, 200), SelectionBounds.KeepOnPage(new Rect(-10, -20, 300, 200), A4, endless: true));
        Assert.Equal(new Rect(5000, 9000, 300, 200), SelectionBounds.KeepOnPage(new Rect(5000, 9000, 300, 200), A4, endless: true));
    }
}
