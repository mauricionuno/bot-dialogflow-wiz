using System.Threading.Tasks;

namespace Wiz.ChatBot.Domain.Interfaces.Repository
{
    public interface IDapperReadRepository<TEntity> where TEntity : class
    {
        Task<TEntity> GetByIdAsync(int id);
    }
}
