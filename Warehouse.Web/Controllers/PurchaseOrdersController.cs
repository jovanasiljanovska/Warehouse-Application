using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Repository.Interface;
using Warehouse.Service.Interface;

namespace Warehouse.Web.Controllers
{
    public class PurchaseOrdersController : Controller
    {
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IProductService _productService;
        private readonly IUserRepository _userRepository;

        public PurchaseOrdersController(
            IPurchaseOrderService purchaseOrderService,
            IProductService productService,
            IUserRepository userRepository)
        {
            _purchaseOrderService = purchaseOrderService;
            _productService = productService;
            _userRepository = userRepository;
        }

        // =========================
        // EMPLOYEE SIDE
        // =========================

        [Authorize(Roles = "Employee")]
        public IActionResult Index()
        {
            return View(_purchaseOrderService.GetAll());
        }

        [Authorize(Roles = "Employee")]
        public IActionResult Details(Guid? id)
        {
            if (id == null) return NotFound();

            var po = _purchaseOrderService.GetById(id.Value);
            if (po == null) return NotFound();

            return View(po);
        }

        [Authorize(Roles = "Employee")]
        public IActionResult Create(Guid? productId)
        {
            var products = _productService.GetAll();

            ViewBag.IsRestockMode = productId.HasValue;
            ViewBag.SelectedProductId = productId;

            ViewData["ProductId"] = new SelectList(products, "Id", "Name", productId);

            return View();
        }



        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Guid ProductId, int Quantity)
        {
            var employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(employeeId)) return Unauthorized();

            if (ProductId == Guid.Empty)
                ModelState.AddModelError("ProductId", "Please select a product.");

            if (Quantity <= 0)
                ModelState.AddModelError("Quantity", "Quantity must be greater than 0.");

            // SupplierId се зема од продуктот
            string? supplierId = null;
            if (ProductId != Guid.Empty)
            {
                var product = _productService.GetById(ProductId);
                supplierId = product?.SupplierId;

                if (string.IsNullOrWhiteSpace(supplierId))
                    ModelState.AddModelError("ProductId", "Selected product has no supplier assigned.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.IsRestockMode = false; // ако има грешки, третирај како generic
                ViewData["ProductId"] = new SelectList(_productService.GetAll(), "Id", "Name", ProductId);
                return View();
            }

            var po = _purchaseOrderService.CreatePurchaseOrder(employeeId, supplierId!, ProductId, Quantity);
            return RedirectToAction(nameof(Details), new { id = po.Id });
        }


        // 2. HELPER METHOD TO RE-FILL VIEW DATA
        private void PrepareViewData(Guid productId, string supplierId)
        {
            ViewData["ProductId"] = new SelectList(_productService.GetAll(), "Id", "Name", productId);

            var suppliers = _userRepository.GetUsersByRole("Supplier");
            ViewData["SupplierId"] = new SelectList(
                suppliers.Select(s => new {
                    s.Id,
                    Display = !string.IsNullOrWhiteSpace(s.Email) ? s.Email : s.UserName
                }),
                "Id", "Display", supplierId
            );

        }

        [Authorize(Roles = "Employee")]
        public IActionResult Receive(Guid? id)
        {
            if (id == null) return NotFound();

            var po = _purchaseOrderService.GetById(id.Value);
            if (po == null) return NotFound();

            return View(po);
        }

        [Authorize(Roles = "Employee")]
        [HttpPost, ActionName("Receive")]
        [ValidateAntiForgeryToken]
        public IActionResult ReceiveConfirmed(Guid id, LocationType targetLocation)
        {
            var employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(employeeId)) return Unauthorized();

            // Only allow real storage targets
            if (targetLocation != LocationType.Shelves &&
                targetLocation != LocationType.Freezer)
            {
                ModelState.AddModelError("", "Please select Shelves, Freezer, or Quarantine.");
                var po = _purchaseOrderService.GetById(id);
                return View(po);
            }

            try
            {
                _purchaseOrderService.Receive(id, employeeId, targetLocation);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                var po = _purchaseOrderService.GetById(id);
                if (po == null) return NotFound();

                ModelState.AddModelError("", ex.Message);
                return View(po);
            }
        }

        // =========================
        // SUPPLIER SIDE
        // =========================

        [Authorize(Roles = "Supplier")]
        public IActionResult SupplierIndex()
        {
            var supplierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(supplierId)) return Unauthorized();

            return View(_purchaseOrderService.GetIncomingForSupplier(supplierId));
        }

        [Authorize(Roles = "Supplier")]
        public IActionResult SupplierDetails(Guid? id)
        {
            if (id == null) return NotFound();

            var supplierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(supplierId)) return Unauthorized();

            var po = _purchaseOrderService.GetById(id.Value);
            if (po == null) return NotFound();

            // ✅ ownership check
            if (po.SupplierId != supplierId) return Forbid();

            return View(po);
        }

        [Authorize(Roles = "Supplier")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Accept(Guid id)
        {
            var supplierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(supplierId)) return Unauthorized();

            try
            {
                _purchaseOrderService.Accept(id, supplierId);
                return RedirectToAction(nameof(SupplierDetails), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(SupplierDetails), new { id });
            }
        }

        [Authorize(Roles = "Supplier")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ship(Guid id)
        {
            var supplierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(supplierId)) return Unauthorized();

            try
            {
                _purchaseOrderService.Ship(id, supplierId);
                return RedirectToAction(nameof(SupplierDetails), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(SupplierDetails), new { id });
            }
        }

        [Authorize(Roles = "Supplier")]
        public IActionResult Finished()
        {
            var supplierId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(supplierId)) return Unauthorized();

            var finished = _purchaseOrderService.GetAll()
                .Where(o => o.SupplierId == supplierId && (o.Status == OrderStatus.Shipped || o.Status == OrderStatus.Received))
                .ToList();

            return View(finished);
        }

    }
}
