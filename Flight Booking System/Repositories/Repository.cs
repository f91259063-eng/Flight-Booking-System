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
        private readonly ILogger<Repository<T>> _logger;

        public Repository(ApplicationDbContext context, ILogger<Repository<T>> logger)
        {
            _context = context;
            _dbset = _context.Set<T>();
            _logger = logger;
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
            catch (DbUpdateException ex)
            {
                // a database problem (for example a record that is still used by another one):
                // write it to the log and tell the caller with -1
                _logger.LogError(ex, "Saving changes of {Entity} failed", typeof(T).Name);
                return -1;
            }
        }

        public async Task<int> CountAsync(
            Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.CountAsync();
        }
    }
    }
