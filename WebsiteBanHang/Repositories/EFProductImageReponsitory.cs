using Microsoft.EntityFrameworkCore;
using WebsiteBanHang.Models;

namespace WebsiteBanHang.Repositories
{
	public class EFProductImageRepository : IProductImageRepository
	{
		private readonly ApplicationDbContext _context;

		public EFProductImageRepository(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<ProductImage>> GetAllAsync()
		{
			return await _context.ProductImages.ToListAsync();
		}

		public async Task<ProductImage> GetByIdAsync(int id)
		{
			return await _context.ProductImages.FindAsync(id);
		}

		public async Task<IEnumerable<ProductImage>> GetByProductIdAsync(int productId)
		{
			return await _context.ProductImages
				.Where(pi => pi.ProductId == productId)
				.ToListAsync();
		}

		public async Task AddAsync(ProductImage productImage)
		{
			_context.ProductImages.Add(productImage);
			await _context.SaveChangesAsync();
		}

		public async Task UpdateAsync(ProductImage productImage)
		{
			_context.ProductImages.Update(productImage);
			await _context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int id)
		{
			var productImage = await GetByIdAsync(id);
			if (productImage != null)
			{
				_context.ProductImages.Remove(productImage);
				await _context.SaveChangesAsync();
			}
		}
	}
}