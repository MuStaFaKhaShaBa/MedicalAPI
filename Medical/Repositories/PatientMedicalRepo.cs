﻿
using Medical.Data;
using Medical.Data.Entities;
using Medical.Models;
using Medical.Specifications;
using Medical.Specifications.PatientMedical_;
using Microsoft.EntityFrameworkCore;

namespace Medical.Repositories
{
    public class PatientMedicalRepo(ApplicationDbContext dbContext, IConfiguration config)
    {
        private readonly ApplicationDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        private readonly IConfiguration _config = config;

        public async Task AddAsync(PatientMedical pm)
        {
            ArgumentNullException.ThrowIfNull(pm);
            await _dbContext.Set<PatientMedical>().AddAsync(pm);
        }

        public async Task AddAsync(IList<PatientMedical> pms)
        {
            ArgumentNullException.ThrowIfNull(pms);
            await _dbContext.Set<PatientMedical>().AddRangeAsync(pms);
        }

        public async Task<int> CommitAsync()
        => await _dbContext.SaveChangesAsync();

        public async Task<int> CountAsync(PatientMedicalSpecifications spec)
        {
            PatientMedicalSpecifications specsCp = new()
            {
                Criteria = spec.Criteria,
            };
            var query = ApplySpecificationEvaluator(_dbContext.Set<PatientMedical>(), specsCp);
            return await query.CountAsync();
        }

        public void Delete(PatientMedical pm)
        {
            ArgumentNullException.ThrowIfNull(pm);
            _dbContext.Set<PatientMedical>().Remove(pm);
        }

        public async Task<Pagination<PatientMedicalVM>?> GetAllAsync(PatientMedicalSpecifications spec, PaginationSpecs pagination)
        {
            var query = ApplySpecificationEvaluator(_dbContext.Set<PatientMedical>(), spec);
            var entities = await query.ToListAsync();

            var models = entities.Select(x => new PatientMedicalVM(x, Path.Combine(_config["baseUrl"], _config["reports:load"]))).ToList();

            return new()
            {
                Count = CountAsync(spec).Result,
                Page = pagination.PageIndex,
                Size = pagination.PageSize,
                Items = models
            };
        }
        public async Task<IEnumerable<PatientMedicalVM>?> GetAllAsync(PatientMedicalSpecifications spec)
        {
            var query = ApplySpecificationEvaluator(_dbContext.Set<PatientMedical>(), spec);
            var entities = await query.ToListAsync();
            var models = entities.Select(x => new PatientMedicalVM(x, Path.Combine(_config["baseUrl"], _config["reports:load"])));

            return models;
        }

        public async Task<PatientMedical?> GetByIdAsync(int id)
        {
            return await _dbContext.Set<PatientMedical>().FindAsync(id);
        }

        public async Task<PatientMedical?> GetAsync(PatientMedicalSpecifications spec)
        {
            var query = ApplySpecificationEvaluator(_dbContext.Set<PatientMedical>(), spec);
            var model = await query.FirstOrDefaultAsync();
            if (model == null) return null;

            model.FileName = Path.Combine(_config["baseUrl"], _config["reports:load"], model.FileName);
            return model;
        }

        public void Update(PatientMedical pm)
        {
            ArgumentNullException.ThrowIfNull(pm);
            _dbContext.Set<PatientMedical>().Update(pm);
        }

        private IQueryable<PatientMedical> ApplySpecificationEvaluator(IQueryable<PatientMedical> query, PatientMedicalSpecifications specs)
        {
            return PatientMedicalSpecificationsEvaluator.Evaluate(query, specs);
        }
    }
}
