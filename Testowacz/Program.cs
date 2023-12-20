using System.Diagnostics;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

await TestPostgres();

app.Run();


async Task TestPostgres()
{
    var your_password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
    var connectionString =
        $"Server=sowavirtualep.writer.postgres.database.azure.com;Database=postgres;Port=5432;User Id=sowa;Password={your_password};Ssl Mode=Require;";

    var client = new Npgsql.NpgsqlConnection(connectionString);

    client.Open();

    Console.WriteLine("Connected to Postgres");

    // create table
    var createTableCommand = client.CreateCommand();
    // guid primary key
    createTableCommand.CommandText = "CREATE TABLE IF NOT EXISTS test_table2 (guid uuid PRIMARY KEY, time timestamp, iter integer)";
    await createTableCommand.ExecuteNonQueryAsync();

    var sw = new Stopwatch();

    var iteration = 0;
    while (true)
    {
        iteration++;
        await Task.Delay(1000);

        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Q)
            {
                break;
            }
        }

        var reader = default(Npgsql.NpgsqlDataReader);
        var countReader = default(Npgsql.NpgsqlDataReader);

        try
        {
            sw.Restart();
            Console.WriteLine($"Iteration: {iteration}");

            var guid = Guid.NewGuid();

            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // insert data
            var insertCommand = client.CreateCommand();
            insertCommand.CommandText = $"INSERT INTO test_table2 (guid, time, iter) VALUES ('{guid}', now(), {iteration})";
            await insertCommand.ExecuteNonQueryAsync();

            // select data
            var selectCommand = client.CreateCommand();
            selectCommand.CommandText = $"SELECT guid, iter FROM test_table2 WHERE guid = '{guid}'";
            reader = await selectCommand.ExecuteReaderAsync();

            // verify data
            while (reader.Read())
            {
                var id = reader.GetGuid(0);
                var iter = reader.GetInt32(1);
                Console.WriteLine($"id: {id}, iter: {iter}");

                if (iter != iteration)
                {
                    throw new Exception($"Invalid iter value: {iter}, should be: {iteration}");
                }
            }
            reader.Close();

            // select data
            var countCommand = client.CreateCommand();
            countCommand.CommandText = $"SELECT COUNT(*) FROM test_table2";
            countReader = await countCommand.ExecuteReaderAsync();

            // verify data
            while (countReader.Read())
            {
                var count = countReader.GetInt32(0);
                Console.WriteLine($"total count: {count}");
            }
            reader.Close();

            sw.Stop();

            Console.WriteLine($"Iteration {iteration} completed in {sw.Elapsed.TotalMilliseconds:00.00} ms");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e.Message}");

            if (e is PostgresException pe)
            {
                Console.WriteLine($"PostgresException: {pe.SqlState}");
                Console.WriteLine($"Reconnecting to Postgres");
                if (reader != null)
                {
                    await reader.CloseAsync();
                    reader = null;
                }
                if (countReader != null)
                {
                    await countReader.CloseAsync();
                    countReader = null;
                }

                client.Close();
                client.Open();
            }
        }
        finally
        {
            if (reader != null)
            {
                await reader.CloseAsync();
            }
            if (countReader != null)
            {
                await countReader.CloseAsync();
            }
        }
    }
}