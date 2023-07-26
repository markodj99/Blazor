using BulkyWeb.Data;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoryController(ApplicationDbContext db) => _db = db;

        public IActionResult Index() => View(_db.Categories.ToList());

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(Category c)
        {
            if(c.Name == c.DisplayOrder.ToString()) ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the name.");
            if (!ModelState.IsValid) return View();
            _db.Categories.Add(c);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
