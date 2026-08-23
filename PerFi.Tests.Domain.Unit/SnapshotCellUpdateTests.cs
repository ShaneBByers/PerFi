using PerFi.Domain.Entities;
using Xunit;

namespace PerFi.Tests.Domain.Unit;

public class SnapshotCellUpdateTests
{
    [Fact]
    public void Constructor_AssignsAllProperties()
    {
        var update = new SnapshotCellUpdate(1, 2, 3.5m);

        Assert.Equal(1, update.SnapshotId);
        Assert.Equal(2, update.AccountId);
        Assert.Equal(3.5m, update.Balance);
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        var first = new SnapshotCellUpdate(1, 2, 3.5m);
        var second = new SnapshotCellUpdate(1, 2, 3.5m);

        Assert.Equal(first, second);
    }
}
