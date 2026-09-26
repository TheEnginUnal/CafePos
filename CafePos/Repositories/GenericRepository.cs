using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CafePos.Data;

namespace CafePos.Repositories
{
    // "T" burada bir Entity'yi (Product, Category vb.) temsil eder
    public class GenericRepository<T> where T : class
    {
        // Tüm listeyi getirir
        public async Task<List<T>> GetAllAsync()
        {
            using (var context = new AppDbContext())
            {
                return await context.Set<T>().ToListAsync();
            }
        }

        // ID'ye göre tek bir kayıt getirir
        public async Task<T> GetByIdAsync(int id)
        {
            using (var context = new AppDbContext())
            {
                return await context.Set<T>().FindAsync(id);
            }
        }

        // Yeni kayıt ekler
        public async Task AddAsync(T entity)
        {
            using (var context = new AppDbContext())
            {
                await context.Set<T>().AddAsync(entity);
                await context.SaveChangesAsync();
            }
        }

        // Mevcut kaydı günceller
        public async Task UpdateAsync(T entity)
        {
            using (var context = new AppDbContext())
            {
                context.Set<T>().Update(entity);
                await context.SaveChangesAsync();
            }
        }

        // Kayıt siler
        public async Task DeleteAsync(T entity)
        {
            using (var context = new AppDbContext())
            {
                context.Set<T>().Remove(entity);
                await context.SaveChangesAsync();
            }
        }
    }
}