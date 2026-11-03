using System.Data;
using Microsoft.Data.SqlClient;

namespace TryBlazor.Data;

public sealed class BeoLineItemService
{
    private readonly string _connectionString;

    public BeoLineItemService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Users")
            ?? throw new InvalidOperationException("Connection string 'Users' is not configured.");
    }

    public async Task<IReadOnlyList<BeoOption>> GetBeosAsync()
    {
        const string query = @"
            SELECT [Id], [EventID], [StartTime],[Location], [Date]
            FROM [TryDbProjects].[dbo].[BEOs]
            ORDER BY [Date] DESC, [Id] DESC;";
        var beos = new List<BeoOption>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            beos.Add(new BeoOption(
                reader.GetInt32(0),
                reader.GetInt32(1),
                TimeOnly.FromTimeSpan(reader.GetTimeSpan(2)),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetDateTime(4)));
        }

        return beos;
    }

    public async Task<IReadOnlyList<LineItem>> GetLineItemsAsync(int beoId)
    {
        const string query = @"
            SELECT [Id], [BEOid], [ItemText], [Quantity], [Notes], [CompletedTime], [CompletedBy]
            FROM [TryDbProjects].[dbo].[LineItems]
            WHERE [BEOid] = @BeoId
            ORDER BY [Id];";
        var lineItems = new List<LineItem>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@BeoId", System.Data.SqlDbType.Int).Value = beoId;
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lineItems.Add(new LineItem(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetTimeSpan(5),
                reader.IsDBNull(6) ? null : reader.GetGuid(6)));
        }

        return lineItems;
    }

    public async Task MarkCompleteAsync(int id)
    {
        const string query = @"
            UPDATE [TryDbProjects].[dbo].[LineItems]
            SET [CompletedTime] = CAST(GETDATE() AS TIME(0))
            WHERE [Id] = @Id;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        if (await command.ExecuteNonQueryAsync() == 0)
        {
            throw new InvalidOperationException($"Line item {id} was not found.");
        }
    }

    public async Task<BeoImportResult> ImportRowsAsync(IReadOnlyList<BeoImportRow> rows)
    {
        if (rows.Count == 0)
        {
            return new BeoImportResult(0, 0);
        }

        var beoIds = new Dictionary<BeoImportKey, int>();
        var lineItemsCreated = 0;
        

        var BEOTable = new DataTable("BEOs");
        var lineItemTable = new DataTable("LineItems");

        BEOTable.Columns.Add("ID", typeof(int));
        BEOTable.Columns.Add("EventID", typeof(int));
        BEOTable.Columns.Add("Location", typeof(string));
        BEOTable.Columns.Add("StartTime", typeof(TimeSpan));
        BEOTable.Columns.Add("EndTime", typeof(TimeSpan));
        BEOTable.Columns.Add("Date", typeof(DateTime));
        BEOTable.Columns.Add("TechAssigned", typeof(int));
        BEOTable.Columns.Add("Zone", typeof(string));
        BEOTable.Columns.Add("ImageLink", typeof(string));
        BEOTable.Columns.Add("Comments", typeof(string));
        BEOTable.Columns.Add("Available", typeof(string));
        BEOTable.Columns.Add("TurnTime", typeof(int));

        //lineItemTable.Columns.Add("Id", typeof(int));
        lineItemTable.Columns.Add("BEOid", typeof(int));
        lineItemTable.Columns.Add("ItemText", typeof(string));
        lineItemTable.Columns.Add("Quantity", typeof(int));
        lineItemTable.Columns.Add("Notes", typeof(string));
        lineItemTable.Columns.Add("CompletedTime", typeof(TimeSpan));
        lineItemTable.Columns.Add("CompletedBy", typeof(int));


        int lastEventId = 0;
        DateTime lastDate = DateTime.MinValue;
        TimeOnly lastStartTime = TimeOnly.MinValue;

        int hashCode = 0;

        foreach (var row in rows)
        {
            var key = new BeoImportKey(row.EventId, row.Date, row.Room, row.StartTime, row.EndTime, row.Avail);
            if (!beoIds.ContainsKey(key))
            {
                if(row.Avail != "" && (row.EventId != lastEventId || row.Date.ToDateTime(TimeOnly.MinValue) != lastDate || row.StartTime != lastStartTime))
                {
                    var newRow = BEOTable.NewRow();

                    string dataForHash = $"{row.EventId}-{row.Date}-{row.Room}-{row.StartTime}-{row.EndTime}-{row.Avail}";
                    
                    hashCode = dataForHash.GetHashCode();
                    
                    newRow["ID"] = hashCode; // Generate a unique ID based on the row data
                    newRow["EventID"] = row.EventId;
                    newRow["StartTime"] = row.StartTime.ToTimeSpan();
                    newRow["EndTime"] = row.EndTime.ToTimeSpan();
                    newRow["Date"] = row.Date.ToDateTime(TimeOnly.MinValue);
                    newRow["Location"] = row.Room;
                    newRow["Available"] = row.Avail;
                    newRow["TechAssigned"] = DBNull.Value;
                    newRow["Zone"] = DBNull.Value;
                    newRow["ImageLink"] = DBNull.Value;
                    newRow["Comments"] = DBNull.Value;
                    newRow["TurnTime"] = DBNull.Value;
                    BEOTable.Rows.Add(newRow);
                }
            }

            var lineItemRow = lineItemTable.NewRow();
            lineItemRow["BEOid"] = hashCode; // Use the generated ID
            lineItemRow["ItemText"] = (object?)row.Description ?? "";
            lineItemRow["Quantity"] = (object?)row.Quantity ?? 0;
            lineItemRow["Notes"] = (object?)row.Notes ?? DBNull.Value;
            lineItemRow["CompletedTime"] = DBNull.Value;
            lineItemRow["CompletedBy"] = DBNull.Value;
            lineItemTable.Rows.Add(lineItemRow);
            lineItemsCreated++;
        }
        try{
            await using var connection = new SqlConnection(_connectionString);
            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = "TryDbProjects.dbo.BEOs",
                BatchSize = 1000
            };
            await connection.OpenAsync();
        
            await bulkCopy.WriteToServerAsync(BEOTable);

            bulkCopy.DestinationTableName = "TryDbProjects.dbo.LineItems";

            bulkCopy.ColumnMappings.Clear();
            bulkCopy.ColumnMappings.Add("BEOid", "BEOid");
            bulkCopy.ColumnMappings.Add("ItemText", "ItemText");
            bulkCopy.ColumnMappings.Add("Quantity", "Quantity");
            bulkCopy.ColumnMappings.Add("Notes", "Notes");
            bulkCopy.ColumnMappings.Add("CompletedTime", "CompletedTime");
            bulkCopy.ColumnMappings.Add("CompletedBy", "CompletedBy");

            await bulkCopy.WriteToServerAsync(lineItemTable);
        }
        catch (Exception ex)
        {
            // Handle the exception (e.g., log it)
            throw new InvalidOperationException("An error occurred while importing BEOs.", ex);
        }

        
        // foreach (var row in rows)
        // {
        //     var key = new BeoImportKey(row.EventId, row.Date, row.Room, row.StartTime, row.EndTime, row.Avail);
        //     if (!beoIds.TryGetValue(key, out var beoId))
        //     {
        //         const string insertBeo = @"
        //             INSERT INTO [TryDbProjects].[dbo].[BEOs] ([EventID], [StartTime], [Location], [Date])
        //             OUTPUT INSERTED.[Id]
        //             VALUES (@EventId, @StartTime, @Location, @Date);";

        //         await using var beoCommand = new SqlCommand(insertBeo, connection, transaction);
        //         beoCommand.Parameters.Add("@EventId", System.Data.SqlDbType.Int).Value = row.EventId;
        //         beoCommand.Parameters.Add("@StartTime", System.Data.SqlDbType.Time).Value = row.StartTime.ToTimeSpan();
        //         beoCommand.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 200).Value = row.Room;
        //         beoCommand.Parameters.Add("@Date", System.Data.SqlDbType.Date).Value = row.Date.ToDateTime(TimeOnly.MinValue);
        //         beoId = (int)(await beoCommand.ExecuteScalarAsync()
        //             ?? throw new InvalidOperationException("The BEO insert did not return an ID."));
        //         beoIds.Add(key, beoId);
        //     }

        //     const string insertLineItem = @"
        //         INSERT INTO [TryDbProjects].[dbo].[LineItems] ([BEOid], [ItemText], [Quantity], [Notes])
        //         VALUES (@BeoId, @ItemText, @Quantity, @Notes);";

        //     await using var lineItemCommand = new SqlCommand(insertLineItem, connection, transaction);
        //     lineItemCommand.Parameters.Add("@BeoId", System.Data.SqlDbType.Int).Value = beoId;
        //     lineItemCommand.Parameters.Add("@ItemText", System.Data.SqlDbType.NVarChar, -1).Value =
        //         (object?)row.Description ?? DBNull.Value;
        //     lineItemCommand.Parameters.Add("@Quantity", System.Data.SqlDbType.Int).Value =
        //         (object?)row.Quantity ?? DBNull.Value;
        //     lineItemCommand.Parameters.Add("@Notes", System.Data.SqlDbType.NVarChar, -1).Value =
        //         (object?)row.Notes ?? DBNull.Value;
        //     await lineItemCommand.ExecuteNonQueryAsync();
        //     lineItemsCreated++;
        // }

        //await transaction.CommitAsync();
        return new BeoImportResult(beoIds.Count, lineItemsCreated);
    }
}

public sealed record BeoOption(int Id, int EventId, TimeOnly StartTime, string? Location, DateTime Date);

public sealed record BeoImportRow(    
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Room,
    string Avail,
    string? Description,
    int? Quantity,
    string? Notes,
    int EventId);

public sealed record BeoImportResult(int BeosCreated, int LineItemsCreated);

internal readonly record struct BeoImportKey(int EventId, DateOnly Date, string Room, TimeOnly StartTime, TimeOnly EndTime, string Avail)
{
   // public BeoImportKey(int eventId, DateOnly date, string room, TimeOnly startTime, TimeOnly endTime, string avail)
   //     : this(eventId, date, room, startTime, endTime, avail)
   // {
   // }
};

public sealed record LineItem(
    int Id,
    int BeoId,
    string? ItemText,
    int? Quantity,
    string? Notes,
    TimeSpan? CompletedTime,
    Guid? CompletedBy);
