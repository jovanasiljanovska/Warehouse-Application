using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Warehouse.Repository;
using System.IO;

namespace Warehouse.Tests.Integration.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public TestWebApplicationFactory()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"warehouse_test_{Guid.NewGuid():N}.db");
         _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();

    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(ApplicationDbContext));
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
            services.RemoveAll(typeof(Microsoft.AspNetCore.Antiforgery.IAntiforgery));
            services.AddSingleton<Microsoft.AspNetCore.Antiforgery.IAntiforgery, NoOpAntiforgery>();

            var sqliteEfProvider = new ServiceCollection()
                .AddEntityFrameworkSqlite()
                .BuildServiceProvider();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
                //options.UseInternalServiceProvider(sqliteEfProvider);
            });


            services.PostConfigure<MvcOptions>(o =>
                o.Filters.Add(new IgnoreAntiforgeryTokenAttribute()));

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });

    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using (var scope = host.Services.CreateScope())
        {
            var authOptions = scope.ServiceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Authentication.AuthenticationOptions>>();
            authOptions.Value.DefaultAuthenticateScheme = "Test";
            authOptions.Value.DefaultChallengeScheme = "Test";
        }

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
           
            db.Database.EnsureCreated();
        }

        return host;
    }

    public HttpClient CreateClientAs(string role, string userId)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        return client;
    }


    public HttpClient CreateClientAs(string role) => CreateClientAs(role, "test-user-1");


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}


