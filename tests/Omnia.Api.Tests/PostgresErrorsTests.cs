using Microsoft.EntityFrameworkCore;
using Npgsql;
using Omnia.Api.Data;

namespace Omnia.Api.Tests;

public class PostgresErrorsTests
{
    [Fact]
    public void IsUniqueViolation_UniqueConstraint_ReturnsTrue()
    {
        var exception = new DbUpdateException(
            "duplicate", new PostgresException("duplicate key", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation));

        Assert.True(PostgresErrors.IsUniqueViolation(exception));
    }

    [Fact]
    public void IsUniqueViolation_OtherSqlState_ReturnsFalse()
    {
        var exception = new DbUpdateException(
            "check", new PostgresException("check violation", "ERROR", "ERROR", PostgresErrorCodes.CheckViolation));

        Assert.False(PostgresErrors.IsUniqueViolation(exception));
    }

    [Fact]
    public void IsUniqueViolation_NonPostgresInnerException_ReturnsFalse()
    {
        var exception = new DbUpdateException("network", new InvalidOperationException("connection lost"));

        Assert.False(PostgresErrors.IsUniqueViolation(exception));
    }
}
