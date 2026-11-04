using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Starter.Api.Common;

public static class DatabaseErrors
{
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
