
using Medical.Data;
using Medical.Data.Entities;
using Medical.Models;
using Medical.Specifications;
using Medical.Specifications.ApplicationUser_;
using Microsoft.EntityFrameworkCore;

namespace Medical.Repositories
{
    public class ApplicationUserRepo(ApplicationDbContext dbContext, IConfiguration config)
    {
        private readonly ApplicationDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        private readonly IConfiguration _config = config;

        public async Task AddAsync(ApplicationUser user)
        {
            ArgumentNullException.ThrowIfNull(user);
            await _dbContext.Users.AddAsync(user);
        }

        public async Task AddAsync(IList<ApplicationUser> users)
        {
            ArgumentNullException.ThrowIfNull(users);
            await _dbContext.Users.AddRangeAsync(users);
        }

        public async Task<int> CommitAsync()
        => await _dbContext.SaveChangesAsync();

        public async Task<int> CountAsync(ApplicationUserSpecifications spec)
        {
            ApplicationUserSpecifications specsCp = new()
            {
                Criteria = spec.Criteria,
            };
            var query = ApplySpecificationEvaluator(_dbContext.Users, specsCp);
            return await query.CountAsync();
        }

        public void Delete(ApplicationUser user)
        {
            ArgumentNullException.ThrowIfNull(user);
            _dbContext.Users.Remove(user);
        }

        public async Task<Pagination<ApplicationUserVM>?> GetAllAsync(ApplicationUserSpecifications spec, PaginationSpecs pagination)
        {
            var query = ApplySpecificationEvaluator(_dbContext.Users, spec);
            var entities = await query.ToListAsync();
            var modelEntities = entities.Select(x => new ApplicationUserVM(x, Path.Combine(_config["baseUrl"], _config["images:load"])));
            return new()
            {
                Count = CountAsync(spec).Result,
                Page = pagination.PageIndex,
                Size = pagination.PageSize,
                Items = modelEntities
            };
        }
        public async Task<IEnumerable<ApplicationUserVM>?> GetAllAsync(ApplicationUserSpecifications spec)
        {
            var query = ApplySpecificationEvaluator(_dbContext.Users, spec);
            var entities = await query.ToListAsync();
            var modelEntities = entities.Select(x => new ApplicationUserVM(x, Path.Combine(_config["baseUrl"], _config["images:load"])));

            return modelEntities;
        }

        public async Task<ApplicationUserVM?> GetByIdAsync(int id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null) return null;

            return new(user, Path.Combine(_config["baseUrl"], _config["images:load"]));
        }

        public async Task<ApplicationUserVM?> GetAsync(ApplicationUserSpecifications spec)
        {
            var query = ApplySpecificationEvaluator(_dbContext.Users, spec);
            var user = await query.FirstOrDefaultAsync();
            if (user == null) return null;

            return new(user, Path.Combine(_config["baseUrl"], _config["images:load"]));
        }

        public async Task<ApplicationUser?> GetPatientAsync(ApplicationUserSpecifications spec)
        {
            var query = ApplySpecificationEvaluator(_dbContext.Users, spec);
            var user = await query.FirstOrDefaultAsync();
            if (user == null) return null;
            user.ImagePath = Path.Combine(_config["baseUrl"], _config["images:load"], user.ImagePath);
            user.QR = Path.Combine(_config["baseUrl"], _config["images:load"], user.QR);

            return user;
        }

        public void Update(ApplicationUser user)
        {
            ArgumentNullException.ThrowIfNull(user);
            _dbContext.Users.Update(user);
        }

        private IQueryable<ApplicationUser> ApplySpecificationEvaluator(IQueryable<ApplicationUser> query, ApplicationUserSpecifications specs)
        {
            return ApplicationUserSpecificationsEvaluator.Evaluate(query, specs);
        }
    }
}
