using CryptoTrader.Domain.Entities;
using CryptoTrader.Domain.Enums;
using Xunit;

namespace CryptoTrader.Domain.Tests.Entities;

public class OpenPositionStateTests
{
    [Fact]
    public void TryAdvanceStopLoss_TP2Hit_MovesSLToEntry()
    {
        // Arrange
        var state = new OpenPositionState
        {
            EntryPrice = 50000,
            InitialStopLoss = 51000,
            CurrentStopLoss = 51000,
            TakeProfits = new List<TakeProfitState>
            {
                new() { Sequence = 1, TargetPrice = 49000, IsTriggered = false },
                new() { Sequence = 2, TargetPrice = 48000, IsTriggered = false },
                new() { Sequence = 3, TargetPrice = 47000, IsTriggered = false }
            }
        };

        // Act
        state.TryAdvanceStopLoss(49000, DateTime.UtcNow, out _, out _);
        bool result = state.TryAdvanceStopLoss(48000, DateTime.UtcNow, out int seq, out decimal? newSL);

        // Assert
        Assert.True(result);
        Assert.Equal(2, seq);
        Assert.Equal(50000, newSL);
        Assert.Equal(50000, state.CurrentStopLoss);
    }

    [Fact]
    public void TryAdvanceStopLoss_TP3Hit_MovesSLToTP2()
    {
        // Arrange
        var state = new OpenPositionState
        {
            EntryPrice = 50000,
            TakeProfits = new List<TakeProfitState>
            {
                new() { Sequence = 1, TargetPrice = 49000, IsTriggered = false },
                new() { Sequence = 2, TargetPrice = 48000, IsTriggered = false },
                new() { Sequence = 3, TargetPrice = 47000, IsTriggered = false }
            }
        };

        // Act
        state.TryAdvanceStopLoss(49000, DateTime.UtcNow, out _, out _);
        state.TryAdvanceStopLoss(48000, DateTime.UtcNow, out _, out _);
        bool result = state.TryAdvanceStopLoss(47000, DateTime.UtcNow, out int seq, out decimal? newSL);

        // Assert
        Assert.True(result);
        Assert.Equal(3, seq);
        Assert.Equal(48000, newSL);
        Assert.Equal(48000, state.CurrentStopLoss);
        Assert.True(state.IsClosed);
    }
}
