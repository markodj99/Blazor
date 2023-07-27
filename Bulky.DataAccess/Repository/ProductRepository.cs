using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _db;

        public ProductRepository(ApplicationDbContext db) : base(db) => _db = db;

        public void Update(Product p) => _db.Products.Update(p);
        //{
        //    _db.Products.Update(p);
        //    var objFromDb = _db.Products.FirstOrDefault(u => u.Id == p.Id);
        //    if (objFromDb != null)
        //    {
        //        objFromDb.Title = p.Title;
        //        objFromDb.ISBN = p.ISBN;
        //        objFromDb.Price = p.Price;
        //        objFromDb.Price50 = p.Price50;
        //        objFromDb.ListPrice = p.ListPrice;
        //        objFromDb.Price100 = p.Price100;
        //        objFromDb.Description = p.Description;
        //        objFromDb.CategoryId = p.CategoryId;
        //        objFromDb.Author = p.Author;
        //        if (p.ImageUrl != null)
        //        {
        //            objFromDb.ImageUrl = p.ImageUrl;
        //        }
        //    }
        //}
    }
}
