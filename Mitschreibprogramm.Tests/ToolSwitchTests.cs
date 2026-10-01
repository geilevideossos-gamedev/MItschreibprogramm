using Mitschreibprogramm.Models;
using Mitschreibprogramm.Services;

namespace Mitschreibprogramm.Tests;

public sealed class ToolSwitchTests
{
    [Fact]
    public void StartsWithThePen() => Assert.Equal(Tool.Pen, new ToolSwitch().Current);

    [Theory]
    [InlineData(Tool.Pen)]
    [InlineData(Tool.Eraser)]
    public void SelectionFromAnotherTool_SwitchesToSelectAndBack(Tool tool)
    {
        var tools = new ToolSwitch();
        tools.Choose(tool);

        tools.SelectionStarted();
        Assert.Equal(Tool.Select, tools.Current);

        tools.SelectionEnded();
        Assert.Equal(tool, tools.Current);
    }

    [Fact]
    public void SelectionWithTheSelectTool_KeepsSelect()
    {
        var tools = new ToolSwitch();
        tools.Choose(Tool.Select);

        tools.SelectionStarted();
        tools.SelectionEnded();

        Assert.Equal(Tool.Select, tools.Current);
    }

    [Fact]
    public void SecondSelection_StillReturnsToTheFirstTool()
    {
        var tools = new ToolSwitch();
        tools.Choose(Tool.Eraser);

        tools.SelectionStarted();
        tools.SelectionStarted();
        tools.SelectionEnded();

        Assert.Equal(Tool.Eraser, tools.Current);
    }

    [Fact]
    public void ToolChosenDuringTheSelection_Stays()
    {
        var tools = new ToolSwitch();
        tools.SelectionStarted();

        tools.Choose(Tool.Eraser);
        tools.SelectionEnded();

        Assert.Equal(Tool.Eraser, tools.Current);
    }

    [Fact]
    public void SelectionEndedWithoutStart_ChangesNothing()
    {
        var tools = new ToolSwitch();
        tools.Choose(Tool.Eraser);

        tools.SelectionEnded();

        Assert.Equal(Tool.Eraser, tools.Current);
    }
}
