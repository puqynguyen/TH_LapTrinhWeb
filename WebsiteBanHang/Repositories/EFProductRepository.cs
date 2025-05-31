using WebsiteBanHang.Models;
using Microsoft.EntityFrameworkCore;

namespace WebsiteBanHang.Repositories
{
	public class EFProductRepository : IProductRepository
	{
		private readonly ApplicationDbContext _context;

		public EFProductRepository(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<Product>> GetAllAsync()
		{
			// Thêm Include để load thông tin Category
			return await _context.Products
				.Include(p => p.Category)
				.ToListAsync();
		}

		public async Task<Product> GetByIdAsync(int id)
		{
			// Cũng thêm Include cho GetByIdAsync để có thể hiển thị category trong Display view
			return await _context.Products
				.Include(p => p.Category)
				.FirstOrDefaultAsync(p => p.Id == id);
		}

		public async Task AddAsync(Product product)
		{
			_context.Products.Add(product);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(Product product)
		{
			_context.Products.Update(product);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var product = await GetByIdAsync(id);
			if (product != null)
			{
				_context.Products.Remove(product);
				await _context.SaveChangesAsync();
			}
		}
	}
}