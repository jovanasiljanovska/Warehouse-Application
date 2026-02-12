using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using Warehouse.Service.Implementation;
using Warehouse.Tests.Unit.Common;

namespace Warehouse.Tests.Unit.Services
{
    public class FakeStoreCatalogServiceTests
    {
        [Fact]
        public void GetCategories_WhenApiReturnsNull_ShouldReturnEmptyList()
        {
            
            var handler = new StubHttpMessageHandler(req =>
            {
                if (req.RequestUri!.AbsoluteUri == "https://fakestoreapi.com/products/categories")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var http = new HttpClient(handler);
            var sut = new FakeStoreCatalogService(http);

            
            var result = sut.GetCategories();

            
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetCategories_WhenApiReturnsList_ShouldReturnSameList()
        {
            
            var json = JsonSerializer.Serialize(new List<string> { "electronics", "jewelery" });

            var handler = new StubHttpMessageHandler(req =>
            {
                if (req.RequestUri!.AbsoluteUri == "https://fakestoreapi.com/products/categories")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var http = new HttpClient(handler);
            var sut = new FakeStoreCatalogService(http);

            
            var result = sut.GetCategories();

           
            Assert.Equal(2, result.Count);
            Assert.Contains("electronics", result);
            Assert.Contains("jewelery", result);
        }

        [Fact]
        public void GetProductsByCategory_ShouldMapDtosToExternalCatalogItems()
        {
            
            var category = "men's clothing"; 
            var escaped = Uri.EscapeDataString(category);

            var dtoList = new[]
            {
                new { id = 12, title = "T-Shirt", price = 25.49, category = "men's clothing", image = "img1" },
                new { id = 13, title = "Jacket",  price = 100.10, category = "men's clothing", image = "img2" }
            };

            var json = JsonSerializer.Serialize(dtoList);

            var expectedUrl = $"https://fakestoreapi.com/products/category/{escaped}";

            var handler = new StubHttpMessageHandler(req =>
            {
                if (req.RequestUri!.AbsoluteUri == expectedUrl)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var http = new HttpClient(handler);
            var sut = new FakeStoreCatalogService(http);

            
            var result = sut.GetProductsByCategory(category);

            
            Assert.Equal(2, result.Count);

           
            Assert.Equal("FS-12", result[0].ExternalId);
            Assert.Equal("T-Shirt", result[0].Name);
            Assert.Equal("Mens Clothing", result[0].CategoryName); 
            Assert.Equal(25, result[0].UnitPrice); 
            Assert.Equal("img1", result[0].ImageUrl);

            Assert.Equal("FS-13", result[1].ExternalId);
            Assert.Equal(100, result[1].UnitPrice); 
        }

        [Fact]
        public void GetByExternalId_WhenExternalIdIsNullOrWhitespace_ShouldReturnNull()
        {
            var http = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
            var sut = new FakeStoreCatalogService(http);

            Assert.Null(sut.GetByExternalId(null!));
            Assert.Null(sut.GetByExternalId(""));
            Assert.Null(sut.GetByExternalId("   "));
        }

        [Theory]
        [InlineData("FS")]         
        [InlineData("FS-")]        
        [InlineData("FS-abc")]     
        [InlineData("12")]         
        [InlineData("FS-12-34")]   
        public void GetByExternalId_WhenFormatIsInvalid_ShouldReturnNull(string externalId)
        {
            var http = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
            var sut = new FakeStoreCatalogService(http);

            var result = sut.GetByExternalId(externalId);

            Assert.Null(result);
        }

        [Fact]
        public void GetByExternalId_WhenApiReturnsNull_ShouldReturnNull()
        {
            
            var handler = new StubHttpMessageHandler(req =>
            {
                if (req.RequestUri!.AbsoluteUri == "https://fakestoreapi.com/products/12")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("null", Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var http = new HttpClient(handler);
            var sut = new FakeStoreCatalogService(http);

           
            var result = sut.GetByExternalId("FS-12");

            
            Assert.Null(result);
        }

        [Fact]
        public void GetByExternalId_WhenValid_ShouldReturnMappedItem()
        {
            
            var dto = new { id = 12, title = "Laptop", price = 999.60, category = "electronics", image = "img" };
            var json = JsonSerializer.Serialize(dto);

            var handler = new StubHttpMessageHandler(req =>
            {
                if (req.RequestUri!.AbsoluteUri == "https://fakestoreapi.com/products/12")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var http = new HttpClient(handler);
            var sut = new FakeStoreCatalogService(http);

            
            var result = sut.GetByExternalId("FS-12");

            
            Assert.NotNull(result);
            Assert.Equal("FS-12", result!.ExternalId);
            Assert.Equal("Laptop", result.Name);
            Assert.Equal("Electronics", result.CategoryName);
            Assert.Equal(1000, result.UnitPrice); // Round(999.60)=1000
            Assert.Equal("img", result.ImageUrl);
        }
    }
}
