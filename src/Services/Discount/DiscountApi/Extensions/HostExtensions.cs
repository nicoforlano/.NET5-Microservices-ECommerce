using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DiscountApi.Extensions
{
    public static class HostExtensions
    {
        public static IHost MigrateDatabase<TContext>(this IHost host, int? retry = 0)
        {

            int retryForAvalilability = retry.Value;

            using( var scope = host.Services.CreateScope()) 
            {
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger>();

                try
                {
                    logger.LogInformation("Migrating postgresql");
                    using var connection = new NpgsqlConnection(
                        configuration.GetValue<string>("DatabaseSettings:ConnectionString")
                    );
                    connection.Open();

                    using var command = new NpgsqlCommand
                    {
                        Connection = connection
                    };

                    command.CommandText = "DROP TABLE IF EXISTS Coupon";
                    command.ExecuteNonQuery();

                    command.CommandText = @"create table Coupon (
	                                            ID serial PRIMARY KEY NOT NULL,
	                                            ProductName VARCHAR(24) NOT NULL,
	                                            Description TEXT,
	                                            Amount INT
                                            );";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into Coupon (ProductName, Description, Amount) values ('Iphone X', 'IPhone Discount', 150);";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into Coupon(ProductName, Description, Amount) values('Samsung 10', 'Samsung Discount', 100);";
                    command.ExecuteNonQuery();

                    logger.LogInformation("Migrated postgresql database.");
                }
                catch (NpgsqlException exc)
                {
                    logger.LogError(exc, "An error ocurred while migrating db");
                    if(retryForAvalilability < 50)
                    {
                        retryForAvalilability++;
                        System.Threading.Thread.Sleep(2000);
                        MigrateDatabase<TContext>(host, retryForAvalilability);
                    }
                }

                return host;
            }
        }
    }
}
