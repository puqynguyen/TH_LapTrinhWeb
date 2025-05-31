using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebsiteBanHang.Repositories;
using WebsiteBanHang.Models;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace WebsiteBanHang.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IProductImageRepository _productImageRepository;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IWebHostEnvironment webHostEnvironment,
            IProductImageRepository productImageRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _webHostEnvironment = webHostEnvironment;
            _productImageRepository = productImageRepository;
        }

        // GET: Display list of products
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        // GET: Add product form
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList((System.Collections.IEnumerable)categories, "Id", "Name");
            return View();
        }

        // POST: Add product
        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile mainImage, List<IFormFile> additionalImages)
        {
            if (ModelState.IsValid)
            {
                // Xử lý hình ảnh chính
                if (mainImage != null && mainImage.Length > 0)
                {
                    product.ImageUrl = await SaveImage(mainImage);
                }

                // Lưu sản phẩm trước để có ID
                await _productRepository.AddAsync(product);

                // Xử lý các hình ảnh bổ sung
                if (additionalImages != null && additionalImages.Count > 0)
                {
                    foreach (var image in additionalImages)
                    {
                        if (image != null && image.Length > 0)
                        {
                            var imageUrl = await SaveImage(image);
                            var productImage = new ProductImage
                            {
                                ProductId = product.Id,
                                Url = imageUrl
                            };
                            await _productImageRepository.AddAsync(productImage);
                        }
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        // GET: Display single product
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Lấy tất cả hình ảnh bổ sung của sản phẩm
            var additionalImages = await _productImageRepository.GetByProductIdAsync(id);
            ViewBag.AdditionalImages = additionalImages;

            return View(product);
        }

        // GET: Update product form
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Lấy tất cả hình ảnh bổ sung của sản phẩm
            var additionalImages = await _productImageRepository.GetByProductIdAsync(id);
            ViewBag.AdditionalImages = additionalImages;

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Update product
        [HttpPost]
        public async Task<IActionResult> Update(int id, Product product, IFormFile mainImage, List<IFormFile> additionalImages)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Lấy sản phẩm đã được theo dõi
                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Cập nhật các thuộc tính từ product vào existingProduct
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.CategoryId = product.CategoryId;
                // Cập nhật các thuộc tính khác nếu có

                // Xử lý hình ảnh chính nếu có
                if (mainImage != null && mainImage.Length > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                    {
                        DeleteImage(existingProduct.ImageUrl);
                    }
                    // Lưu ảnh mới
                    existingProduct.ImageUrl = await SaveImage(mainImage);
                }

                // Gọi UpdateAsync với existingProduct
                await _productRepository.UpdateAsync(existingProduct);

                // Xử lý các hình ảnh bổ sung
                if (additionalImages != null && additionalImages.Count > 0)
                {
                    foreach (var image in additionalImages)
                    {
                        if (image != null && image.Length > 0)
                        {
                            var imageUrl = await SaveImage(image);
                            var productImage = new ProductImage
                            {
                                ProductId = existingProduct.Id,
                                Url = imageUrl
                            };
                            await _productImageRepository.AddAsync(productImage);
                        }
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            var additionalImagesData = await _productImageRepository.GetByProductIdAsync(id);
            ViewBag.AdditionalImages = additionalImagesData;

            return View(product);
        }

        // GET: Delete confirmation
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product != null)
            {
                // Xóa hình ảnh chính
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    DeleteImage(product.ImageUrl);
                }

                // Xóa tất cả các hình ảnh phụ
                var additionalImages = await _productImageRepository.GetByProductIdAsync(id);
                foreach (var image in additionalImages)
                {
                    DeleteImage(image.Url);
                    await _productImageRepository.DeleteAsync(image.Id);
                }
            }

            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // Xóa hình ảnh phụ
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _productImageRepository.GetByIdAsync(imageId);
            if (image != null)
            {
                // Xóa file hình ảnh
                DeleteImage(image.Url);

                // Xóa bản ghi trong DB
                await _productImageRepository.DeleteAsync(imageId);

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        // Lưu hình ảnh vào thư mục wwwroot/images
        private async Task<string> SaveImage(IFormFile image)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");

            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Tạo tên file độc nhất để tránh trùng lặp
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/images/products/" + uniqueFileName;
        }

        // Xóa hình ảnh từ thư mục wwwroot
        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            try
            {
                string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }
            catch (Exception)
            {
                // Ghi log nếu cần
            }
        }
    }
}