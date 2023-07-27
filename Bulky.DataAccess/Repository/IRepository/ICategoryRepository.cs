using Bulky.Models;
using System.Linq.Expressions;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface ICategoryRepository : IRepository<Category>
    {
        void Update(Category c);
    }
}
