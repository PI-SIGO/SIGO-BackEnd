using Microsoft.EntityFrameworkCore;
using SIGO.Data.Interfaces;

namespace SIGO.Data.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public virtual async Task<IEnumerable<T>> Get()
        {
            return await _dbSet.ToListAsync();
        }


        public async Task<T> GetById(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task Add(T entity)
        {
            await _dbSet.AddAsync(entity);
            await SaveChanges();
        }

        public virtual async Task Update(T entity)
        {
            var entityId = _context.Entry(entity).Property("Id").CurrentValue;
            var existingEntity = await _dbSet.FindAsync(entityId);

            if (existingEntity == null)
                throw new KeyNotFoundException($"Entity with id {entityId} not found.");

            _context.Entry(existingEntity).CurrentValues.SetValues(entity);
            await SaveChanges();
        }

        public async Task Remove(T entity)
        {
            _dbSet.Remove(entity);
            await SaveChanges();
        }

            public async Task<int> SaveChanges()
            {
                return await _context.SaveChangesAsync();
            }
    }
}
