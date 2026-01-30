using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Warehouse.Domain.Identity;
using Warehouse.Web.Models;

namespace Warehouse.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<WarehouseApplicationUser> _userManager;

        public HomeController(UserManager<WarehouseApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task<IActionResult> IndexAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
                return View("Landing"); // optional page for guests

            var user = await _userManager.GetUserAsync(User);

            ViewData["DisplayName"] = !string.IsNullOrWhiteSpace(user?.FirstName)
                ? user!.FirstName
                : user?.UserName;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
