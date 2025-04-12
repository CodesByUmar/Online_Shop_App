using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineShop.Areas.Admin.Models;
using OnlineShopApp.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _db;

        public RoleController(RoleManager<IdentityRole> roleManager, ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _db = db;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            ViewBag.Roles = _roleManager.Roles.ToList();
            return View();
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Role name cannot be empty.");
                return View();
            }

            if (await _roleManager.RoleExistsAsync(name))
            {
                ModelState.AddModelError("", "This role already exists.");
                return View();
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(name));
            if (result.Succeeded)
            {
                TempData["save"] = "Role has been created successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Role ID is required.");

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            return View(role);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Role name cannot be empty.");
                return View();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            role.Name = name;

            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                TempData["save"] = "Role has been updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(role);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Role ID is required.");

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            return View(role);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                TempData["delete"] = "Role has been deleted successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(role);
        }

        public IActionResult Assign()
        {
            PopulateUserAndRoleViewData();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Assign(RoleUserVm roleUser)
        {
            var user = await _userManager.FindByIdAsync(roleUser.UserId);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                PopulateUserAndRoleViewData();
                return View();
            }

            if (await _userManager.IsInRoleAsync(user, roleUser.RoleId))
            {
                ModelState.AddModelError("", "This user is already assigned to this role.");
                PopulateUserAndRoleViewData();
                return View();
            }

            var result = await _userManager.AddToRoleAsync(user, roleUser.RoleId);
            if (result.Succeeded)
            {
                TempData["save"] = "User role has been assigned.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            PopulateUserAndRoleViewData();
            return View();
        }

        public IActionResult AssignUserRole()
        {
            var result = from ur in _db.UserRoles
                         join r in _db.Roles on ur.RoleId equals r.Id
                         join a in _db.ApplicationUsers on ur.UserId equals a.Id
                         select new UserRoleMaping
                         {
                             UserId = ur.UserId,
                             RoleId = ur.RoleId,
                             UserName = a.UserName,
                             RoleName = r.Name
                         };

            ViewBag.UserRoles = result.ToList();
            return View();
        }

        private void PopulateUserAndRoleViewData()
        {
            ViewData["UserId"] = new SelectList(
                _db.ApplicationUsers.Where(u => u.LockoutEnd == null || u.LockoutEnd < DateTime.Now),
                "Id", "UserName");
            ViewData["RoleId"] = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
        }
    }
}
