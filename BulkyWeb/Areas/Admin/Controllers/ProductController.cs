using Bulky.DataAccess.Data;
using Bulky.DataAccess.Migrations;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
//using Stripe;
using System.Data;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index() => View(_unitOfWork.Product.GetAll(includeProperties:"Category").ToList());

        public IActionResult Upsert(int? id)
        {
            IEnumerable<SelectListItem> categoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem()
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
            //ViewBag.CategoryList = categoryList;
            //ViewData["CategoryList"] = categoryList;
            var pvm = new ProductVM()
            {
                CategoryList = categoryList,
                Product = new Product()
            };

            if (id is null or 0) //create
            {
                return View(pvm);
            }
            else //update
            {
                pvm.Product = _unitOfWork.Product.Get(x => x.Id == id, includeProperties:"ProductImages");
                return View(pvm);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM p, List<IFormFile> files)
        {
            if (ModelState.IsValid)
            {
                if (p.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(p.Product);
                    TempData["success"] = "Product created successfully.";
                }
                else
                {
                    _unitOfWork.Product.Update(p.Product);
                    TempData["success"] = "Product updated successfully.";
                }

                _unitOfWork.Save();

                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (files is not null)
                {
                    foreach (var file in files)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"images\product\product-" + p.Product.Id;
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        if (!Directory.Exists(finalPath)) Directory.CreateDirectory(finalPath);

                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        var productImage = new ProductImage()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = p.Product.Id,
                        };

                        if (p.Product.ProductImages == null) p.Product.ProductImages = new List<ProductImage>();

                        p.Product.ProductImages.Add(productImage);
                    }

                    _unitOfWork.Product.Update(p.Product);
                    _unitOfWork.Save();
                }

                return RedirectToAction("Index");
            }
            p.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem()
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
            return View(p);
        }

        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDeleted = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
            int productId = imageToBeDeleted.ProductId;
            if (imageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                    var oldImagePath =
                        Path.Combine(_webHostEnvironment.WebRootPath,
                            imageToBeDeleted.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                _unitOfWork.ProductImage.Remove(imageToBeDeleted);
                _unitOfWork.Save();

                TempData["success"] = "Deleted successfully";
            }

            return RedirectToAction(nameof(Upsert), new { id = productId });
        }

        //public IActionResult Delete(int? id)
        //{
        //    if (id is null or 0) return NotFound();
        //    var p = _unitOfWork.Product.Get(u => u.Id == id);
        //    if (p is null) return NotFound();
        //    return View(p);
        //}
        //[HttpPost, ActionName("Delete")]
        //public IActionResult DeletePost(int? id)
        //{
        //    var p = _unitOfWork.Product.Get(u => u.Id == id);
        //    if (p is null) return NotFound();
        //    _unitOfWork.Product.Remove(p);
        //    _unitOfWork.Save();
        //    TempData["success"] = "Product deleted successfully.";
        //    return RedirectToAction("Index");
        //}

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            var prodList = _unitOfWork.Product.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = prodList});
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var product = _unitOfWork.Product.Get(u => u.Id == id);
            if (product == null)
            {
                return Json(new {success = false, message = "Error while deleting"});
            }

            string productPath = @"images\product\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

            if (Directory.Exists(finalPath))
            {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach (string filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }

                Directory.Delete(finalPath);
            }

            _unitOfWork.Product.Remove(product);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion
    }
}
