using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public IActionResult Index() => View(_unitOfWork.Category.GetAll().ToList());

        public IActionResult Create() => View();
        [HttpPost]
        public IActionResult Create(Category c)
        {
            if (!ModelState.IsValid) return View();
            //if (c.Name == c.DisplayOrder.ToString()) ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the name.");
            //if (c.Name.ToLower() == "test") ModelState.AddModelError("", "Test is an invalid value.");

            _unitOfWork.Category.Add(c);
            _unitOfWork.Save();
            TempData["success"] = "Category created successfully.";
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int? id)
        {
            if (id is null or 0) return NotFound();

            var c = _unitOfWork.Category.Get(u => u.Id == id);
            //var c = _categoryRepository.Categories.Find(id);
            //var c1 = _db.Categories.FirstOrDefault(x => x.Id == id);
            //var c2 = _db.Categories.Where(x => x.Id == id).FirstOrDefault();
            if (c is null) return NotFound();

            return View(c);
        }
        [HttpPost]
        public IActionResult Edit(Category c)
        {
            if (!ModelState.IsValid) return View();

            _unitOfWork.Category.Update(c);
            _unitOfWork.Save();
            TempData["success"] = "Category updated successfully.";
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int? id)
        {
            if (id is null or 0) return NotFound();
            var c = _unitOfWork.Category.Get(u => u.Id == id);
            if (c is null) return NotFound();
            return View(c);
        }
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            var obj = _unitOfWork.Category.Get(u => u.Id == id);
            if (obj is null) return NotFound();
            _unitOfWork.Category.Remove(obj);
            _unitOfWork.Save();
            TempData["success"] = "Category deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}
