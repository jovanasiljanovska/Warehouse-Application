using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Warehouse.Domain.Domain;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Repository;
using Warehouse.Repository.Interface;
using Warehouse.Service.Implementation;
using Warehouse.Service.Interface;

namespace Warehouse.Web.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IInventoryService _inventoryService;
        private readonly IUserRepository _userRepository;
        private readonly IFakeStoreCatalogService _fs;

        public ProductsController(IProductService productService, ICategoryService categoryService, IInventoryService inventoryService, IUserRepository userRepository, IFakeStoreCatalogService fs)
        {
            _productService = productService;
            _categoryService = categoryService;
            _inventoryService = inventoryService;
            _userRepository = userRepository;
            _fs = fs;
        }

        // GET: Products
        public IActionResult Index(string search, Guid? categoryId) 
        {
            // 2. Start with all products (as IQueryable or IEnumerable)
            var products = _productService.GetAll().AsEnumerable();

            // 3. Filter by Search if text is provided
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p => (p.Name != null && p.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                                            || (p.SKU != null && p.SKU.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            // 4. Filter by Category if ID is provided
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            return View(products.ToList());
        }


        // GET: Products/Details/5
        public IActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = _productService.GetById(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }


        // GET: Products/Create
        [Authorize(Roles = "Employee")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_categoryService.GetAll(), "Id", "Name");
            ViewData["SupplierId"] = new SelectList(_userRepository.GetUsersByRole("Supplier"), "Id", "CompanyName");
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public IActionResult Create([Bind("Name,SKU,CategoryId,SupplierId,ImageURL,UnitPrice")] Product product,LocationType locationType,int quantity)
        {
            if (quantity <= 0)
                ModelState.AddModelError("quantity", "Please enter initial stock quantity.");


            if (!ModelState.IsValid)
            {
                // (If you're keeping dropdowns, re-load them here. If not, just return View(product))
                ViewData["CategoryId"] = new SelectList(_categoryService.GetAll(), "Id", "Name", product.CategoryId);
                ViewData["SupplierId"] = new SelectList(_userRepository.GetUsersByRole("Supplier"), "Id", "CompanyName", product.SupplierId);
                return View(product);
            }

            product.Id = Guid.NewGuid();
            _productService.Insert(product);

            //  create initial stock record
            _inventoryService.SetInitialStock(product.Id, quantity, locationType);

            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Edit/5
        [Authorize(Roles = "Employee")]
        public IActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = _productService.GetById(id.Value);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_categoryService.GetAll(), "Id", "Name", product.CategoryId);
            ViewData["SupplierId"] = new SelectList(_userRepository.GetUsersByRole("Supplier"), "Id", "CompanyName", product.SupplierId);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public IActionResult Edit(Guid id, [Bind("Name,SKU,CategoryId,SupplierId,ImageURL,UnitPrice,Id")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            _productService.Update(product);
            ViewData["CategoryId"] = new SelectList(_categoryService.GetAll(), "Id", "Name", product.CategoryId);
            ViewData["SupplierId"] = new SelectList(_userRepository.GetUsersByRole("Supplier"), "Id", "CompanyName", product.SupplierId);
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Delete/5
        [Authorize(Roles = "Employee")]
        public IActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = _productService.GetById(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public IActionResult DeleteConfirmed(Guid id)
        {
            _productService.DeleteById(id);
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(Guid id)
        {
            return _productService.GetAll().Any(e => e.Id == id);
        }

        [Authorize(Roles = "Employee")]
        public IActionResult InventoryDashboard()
        {
            return View();
        }

        [Authorize(Roles = "Supplier")]
        public IActionResult ApiCategories()
        {
            var cats = _fs.GetCategories();
            return View(cats); // Views/Products/ApiCategories.cshtml
        }

        [Authorize(Roles = "Supplier")]
        public IActionResult ApiProducts(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return RedirectToAction(nameof(ApiCategories));

            ViewData["ApiCategory"] = category;
            var items = _fs.GetProductsByCategory(category);
            return View(items); // Views/Products/ApiProducts.cshtml
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="Supplier")]
        public IActionResult Import(string externalId)
        {
            var item = _fs.GetByExternalId(externalId);
            if (item == null) return NotFound();

            // ✅ SupplierId = logged in supplier
            var supplierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(supplierId)) return Unauthorized();

            // ✅ Category: find or create (опција 1 - во контролер)
            var cat = _categoryService.GetAll()
                .FirstOrDefault(c => c.Name != null &&
                                     c.Name.Equals(item.CategoryName, StringComparison.OrdinalIgnoreCase));

            if (cat == null)
            {
                cat = new Category { Id = Guid.NewGuid(), Name = item.CategoryName };
                _categoryService.Insert(cat);
            }

            // ✅ Avoid duplicates by SKU = external id
            var exists = _productService.GetAll().Any(p => p.SKU == item.ExternalId);
            if (exists)
            {
                TempData["Info"] = "Already imported.";
                return RedirectToAction(nameof(_productService));
            }

            // ✅ Create product with supplier
            var p = new Product
            {
                Id = Guid.NewGuid(),
                Name = item.Name,
                SKU = item.ExternalId,
                CategoryId = cat.Id,
                ImageURL = item.ImageUrl,
                UnitPrice = item.UnitPrice,
                SupplierId = supplierId //  ова ќе ја пополни Supplier навигацијата
            };

            _productService.Insert(p);

            // ✅ Initial stock = 10, initial location = Shelves
            _inventoryService.SetInitialStock(p.Id, 10, LocationType.Shelves);

            TempData["Success"] = $"Imported: {p.Name} (Stock: 10, Shelves)";
            return RedirectToAction("ApiCategories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Supplier")]
        public IActionResult ImportAll(string apiCategory)
        {
            if (string.IsNullOrWhiteSpace(apiCategory)) return BadRequest("Category name is required.");

            // 1. Get all items from the external service for this category
            var items = _fs.GetProductsByCategory(apiCategory);
            if (items == null || !items.Any())
            {
                TempData["Info"] = "No items found in the external catalog for this category.";
                return RedirectToAction(nameof(ApiCategories));
            }

            // 2. Identify the logged-in supplier
            var supplierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(supplierId)) return Unauthorized();

            int importedCount = 0;
            int skippedCount = 0;

            foreach (var item in items)
            {
                // 3. Avoid duplicates by SKU = external id
                var exists = _productService.GetAll().Any(p => p.SKU == item.ExternalId);
                if (exists)
                {
                    skippedCount++;
                    continue;
                }

                // 4. Category: find or create
                var cat = _categoryService.GetAll()
                    .FirstOrDefault(c => c.Name != null &&
                                         c.Name.Equals(item.CategoryName, StringComparison.OrdinalIgnoreCase));

                if (cat == null)
                {
                    cat = new Category { Id = Guid.NewGuid(), Name = item.CategoryName };
                    _categoryService.Insert(cat);
                }

                // 5. Create product
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

                _productService.Insert(p);

                // 6. Initial stock setup
                _inventoryService.SetInitialStock(p.Id, 10, LocationType.Shelves);

                importedCount++;
            }

            // 7. Feedback to user
            if (importedCount > 0)
            {
                TempData["Success"] = $"Successfully imported {importedCount} items to {apiCategory}. (Skipped {skippedCount} duplicates)";
            }
            else
            {
                TempData["Info"] = $"No new items were imported. All {skippedCount} items already exist in your inventory.";
            }

            return RedirectToAction(nameof(ApiCategories));
        }

    }
}
