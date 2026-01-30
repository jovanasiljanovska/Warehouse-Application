using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using Warehouse.Domain.Domain.Enums;
using Warehouse.Service.Interface;

namespace Warehouse.Web.Controllers
{
    [Authorize] // must be logged in
    public class CustomerOrdersController : Controller
    {
        private readonly ICustomerOrderService _orderService;
        private readonly IProductService _productService;

        public CustomerOrdersController(ICustomerOrderService orderService, IProductService productService)
        {
            _orderService = orderService;
            _productService = productService;
        }

        // =========================
        // CUSTOMER SIDE
        // =========================

        [Authorize(Roles = "Customer")]
        public IActionResult Index()
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(customerId)) return Unauthorized();

            return View(_orderService.GetOrdersForCustomer(customerId));
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Details(Guid? id)
        {
            if (id == null) return NotFound();

            var order = _orderService.GetById(id.Value);
            if (order == null) return NotFound();

            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!order.CustomerId.Equals(customerId)) return Forbid();

            return View(order);
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Create(Guid? productId)
        {
            if (productId == null) return NotFound();

            var product = _productService.GetById(productId.Value);
            if (product == null) return NotFound();

            ViewData["ProductId"] = product.Id;
            ViewData["ProductName"] = product.Name;

            return View();
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Guid productId, int quantity)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(customerId)) return Unauthorized();

            if (quantity <= 0)
            {
                var product = _productService.GetById(productId);
                ViewData["ProductId"] = productId;
                ViewData["ProductName"] = product?.Name ?? "Unknown";
                ModelState.AddModelError("", "Quantity must be greater than 0.");
                return View();
            }

            try
            {
                var order = _orderService.CreateOrder(customerId, productId, quantity);
                return RedirectToAction(nameof(Details), new { id = order.Id });
            }
            catch (Exception ex)
            {
                var product = _productService.GetById(productId);
                ViewData["ProductId"] = productId;
                ViewData["ProductName"] = product?.Name ?? "Unknown";
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        //[Authorize(Roles = "Customer")]
        //public IActionResult Cancel(Guid? id)
        //{
        //    if (id == null) return NotFound();

        //    var order = _orderService.GetById(id.Value);
        //    if (order == null) return NotFound();

        //    var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    if (order.CustomerId != customerId) return Forbid();

        //    return View(order);
        //}

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(Guid id)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(customerId)) return Unauthorized();

            var order = _orderService.GetById(id);
            if (order == null) return NotFound();
            if (order.CustomerId != customerId) return Forbid();

            try
            {
                _orderService.CancelOrder(id, customerId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Cancel", order);
            }
        }

        // =========================
        // EMPLOYEE SIDE
        // =========================

        [Authorize(Roles = "Employee")]
        public IActionResult EmployeeIndex()
        {
            var incoming = _orderService.GetAll()
                                        .Where(o => o.Status == OrderStatus.Ordered)
                                        .ToList();
            return View(incoming);
        }

        [Authorize(Roles = "Employee")]
        public IActionResult Finished()
        {
            var finished = _orderService.GetAll()
                                        .Where(o => o.Status == OrderStatus.Shipped || o.Status == OrderStatus.Cancelled)
                                        .ToList();
            return View(finished);
        }

        [Authorize(Roles = "Employee")]
        public IActionResult EmployeeDetails(Guid? id)
        {
            if (id == null) return NotFound();

            var order = _orderService.GetById(id.Value);
            if (order == null) return NotFound();

            return View(order);
        }

        

        [Authorize(Roles = "Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ship(Guid id)
        {
            var employeeId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(employeeId)) return Unauthorized();

            try
            {
                _orderService.ShipOrder(id, employeeId);
                return RedirectToAction(nameof(EmployeeDetails), new { id });
            }
            catch (Exception ex)
            {
                var order = _orderService.GetById(id);
                if (order == null) return NotFound();

                ModelState.AddModelError("", ex.Message);
                return View("Ship", order);
            }
        }
    }
}
