using PerFi.API.Requests;
using PerFi.API.Validation;
using Xunit;

namespace PerFi.Tests.API.Unit;

public class RequestValidatorTests
{
    [Fact]
    public void ValidateCreateAccountRequest_WithMissingName_ReturnsError()
    {
        var errors = RequestValidator.ValidateCreateAccountRequest("   ", 1, 1);

        Assert.True(errors.ContainsKey("accountName"));
    }

    [Fact]
    public void ValidateCreateAccountRequest_WithNonPositiveInstitutionId_ReturnsError()
    {
        var errors = RequestValidator.ValidateCreateAccountRequest("Checking", 0, 1);

        Assert.True(errors.ContainsKey("institutionId"));
    }

    [Fact]
    public void ValidateCreateAccountRequest_WithNonPositiveAccountTypeId_ReturnsError()
    {
        var errors = RequestValidator.ValidateCreateAccountRequest("Checking", 1, 0);

        Assert.True(errors.ContainsKey("accountTypeId"));
    }

    [Fact]
    public void ValidateCreateAccountRequest_WithValidInput_ReturnsNoErrors()
    {
        var errors = RequestValidator.ValidateCreateAccountRequest("Checking", 1, 1);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateUpdateAccountRequest_WithMissingName_ReturnsError()
    {
        var errors = RequestValidator.ValidateUpdateAccountRequest(null, 1, 1);

        Assert.True(errors.ContainsKey("accountName"));
    }

    [Fact]
    public void ValidateCreateInstitutionRequest_WithMissingName_ReturnsError()
    {
        var errors = RequestValidator.ValidateCreateInstitutionRequest("   ");

        Assert.True(errors.ContainsKey("institutionName"));
    }

    [Fact]
    public void ValidateUpdateInstitutionRequest_WithValidName_ReturnsNoErrors()
    {
        var errors = RequestValidator.ValidateUpdateInstitutionRequest("First Bank");

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateCreateAccountTypeRequest_WithMissingName_ReturnsError()
    {
        var errors = RequestValidator.ValidateCreateAccountTypeRequest("   ", 1);

        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void ValidateCreateAccountTypeRequest_WithNonPositiveGroupId_ReturnsError()
    {
        var errors = RequestValidator.ValidateCreateAccountTypeRequest("Checking", 0);

        Assert.True(errors.ContainsKey("accountTypeGroupId"));
    }

    [Fact]
    public void ValidateUpdateAccountTypeRequest_WithValidInput_ReturnsNoErrors()
    {
        var errors = RequestValidator.ValidateUpdateAccountTypeRequest("Checking", 1);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateCreateAccountTypeGroupRequest_WithMissingName_ReturnsError()
    {
        var errors = RequestValidator.ValidateCreateAccountTypeGroupRequest(null);

        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void ValidateUpdateAccountTypeGroupRequest_WithValidName_ReturnsNoErrors()
    {
        var errors = RequestValidator.ValidateUpdateAccountTypeGroupRequest("Assets");

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateCreateFinanceSnapshotRequest_WithDefaultDate_ReturnsError()
    {
        var errors = RequestValidator.ValidateCreateFinanceSnapshotRequest(default, new Dictionary<int, decimal> { [1] = 10m });

        Assert.True(errors.ContainsKey("snapshotDate"));
    }

    [Fact]
    public void ValidateCreateFinanceSnapshotRequest_WithNullBalanceMap_ReturnsError()
    {
        var errors = RequestValidator.ValidateCreateFinanceSnapshotRequest(new DateOnly(2026, 1, 1), null);

        Assert.True(errors.ContainsKey("accountIdToBalanceMap"));
    }

    [Fact]
    public void ValidateCreateFinanceSnapshotRequest_WithNonPositiveAccountId_ReturnsError()
    {
        var errors = RequestValidator.ValidateCreateFinanceSnapshotRequest(new DateOnly(2026, 1, 1), new Dictionary<int, decimal> { [0] = 10m });

        Assert.True(errors.ContainsKey("accountIdToBalanceMap"));
    }

    [Fact]
    public void ValidateUpdateFinanceSnapshotRequest_WithValidInput_ReturnsNoErrors()
    {
        var errors = RequestValidator.ValidateUpdateFinanceSnapshotRequest(new DateOnly(2026, 1, 1), new Dictionary<int, decimal> { [1] = 10m });

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateBulkUpdateFinanceSnapshotCellsRequest_WithNullUpdates_ReturnsError()
    {
        var errors = RequestValidator.ValidateBulkUpdateFinanceSnapshotCellsRequest(null);

        Assert.True(errors.ContainsKey("updates"));
    }

    [Fact]
    public void ValidateBulkUpdateFinanceSnapshotCellsRequest_WithEmptyUpdates_ReturnsError()
    {
        var errors = RequestValidator.ValidateBulkUpdateFinanceSnapshotCellsRequest([]);

        Assert.True(errors.ContainsKey("updates"));
    }

    [Fact]
    public void ValidateBulkUpdateFinanceSnapshotCellsRequest_WithNonPositiveSnapshotId_ReturnsError()
    {
        var errors = RequestValidator.ValidateBulkUpdateFinanceSnapshotCellsRequest([new SnapshotCellUpdateRequest(0, 1, 10m)]);

        Assert.True(errors.ContainsKey("updates"));
    }

    [Fact]
    public void ValidateBulkUpdateFinanceSnapshotCellsRequest_WithNonPositiveAccountId_ReturnsError()
    {
        var errors = RequestValidator.ValidateBulkUpdateFinanceSnapshotCellsRequest([new SnapshotCellUpdateRequest(1, 0, 10m)]);

        Assert.True(errors.ContainsKey("updates"));
    }

    [Fact]
    public void ValidateBulkUpdateFinanceSnapshotCellsRequest_WithValidInput_ReturnsNoErrors()
    {
        var errors = RequestValidator.ValidateBulkUpdateFinanceSnapshotCellsRequest([new SnapshotCellUpdateRequest(1, 1, 10m)]);

        Assert.Empty(errors);
    }
}
