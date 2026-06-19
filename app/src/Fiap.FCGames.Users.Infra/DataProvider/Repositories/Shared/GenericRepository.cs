using Fiap.FCGames.Users.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Fiap.FCGames.Users.Infra.DataProvider.Repositories.Shared;

public abstract class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : class
{
    private readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    protected GenericRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public IQueryable<TEntity> GetAll() => _dbSet;
    public IQueryable<TEntity> Get(Expression<Func<TEntity, bool>> predicate) => _dbSet.Where(predicate);
    public TEntity? Get(params object[] key) => _dbSet.Find(key);
    public void Create(TEntity entity) => _dbSet.Add(entity);
    public void Update(TEntity entity) => _context.Entry(entity).State = EntityState.Modified;
    public void Delete(Func<TEntity, bool> predicate)
        => _dbSet.Where(predicate).ToList().ForEach(e => _dbSet.Remove(e));

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
