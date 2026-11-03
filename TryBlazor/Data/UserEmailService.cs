using Microsoft.Data.SqlClient;

namespace TryBlazor.Data;

public sealed class UserEmailService
{
    private readonly string _connectionString;

    public UserEmailService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Users")
            ?? throw new InvalidOperationException("Connection string 'Users' is not configured.");
    }

    public async Task<IReadOnlyList<string?>> GetFirstTenAsync()
    {
        const string query = "SELECT TOP (10) [Email] FROM [TryDbProjects].[dbo].[Users] ORDER BY [Id];";
        var emails = new List<string?>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            emails.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
        }

        return emails;
    }
}