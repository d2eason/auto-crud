using System.Threading;
using System.Threading.Tasks;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.EntityFramework.Interfaces;

namespace Firebend.AutoCrud.EntityFramework.Abstractions.Client
{
    public abstract class DbContextProvider<TKey, TEntity, TContext> : IDbContextProvider<TKey, TEntity>
        where TKey : struct
        where TEntity : IEntity<TKey>
        where TContext : IDbContext
    {
        private readonly TContext _context;

        public DbContextProvider(TContext context)
        {
            _context = context;
        }

        private IDbContext GetDbContext() => _context;

        public Task<IDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(GetDbContext());
    }
}