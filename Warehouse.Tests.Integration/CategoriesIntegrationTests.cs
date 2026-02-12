using Microsoft.Extensions.DependencyInjection;
using NuGet.Common;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Warehouse.Domain.Domain;
using Warehouse.Repository;
using Warehouse.Tests.Integration.Infrastructure;
using Xunit;

public class CategoriesIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CategoriesIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

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
    public async Task Categories_Index_ShouldReturn200()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/Categories");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Categories_Create_ShouldRedirectToIndex()
    {
        var client = _factory.CreateClientAs("Employee");

        var get = await client.GetAsync("/Categories/Create");
        get.EnsureSuccessStatusCode();

        var html = await get.Content.ReadAsStringAsync();

        var token = ExtractAntiForgeryToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string,string>("__RequestVerificationToken", token),
        new KeyValuePair<string,string>("Name", "Dairy"),
        new KeyValuePair<string,string>("Description", "Milk products"),
        new KeyValuePair<string,string>("ImageURL", "")
    });

        var res = await client.PostAsync("/Categories/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Categories", res.Headers.Location!.ToString());
    }


    [Fact]
    public async Task Categories_Edit_ShouldRedirectToIndex()
    {
        var client = _factory.CreateClientAs("Employee");

        Guid categoryId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cat = new Category { Id = Guid.NewGuid(), Name = "OldName", Description = "Old", ImageURL = "" };
            db.Categories.Add(cat);
            db.SaveChanges();
            categoryId = cat.Id;
        }


        var get = await client.GetAsync($"/Categories/Edit/{categoryId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var html = await get.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(html);

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", token),
            new KeyValuePair<string,string>("Id", categoryId.ToString()),
            new KeyValuePair<string,string>("Name", "NewName"),
            new KeyValuePair<string,string>("Description", "NewDesc"),
            new KeyValuePair<string,string>("ImageURL", "")
        });

        var res = await client.PostAsync($"/Categories/Edit/{categoryId}", form);

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Categories", res.Headers.Location!.ToString());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updated = await db.Categories.FindAsync(categoryId);
            Assert.NotNull(updated);
            Assert.Equal("NewName", updated!.Name);
        }
    }


}
