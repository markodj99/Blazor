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
            if (!ModelState.IsValid) return View();
            //if (c.Name == c.DisplayOrder.ToString()) ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the name.");
            //if (c.Name.ToLower() == "test") ModelState.AddModelError("", "Test is an invalid value.");

            _db.Categories.Add(c);
            _db.SaveChanges();
            TempData["success"] = "Category created successfully.";
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int? id)
        {
            if (id is null or 0) return NotFound();

            var c = _db.Categories.Find(id);
            //var c1 = _db.Categories.FirstOrDefault(x => x.Id == id);
            //var c2 = _db.Categories.Where(x => x.Id == id).FirstOrDefault();
            if (c is null) return NotFound();

            return View(c);
        }
        [HttpPost]
        public IActionResult Edit(Category c)
        {
            if (!ModelState.IsValid) return View();

            _db.Categories.Update(c);
            _db.SaveChanges();
            TempData["success"] = "Category updated successfully.";
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int? id)
        {
            if (id is null or 0) return NotFound();
            var c = _db.Categories.Find(id);
            if (c is null) return NotFound();
            return View(c);
        }
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            var obj = _db.Categories.Find(id);
            if(obj is null) return NotFound();
            _db.Categories.Remove(obj);
            _db.SaveChanges();
            TempData["success"] = "Category deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}
