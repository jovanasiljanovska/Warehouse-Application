using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.Json;
using Warehouse.Domain.External;
using Warehouse.Domain.External.FakeStore;
using Warehouse.Service.Interface;

namespace Warehouse.Service.Implementation;

public class FakeStoreCatalogService : IFakeStoreCatalogService
{
    private readonly HttpClient _http;

    public FakeStoreCatalogService(HttpClient http)
    {
        _http = http;
    }

    public List<string> GetCategories()
    {
        var url = "https://fakestoreapi.com/products/categories";
        var categories = _http.GetFromJsonAsync<List<string>>(url).GetAwaiter().GetResult();
        return categories ?? new List<string>();
    }

    public List<ExternalCatalogItem> GetProductsByCategory(string category)
    {
        var url = $"https://fakestoreapi.com/products/category/{Uri.EscapeDataString(category)}";
        var products = _http.GetFromJsonAsync<List<FSProductDto>>(url).GetAwaiter().GetResult()
                      ?? new List<FSProductDto>();

        return products.Select(p => new ExternalCatalogItem
        {
            ExternalId = $"FS-{p.Id}",
            Name = p.Title,
            CategoryName = NormalizeCategory(p.Category),
            UnitPrice = (int)Math.Round(p.Price),
            ImageUrl = p.Image
        }).ToList();
    }

    public ExternalCatalogItem? GetByExternalId(string externalId)
    {
        // externalId: "FS-12"
        if (string.IsNullOrWhiteSpace(externalId)) return null;
        var parts = externalId.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var id)) return null;

        var url = $"https://fakestoreapi.com/products/{id}";
        var p = _http.GetFromJsonAsync<FSProductDto>(url).GetAwaiter().GetResult();
        if (p == null) return null;

        return new ExternalCatalogItem
        {
            ExternalId = $"FS-{p.Id}",
            Name = p.Title,
            CategoryName = NormalizeCategory(p.Category),
            UnitPrice = (int)Math.Round(p.Price),
            ImageUrl = p.Image
        };
    }

    private static string NormalizeCategory(string c)
    {
        c = (c ?? "").Trim();
        if (c.Length == 0) return "General";
        // "men's clothing" -> "Mens Clothing"
        c = c.Replace("'", "");
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(c);
    }
}

