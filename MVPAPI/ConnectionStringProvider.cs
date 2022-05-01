public class ConnectionStringProvider : IConnectionStringProvider
{
    public string ConnectionString { get; }

    public ConnectionStringProvider()
    {
        ConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? throw new InvalidOperationException("Missing: DB_CONNECTION_STRING");
    }
}