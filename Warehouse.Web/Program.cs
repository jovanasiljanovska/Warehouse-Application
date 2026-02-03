using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain.Identity;
using Warehouse.Repository;
using Warehouse.Repository.Implementation;
using Warehouse.Repository.Interface;
using Warehouse.Service.Implementation;
using Warehouse.Service.Interface;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlite(connectionString));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//  IMPORTANT: use WarehouseApplicationUser + add Roles
builder.Services.AddDefaultIdentity<WarehouseApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

//  IMPORTANT: enable MVC controllers + views
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IUserRepository), typeof(UserRepository));
builder.Services.AddScoped<IStockBalanceRepository, StockBalanceRepository>();

// services
builder.Services.AddTransient<IProductService, ProductService>();
builder.Services.AddTransient<ICategoryService, CategoryService>();
builder.Services.AddTransient<IInventoryService, InventoryService>();
builder.Services.AddTransient<ICustomerOrderService, CustomerOrderService>();
builder.Services.AddTransient<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddTransient<ICartService, CartService>();

builder.Services.AddHttpClient<IFakeStoreCatalogService, FakeStoreCatalogService>();



var app = builder.Build();

//  Role seeding works now because AddRoles<IdentityRole>() exists
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Customer", "Employee", "Supplier" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();   //  you should have this for css/js
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<Warehouse.Repository.ApplicationDbContext>();
        // This will create the database and all tables (like AspNetRoles) if they are missing
        context.Database.Migrate(); 
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
