using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Omnia.Api.Data;

internal static class PostgresErrors
{
    internal static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
