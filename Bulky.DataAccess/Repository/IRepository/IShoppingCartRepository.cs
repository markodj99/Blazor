using Bulky.Models;
using System.Linq.Expressions;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IShoppingCartRepository : IRepository<ShoppingCart>
    {
        void Update(ShoppingCart sc);
    }
}
