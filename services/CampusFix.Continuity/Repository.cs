using System.Text.Json;
using Npgsql;
namespace CampusFix.Continuity;

public sealed class Repository(NpgsqlDataSource source) {
    public static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    public async Task Initialize(){await using var cmd=source.CreateCommand("CREATE TABLE IF NOT EXISTS continuity_state (id integer PRIMARY KEY CHECK(id=1), data jsonb NOT NULL); INSERT INTO continuity_state VALUES(1,'{}'::jsonb) ON CONFLICT(id) DO NOTHING;");await cmd.ExecuteNonQueryAsync();}
    public async Task<State> Read(){await using var cmd=source.CreateCommand("SELECT data::text FROM continuity_state WHERE id=1");return JsonSerializer.Deserialize<State>((string)(await cmd.ExecuteScalarAsync())!,Json)!;}
    public async Task<State> Change(Action<State> mutate){
        await using var conn=await source.OpenConnectionAsync();await using var tx=await conn.BeginTransactionAsync();
        await using var get=new NpgsqlCommand("SELECT data::text FROM continuity_state WHERE id=1 FOR UPDATE",conn,tx);var state=JsonSerializer.Deserialize<State>((string)(await get.ExecuteScalarAsync())!,Json)!;
        mutate(state);
        await using var put=new NpgsqlCommand("UPDATE continuity_state SET data=CAST(@data AS jsonb) WHERE id=1",conn,tx);put.Parameters.AddWithValue("data",JsonSerializer.Serialize(state,Json));await put.ExecuteNonQueryAsync();await tx.CommitAsync();return state;
    }
}
