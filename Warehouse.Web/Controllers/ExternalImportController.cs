using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Service.Interface;

namespace Warehouse.Web.Controllers;

[Authorize(Roles = "Supplier")] 
public class ExternalImportController : Controller
{
    private readonly IFakeStoreCatalogService _fs;
    private readonly ICategoryService _categories;
    private readonly IProductService _products;
    private readonly IInventoryService _inventory;  

    public ExternalImportController(
        IFakeStoreCatalogService fs,
        ICategoryService categories,
        IProductService products,
        IInventoryService inventory)
    {
        _fs = fs;
        _categories = categories;
        _products = products;
        _inventory = inventory;
    }

    public IActionResult Categories()
    {
        var cats = _fs.GetCategories();
        return View(cats);
    }

    public IActionResult Products(string category)
    {
        if (string.IsNullOrWhiteSpace(category)) return RedirectToAction(nameof(Categories));
        ViewData["category"] = category;
        return View(_fs.GetProductsByCategory(category));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Import(string externalId)
    {
        var item = _fs.GetByExternalId(externalId);
        if (item == null) return NotFound();

        
        var supplierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(supplierId)) return Unauthorized();

        
        var cat = _categories.GetAll()
            .FirstOrDefault(c => c.Name != null &&
                                 c.Name.Equals(item.CategoryName, StringComparison.OrdinalIgnoreCase));

        if (cat == null)
        {
            cat = new Category { Id = Guid.NewGuid(), Name = item.CategoryName };
            _categories.Insert(cat);
        }

        
        var exists = _products.GetAll().Any(p => p.SKU == item.ExternalId);
        if (exists)
        {
            TempData["Info"] = "Already imported.";
            return RedirectToAction(nameof(Categories));
        }

        
        var p = new Product
        {
            Id = Guid.NewGuid(),
            Name = item.Name,
            SKU = item.ExternalId,
            CategoryId = cat.Id,
            ImageURL = item.ImageUrl,
            UnitPrice = item.UnitPrice,
            SupplierId = supplierId 
        };

        _products.Insert(p);

        
        _inventory.SetInitialStock(p.Id, 10, LocationType.Shelves);

        TempData["Success"] = $"Imported: {p.Name} (Stock: 10, Shelves)";
        return RedirectToAction(nameof(Categories));
    }
}
