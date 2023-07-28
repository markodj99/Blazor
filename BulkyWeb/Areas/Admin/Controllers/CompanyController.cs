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
using System.Data;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public IActionResult Index() => View(_unitOfWork.Company.GetAll().ToList());

        public IActionResult Upsert(int? id)
        {


            if (id is null or 0) //create
            {
                return View(new Company());
            }
            else //update
            {
                Company c = _unitOfWork.Company.Get(x => x.Id == id);
                return View(c);
            }
        }

        [HttpPost]
        public IActionResult Upsert(Company c)
        {
            if (ModelState.IsValid)
            {
                if (c.Id == 0)
                {
                    _unitOfWork.Company.Add(c);
                    TempData["success"] = "Company created successfully.";
                }
                else
                {
                    _unitOfWork.Company.Update(c);
                    TempData["success"] = "Company updated successfully.";
                }

                _unitOfWork.Save();
                
                return RedirectToAction("Index");
            }
            return View(c);
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            var prodList = _unitOfWork.Company.GetAll().ToList();
            return Json(new { data = prodList});
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var Company = _unitOfWork.Company.Get(u => u.Id == id);
            if (Company == null)
            {
                return Json(new {success = false, message = "Error while deleting"});
            }

            _unitOfWork.Company.Remove(Company);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion
    }
}
