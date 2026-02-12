using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Domain.Identity;
using Warehouse.Repository;
using Warehouse.Tests.Integration.Infrastructure;

public class ProductsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    public ProductsIntegrationTests(TestWebApplicationFactory factory) => _factory = factory;

    private static string ExtractAntiForgeryToken(string html)
    {
        var marker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var start = html.IndexOf(marker);
        Assert.True(start > 0, "Antiforgery token not found.");
        start += marker.Length;
        var end = html.IndexOf("\"", start);
        return html.Substring(start, end - start);
    }

    [Fact]
    public async Task Products_Create_Valid_ShouldRedirect_AndCreateStockBalance()
    {
        var client = _factory.CreateClientAs("Employee");

        Guid categoryId;
        const string supplierId = "supplier-1";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Users.Add(new WarehouseApplicationUser
            {
                Id = supplierId,
                UserName = "supplier1",
                NormalizedUserName = "SUPPLIER1",
                Email = "supplier1@test.local",
                NormalizedEmail = "SUPPLIER1@TEST.LOCAL",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                CompanyName = "Test Supplier"
            });

            var cat = new Category { Id = Guid.NewGuid(), Name = "Food" };
            db.Categories.Add(cat);

            db.SaveChanges();
            categoryId = cat.Id;
        }

        var get = await client.GetAsync("/Products/Create");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var html = await get.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", token),

            new KeyValuePair<string,string>("Name", "Milk"),
            new KeyValuePair<string,string>("SKU", "MILK-1"),
            new KeyValuePair<string,string>("CategoryId", categoryId.ToString()),
            new KeyValuePair<string,string>("SupplierId", supplierId),
            new KeyValuePair<string,string>("ImageURL", ""),
            new KeyValuePair<string,string>("UnitPrice", "100"),

            new KeyValuePair<string,string>("locationType", LocationType.Shelves.ToString()),
            new KeyValuePair<string,string>("quantity", "10")
        });

        var res = await client.PostAsync("/Products/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Products", res.Headers.Location!.ToString());


        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var p = await db.Products.FirstOrDefaultAsync(x => x.SKU == "MILK-1");
            Assert.NotNull(p);

            var sb = await db.StockBalances.FirstOrDefaultAsync(x =>
                x.ProductId == p!.Id && x.LocationType == LocationType.Shelves);

            Assert.NotNull(sb);
            Assert.Equal(10, sb!.Quantity);
        }
    }

    [Fact]
    public async Task Products_Create_WithQuantityZero_ShouldReturn200_AndNotInsertProduct()
    {
        var client = _factory.CreateClientAs("Employee");

        Guid categoryId;
        const string supplierId = "supplier-2";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Users.Add(new WarehouseApplicationUser
            {
                Id = supplierId,
                UserName = "supplier2",
                NormalizedUserName = "SUPPLIER2",
                Email = "supplier2@test.local",
                NormalizedEmail = "SUPPLIER2@TEST.LOCAL",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                CompanyName = "Test Supplier 2"
            });

            var cat = new Category { Id = Guid.NewGuid(), Name = "Food2" };
            db.Categories.Add(cat);

            db.SaveChanges();
            categoryId = cat.Id;
        }

        var get = await client.GetAsync("/Products/Create");
        get.EnsureSuccessStatusCode();
        var token = ExtractAntiForgeryToken(await get.Content.ReadAsStringAsync());

        var form = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string,string>("__RequestVerificationToken", token),

        new KeyValuePair<string,string>("Name", "Yogurt"),
        new KeyValuePair<string,string>("SKU", "YOG-1"),
        new KeyValuePair<string,string>("CategoryId", categoryId.ToString()),
        new KeyValuePair<string,string>("SupplierId", supplierId),
        new KeyValuePair<string,string>("ImageURL", ""),
        new KeyValuePair<string,string>("UnitPrice", "120"),

        new KeyValuePair<string,string>("locationType", LocationType.Shelves.ToString()),
        new KeyValuePair<string,string>("quantity", "0") 
        });

        var res = await client.PostAsync("/Products/Create", form);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var exists = await db.Products.AnyAsync(x => x.SKU == "YOG-1");
            Assert.False(exists);
        }
    }

    [Fact]
    public async Task Products_DeleteConfirmed_ShouldRedirect_AndRemoveProduct()
    {
        var client = _factory.CreateClientAs("Employee");

        Guid productId;
        const string supplierId = "supplier-del-1";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Users.Add(new WarehouseApplicationUser
            {
                Id = supplierId,
                UserName = "supplier_del",
                NormalizedUserName = "SUPPLIER_DEL",
                Email = "supplier_del@test.local",
                NormalizedEmail = "SUPPLIER_DEL@TEST.LOCAL",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                CompanyName = "Supplier Del"
            });

            var cat = new Category { Id = Guid.NewGuid(), Name = "DeleteCat" };
            db.Categories.Add(cat);

            var p = new Product
            {
                Id = Guid.NewGuid(),
                Name = "ToDelete",
                SKU = "DEL-1",
                CategoryId = cat.Id,
                SupplierId = supplierId,
                UnitPrice = 50,
                ImageURL = ""
            };

            db.Products.Add(p);
            db.SaveChanges();

            productId = p.Id;
        }


        var get = await client.GetAsync($"/Products/Delete/{productId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var token = ExtractAntiForgeryToken(await get.Content.ReadAsStringAsync());


        var form = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string,string>("__RequestVerificationToken", token),
        new KeyValuePair<string,string>("id", productId.ToString())
        });

        var res = await client.PostAsync($"/Products/Delete/{productId}", form);

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Products", res.Headers.Location!.ToString());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var exists = await db.Products.AnyAsync(x => x.Id == productId);
            Assert.False(exists);
        }
    }


}
