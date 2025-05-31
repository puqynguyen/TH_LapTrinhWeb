using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebsiteBanHang.Repositories;
using WebsiteBanHang.Models;
using System.IO;

namespace WebsiteBanHang.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IWebHostEnvironment hostEnvironment)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _hostEnvironment = hostEnvironment;
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
            return View(new ProductViewModel());
        }

        // POST: Add product
        [HttpPost]
        public async Task<IActionResult> Add(ProductViewModel productVM)
        {
            if (ModelState.IsValid)
            {
                // Tạo Product từ ProductViewModel
                var product = new Product
                {
                    Name = productVM.Name,
                    Price = productVM.Price,
                    Description = productVM.Description,
                    CategoryId = productVM.CategoryId
                };

                // Xử lý hình ảnh chính
                if (productVM.MainImage != null)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(productVM.MainImage.FileName);
                    string productPath = Path.Combine(wwwRootPath, "images", "products");

                    // Tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(productPath))
                    {
                        Directory.CreateDirectory(productPath);
                    }

                    // Lưu hình ảnh vào thư mục
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        await productVM.MainImage.CopyToAsync(fileStream);
                    }

                    // Cập nhật đường dẫn ảnh cho sản phẩm
                    product.ImageUrl = "/images/products/" + fileName;
                }

                // Thêm sản phẩm vào database để lấy Id
                await _productRepository.AddAsync(product);

                // Xử lý các hình ảnh bổ sung
                if (productVM.AdditionalImages != null && productVM.AdditionalImages.Count > 0)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string productPath = Path.Combine(wwwRootPath, "images", "products");

                    // Danh sách các hình ảnh bổ sung
                    var productImages = new List<ProductImage>();

                    foreach (var image in productVM.AdditionalImages)
                    {
                        if (image != null && image.Length > 0)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

                            // Lưu hình ảnh vào thư mục
                            using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                            {
                                await image.CopyToAsync(fileStream);
                            }

                            // Thêm vào danh sách hình ảnh bổ sung
                            productImages.Add(new ProductImage
                            {
                                ProductId = product.Id,
                                Url = "/images/products/" + fileName
                            });
                        }
                    }

                    // Lưu các hình ảnh bổ sung vào database
                    if (productImages.Count > 0)
                    {
                        foreach (var img in productImages)
                        {
                            await _productRepository.AddProductImageAsync(img);
                        }
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(productVM);
        }

        // GET: Display single product
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Lấy các hình ảnh bổ sung của sản phẩm
            var additionalImages = await _productRepository.GetProductImagesAsync(id);
            product.ImageUrls = additionalImages.ToList();

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

            // Lấy các hình ảnh bổ sung của sản phẩm
            var additionalImages = await _productRepository.GetProductImagesAsync(id);
            product.ImageUrls = additionalImages.ToList();

            var productVM = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                CategoryId = product.CategoryId,
                ImageUrl = product.ImageUrl
            };

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            ViewBag.AdditionalImages = product.ImageUrls;

            return View(productVM);
        }

        // POST: Update product
        [HttpPost]
        public async Task<IActionResult> Update(int id, ProductViewModel productVM)
        {
            if (id != productVM.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Lấy sản phẩm hiện tại từ database
                var product = await _productRepository.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin sản phẩm
                product.Name = productVM.Name;
                product.Price = productVM.Price;
                product.Description = productVM.Description;
                product.CategoryId = productVM.CategoryId;

                // Xử lý hình ảnh chính
                if (productVM.MainImage != null)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;

                    // Xóa hình ảnh cũ (nếu có)
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Lưu hình ảnh mới
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(productVM.MainImage.FileName);
                    string productPath = Path.Combine(wwwRootPath, "images", "products");

                    // Tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(productPath))
                    {
                        Directory.CreateDirectory(productPath);
                    }

                    // Lưu hình ảnh vào thư mục
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        await productVM.MainImage.CopyToAsync(fileStream);
                    }

                    // Cập nhật đường dẫn ảnh cho sản phẩm
                    product.ImageUrl = "/images/products/" + fileName;
                }

                // Cập nhật sản phẩm vào database
                await _productRepository.UpdateAsync(product);

                // Xử lý các hình ảnh bổ sung
                if (productVM.AdditionalImages != null && productVM.AdditionalImages.Count > 0)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string productPath = Path.Combine(wwwRootPath, "images", "products");

                    // Danh sách các hình ảnh bổ sung
                    var productImages = new List<ProductImage>();

                    foreach (var image in productVM.AdditionalImages)
                    {
                        if (image != null && image.Length > 0)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

                            // Lưu hình ảnh vào thư mục
                            using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                            {
                                await image.CopyToAsync(fileStream);
                            }

                            // Thêm vào danh sách hình ảnh bổ sung
                            productImages.Add(new ProductImage
                            {
                                ProductId = product.Id,
                                Url = "/images/products/" + fileName
                            });
                        }
                    }

                    // Lưu các hình ảnh bổ sung vào database
                    if (productImages.Count > 0)
                    {
                        foreach (var img in productImages)
                        {
                            await _productRepository.AddProductImageAsync(img);
                        }
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", productVM.CategoryId);
            return View(productVM);
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

        // POST: Delete product
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product != null)
            {
                // Xóa hình ảnh chính nếu có
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string imagePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // Lấy và xóa các hình ảnh bổ sung
                var additionalImages = await _productRepository.GetProductImagesAsync(id);
                foreach (var image in additionalImages)
                {
                    // Xóa file hình ảnh
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string imagePath = Path.Combine(wwwRootPath, image.Url.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }

                    // Xóa record trong database
                    await _productRepository.DeleteProductImageAsync(image.Id);
                }

                // Xóa sản phẩm
                await _productRepository.DeleteAsync(id);
            }

            return RedirectToAction(nameof(Index));
        }

        // DELETE: Xóa một hình ảnh bổ sung
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var image = await _productRepository.GetProductImageByIdAsync(id);
            if (image != null)
            {
                // Xóa file hình ảnh
                string wwwRootPath = _hostEnvironment.WebRootPath;
                string imagePath = Path.Combine(wwwRootPath, image.Url.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                // Xóa record trong database
                await _productRepository.DeleteProductImageAsync(id);

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }
    }
}