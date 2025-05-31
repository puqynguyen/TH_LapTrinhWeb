using System.Collections.Generic;
using WebsiteBanHang.Models;

namespace WebsiteBanHang.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);

        // Thêm các phương thức cho ProductImage
        Task<IEnumerable<ProductImage>> GetProductImagesAsync(int productId);
        Task<ProductImage> GetProductImageByIdAsync(int id);
        Task AddProductImageAsync(ProductImage productImage);
        Task DeleteProductImageAsync(int id);
    }
}