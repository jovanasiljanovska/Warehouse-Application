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

public class CartsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    public CartsIntegrationTests(TestWebApplicationFactory factory) => _factory = factory;

    private static string ExtractAntiForgeryToken(string html)
    {
        var marker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var start = html.IndexOf(marker);
        Assert.True(start > 0, "Antiforgery token not found.");
        start += marker.Length;
        var end = html.IndexOf("\"", start);
        return html.Substring(start, end - start);
    }

    private static void EnsureUserExists(ApplicationDbContext db, string userId, string userName, string email)
    {
        if (db.Users.Any(u => u.Id == userId)) return;

        var normalizedUserName = (userName ?? userId).ToUpperInvariant();
        var normalizedEmail = (email ?? $"{userId}@test.local").ToUpperInvariant();

        if (db.Users.Any(u => u.NormalizedUserName == normalizedUserName))
        {
            userName = $"{userName}_{userId}";
            normalizedUserName = userName.ToUpperInvariant();
        }

        if (db.Users.Any(u => u.NormalizedEmail == normalizedEmail))
        {
            email = $"{userId}@test.local";
            normalizedEmail = email.ToUpperInvariant();
        }

        db.Users.Add(new WarehouseApplicationUser
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = normalizedUserName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CompanyName = "Test Company"
        });

        db.SaveChanges();
    }



    [Fact]
    public async Task Carts_Index_AsCustomer_ShouldReturn200_AndCreateCartIfMissing()
    {
        var client = _factory.CreateClientAs("Customer");

        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            EnsureUserExists(seedDb, "test-user-1", "customer1", "customer1@test.local");
        }

        var res = await client.GetAsync("/Carts");
        var body = await res.Content.ReadAsStringAsync();
        Console.WriteLine(body);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cart = await db.ShoppingCarts.FirstOrDefaultAsync(c => c.UserId == "test-user-1");
        Assert.NotNull(cart);
    }

    [Fact]
    public async Task Carts_Add_ShouldRedirect_AndInsertCartItem()
    {
        var client = _factory.CreateClientAs("Customer");

        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            EnsureUserExists(seedDb, "test-user-1", "customer1", "customer1@test.local");

        }

        Guid productId;
        const string supplierId = "supplier-cart-1";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            db.Users.Add(new WarehouseApplicationUser
            {
                Id = supplierId,
                UserName = "supplier_cart",
                NormalizedUserName = "SUPPLIER_CART",
                Email = "supplier_cart@test.local",
                NormalizedEmail = "SUPPLIER_CART@TEST.LOCAL",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                CompanyName = "Supplier Cart"
            });

            var cat = new Category { Id = Guid.NewGuid(), Name = "CatCart" };
            db.Categories.Add(cat);

            var p = new Product
            {
                Id = Guid.NewGuid(),
                Name = "CartProduct",
                SKU = "CART-1",
                CategoryId = cat.Id,
                SupplierId = supplierId,
                UnitPrice = 10,
                ImageURL = ""
            };
            db.Products.Add(p);

            db.SaveChanges();
            productId = p.Id;
        }


        // POST Add
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("productId", productId.ToString()),
            new KeyValuePair<string,string>("quantity", "2"),
        });

        var res = await client.PostAsync("/Carts/Add", form);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Carts", res.Headers.Location!.ToString());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cart = await db.ShoppingCarts.FirstAsync(c => c.UserId == "test-user-1");
            var item = await db.ProductInShoppingCarts
                .FirstOrDefaultAsync(i => i.ShoppingCartId == cart.Id && i.ProductId == productId);

            Assert.NotNull(item);
            Assert.Equal(2, item!.Quantity);
        }
    }

    //[Fact]
    //public async Task Carts_Checkout_ShouldRedirect_AndCreateOrder_AndClearCart_AndMoveStock()
    //{
    //    var client = _factory.CreateClientAs("Customer");

    //    using (var seedScope = _factory.Services.CreateScope())
    //    {
    //        var seedDb = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //        EnsureUserExists(seedDb, "test-user-1", "customer1", "customer1@test.local");

    //    }

    //    // Seed product + initial stock + cart item
    //    Guid productId;
    //    Guid cartId;

    //    const string supplierId = "supplier-checkout-1";
    //    const string customerId = "test-user-1";

    //    using (var scope = _factory.Services.CreateScope())
    //    {
    //        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    //        db.Users.Add(new WarehouseApplicationUser
    //        {
    //            Id = supplierId,
    //            UserName = "supplier_checkout",
    //            NormalizedUserName = "SUPPLIER_CHECKOUT",
    //            Email = "supplier_checkout@test.local",
    //            NormalizedEmail = "SUPPLIER_CHECKOUT@TEST.LOCAL",
    //            SecurityStamp = Guid.NewGuid().ToString(),
    //            ConcurrencyStamp = Guid.NewGuid().ToString(),
    //            CompanyName = "Supplier Checkout"
    //        });

    //        var cat = new Category { Id = Guid.NewGuid(), Name = "CatCheckout" };
    //        db.Categories.Add(cat);

    //        var p = new Product
    //        {
    //            Id = Guid.NewGuid(),
    //            Name = "CheckoutProduct",
    //            SKU = "CHK-1",
    //            CategoryId = cat.Id,
    //            SupplierId = supplierId,
    //            UnitPrice = 25,
    //            ImageURL = ""
    //        };
    //        db.Products.Add(p);
    //        productId = p.Id;

    //        // Stock: put 5 on Shelves so MoveToShipping(3) can succeed
    //        db.StockBalances.AddRange(
    //        new StockBalance
    //        {
    //            Id = Guid.NewGuid(),
    //            ProductId = productId,
    //            LocationType = LocationType.Shelves,
    //            Quantity = 5
    //        },
    //        new StockBalance
    //        {
    //            Id = Guid.NewGuid(),
    //            ProductId = productId,
    //            LocationType = LocationType.Freezer,
    //            Quantity = 0
    //        },
    //        new StockBalance
    //        {
    //            Id = Guid.NewGuid(),
    //            ProductId = productId,
    //            LocationType = LocationType.Shipping,
    //            Quantity = 0
    //        }
    //        );


    //        // Create cart + item
    //        var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = customerId };
    //        db.ShoppingCarts.Add(cart);
    //        cartId = cart.Id;

    //        db.ProductInShoppingCarts.Add(new ProductInShoppingCart
    //        {
    //            Id = Guid.NewGuid(),
    //            ShoppingCartId = cartId,
    //            ProductId = productId,
    //            Quantity = 3
    //        });

    //        db.SaveChanges();
    //        db.ChangeTracker.Clear();
    //        var shelves = db.StockBalances.First(sb => sb.ProductId == productId && sb.LocationType == LocationType.Shelves);

    //        Assert.Equal(5, shelves.Quantity);

    //    }

    //    // GET /Carts to get antiforgery token
    //    // var get = await client.GetAsync("/Carts");
    //    // get.EnsureSuccessStatusCode();
    //    // var token = ExtractAntiForgeryToken(await get.Content.ReadAsStringAsync());

    //    // POST Checkout
    //    //var form = new FormUrlEncodedContent(new[]
    //    //{
    //    //    new KeyValuePair<string,string>("__RequestVerificationToken", token),
    //    //});
    //    using (var checkScope = _factory.Services.CreateScope())
    //    {
    //        var db = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //        var totalStock = db.StockBalances.Where(s => s.ProductId == productId).Sum(s => s.Quantity);
    //        if (totalStock < 3)
    //        {
    //            throw new Exception($"Test Setup Error: Database only has {totalStock} stock for product {productId} before checkout!");
    //        }
    //    }

    //    var res = await client.PostAsync("/Carts/Checkout",
    //    new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>()));

    //    var body = await res.Content.ReadAsStringAsync();
    //    Console.WriteLine(body);
    //    if ((int)res.StatusCode >= 500)
    //    {
    //        throw new Exception($"Status: {(int)res.StatusCode} {res.StatusCode}\n\n{body}");
    //    }

    //    // Redirect to CustomerOrders/Details?id=...
    //    Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
    //    Assert.Contains("/CustomerOrders/Details", res.Headers.Location!.ToString());

    //    using (var scope = _factory.Services.CreateScope())
    //    {
    //        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    //        // Order exists
    //        var order = await db.CustomerOrders.FirstOrDefaultAsync(o => o.CustomerId == customerId);
    //        Assert.NotNull(order);

    //        // Lines exist
    //        var lines = await db.ProductInOrders.Where(l => l.OrderId == order!.Id).ToListAsync();
    //        Assert.Single(lines);
    //        Assert.Equal(productId, lines[0].ProductId);
    //        Assert.Equal(3, lines[0].Quantity);

    //        // Cart cleared
    //        var cartItems = await db.ProductInShoppingCarts.Where(i => i.ShoppingCartId == cartId).ToListAsync();
    //        Assert.Empty(cartItems);

    //        // Stock moved: Shelves down, Shipping up
    //        var shelves = await db.StockBalances.FirstAsync(sb => sb.ProductId == productId && sb.LocationType == LocationType.Shelves);
    //        var shipping = await db.StockBalances.FirstOrDefaultAsync(sb => sb.ProductId == productId && sb.LocationType == LocationType.Shipping);

    //        Assert.Equal(2, shelves.Quantity); // 5 - 3
    //        Assert.NotNull(shipping);
    //        Assert.Equal(3, shipping!.Quantity);
    //    }
    //}

    [Fact]
    public async Task Carts_Remove_ShouldRedirect_AndDeleteCartItem()
    {
        var customerId = $"customer-remove-{Guid.NewGuid():N}";
        var supplierId = $"supplier-remove-{Guid.NewGuid():N}";

        var client = _factory.CreateClientAs("Customer", customerId);

        Guid productId;
        Guid cartId;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            EnsureUserExists(db, customerId, $"customer_{customerId}", $"{customerId}@test.local");
            EnsureUserExists(db, supplierId, $"supplier_{supplierId}", $"{supplierId}@test.local");

            var cat = new Category { Id = Guid.NewGuid(), Name = "CatRemove" };
            db.Categories.Add(cat);

            var p = new Product
            {
                Id = Guid.NewGuid(),
                Name = "RemoveProduct",
                SKU = $"REM-{Guid.NewGuid():N}".Substring(0, 12),
                CategoryId = cat.Id,
                SupplierId = supplierId,
                UnitPrice = 10,
                ImageURL = ""
            };
            db.Products.Add(p);
            productId = p.Id;

            var cart = new ShoppingCart { Id = Guid.NewGuid(), UserId = customerId };
            db.ShoppingCarts.Add(cart);
            cartId = cart.Id;

            db.ProductInShoppingCarts.Add(new ProductInShoppingCart
            {
                Id = Guid.NewGuid(),
                ShoppingCartId = cartId,
                ProductId = productId,
                Quantity = 2
            });

            db.SaveChanges();
        }

        using (var checkScope = _factory.Services.CreateScope())
        {
            var db = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.True(await db.ProductInShoppingCarts.AnyAsync(i => i.ShoppingCartId == cartId && i.ProductId == productId));
        }

        var res = await client.PostAsync(
            $"/Carts/Remove?productId={productId}",
            new FormUrlEncodedContent(Array.Empty<KeyValuePair<string, string>>())
        );

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/Carts", res.Headers.Location!.ToString());

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = await db.ProductInShoppingCarts
                .FirstOrDefaultAsync(i => i.ShoppingCartId == cartId && i.ProductId == productId);

            Assert.Null(item);
        }
    }



}
