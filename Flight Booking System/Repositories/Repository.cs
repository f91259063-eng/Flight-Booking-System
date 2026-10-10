using Flight_Booking_System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace Flight_Booking_System.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context; // = new ApplicationDbContext();
        public DbSet<T> _dbset;
        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbset = _context.Set<T>();
        }


        public async Task<EntityEntry<T>> InsertAsync(T entity)
        {
            return await _dbset.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbset.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbset.Remove(entity);
        }

        private IQueryable<T> Query(

            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>[]? includes = null,
            bool IsTracking = true

            )
        {
            var entities = _dbset.AsQueryable();
            if (filter != null)
            {
                entities = entities.Where(filter);
            }
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    entities = entities.Include(include);
                }
            }

            if (!IsTracking)
            {
                entities = entities.AsNoTracking();
            }
            return entities;
        }

        public async Task<IEnumerable<T>> GetAllAsync(

            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>[]? includes = null,
            bool IsTracking = true

            )
        {
            var entities = Query(filter, includes, IsTracking);
            return await entities.ToListAsync();
        }
        public async Task<T> GetOneAsync(

            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>[]? includes = null,
            bool IsTracking = true

            )
        {
            var entities = Query(filter, includes, IsTracking);
            return await entities.FirstOrDefaultAsync();
        }
        public async Task<int> CommitAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return -1; // Return -1 to indicate an error occurred
            }
        }
    }
}
