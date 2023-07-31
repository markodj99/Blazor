using Bulky.Models;
using System.Linq.Expressions;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IApplicationUserRepository : IRepository<ApplicationUser>
    {
        void Update(ApplicationUser au);
    }
}
