using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace DataAgent.Buffering;

/// <summary>
/// SQLite 기반 store-and-forward 버퍼.
/// 수집 데이터는 먼저 여기에 저장되고, 전송 성공(Ack) 후에만 삭제된다.
/// </summary>
public sealed class SqliteReadingBuffer : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly SqliteConnection _conn;
    private readonly Lock _lock = new();

    /// <param name="path">DB 파일 경로. ":memory:"면 메모리 DB (테스트용).</param>
    public SqliteReadingBuffer(string path)
    {
        if (path != ":memory:")
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }
        _conn = new SqliteConnection($"Data Source={path}");
        _conn.Open();
        Execute("PRAGMA journal_mode=WAL;");
        Execute("CREATE TABLE IF NOT EXISTS readings (id INTEGER PRIMARY KEY AUTOINCREMENT, payload TEXT NOT NULL);");
    }

    public void Put(IReadOnlyCollection<Reading> readings)
    {
        if (readings.Count == 0) return;
        lock (_lock)
        {
            using var tx = _conn.BeginTransaction();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "INSERT INTO readings (payload) VALUES ($p);";
            var p = cmd.Parameters.Add("$p", SqliteType.Text);
            foreach (var r in readings)
            {
                p.Value = JsonSerializer.Serialize(r, Json);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
    }

    /// <summary>가장 오래된 데이터부터 limit건 조회 (삭제하지 않음).</summary>
    public IReadOnlyList<(long Id, Reading Reading)> Peek(int limit)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT id, payload FROM readings ORDER BY id LIMIT $n;";
            cmd.Parameters.AddWithValue("$n", limit);
            using var rd = cmd.ExecuteReader();
            var list = new List<(long, Reading)>();
            while (rd.Read())
                list.Add((rd.GetInt64(0), JsonSerializer.Deserialize<Reading>(rd.GetString(1), Json)!));
            return list;
        }
    }

    /// <summary>전송 성공한 데이터 삭제.</summary>
    public void Ack(IReadOnlyCollection<long> ids)
    {
        if (ids.Count == 0) return;
        lock (_lock)
        {
            using var tx = _conn.BeginTransaction();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "DELETE FROM readings WHERE id = $id;";
            var p = cmd.Parameters.Add("$id", SqliteType.Integer);
            foreach (var id in ids)
            {
                p.Value = id;
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
    }

    public long Count()
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM readings;";
            return (long)cmd.ExecuteScalar()!;
        }
    }

    private void Execute(string sql)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }

    public void Dispose() => _conn.Dispose();
}
