using WebsiteBanHang.Models;

namespace WebsiteBanHang.Repositories
{
	public interface IProductImageRepository
	{
		Task<IEnumerable<ProductImage>> GetAllAsync();
		Task<ProductImage> GetByIdAsync(int id);
		Task<IEnumerable<ProductImage>> GetByProductIdAsync(int productId);
		Task AddAsync(ProductImage productImage);
		Task UpdateAsync(ProductImage productImage);
		Task DeleteAsync(int id);
	}
}