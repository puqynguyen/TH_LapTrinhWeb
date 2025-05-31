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
		private readonly ILogger<ProductController> _logger;

		public ProductController(
			IProductRepository productRepository,
			ICategoryRepository categoryRepository,
			IWebHostEnvironment hostEnvironment,
			ILogger<ProductController> logger)
		{
			_productRepository = productRepository;
			_categoryRepository = categoryRepository;
			_hostEnvironment = hostEnvironment;
			_logger = logger;
		}

		// GET: Display list of products
		public async Task<IActionResult> Index()
		{
			try
			{
				var products = await _productRepository.GetAllAsync();
				return View(products);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting products");
				TempData["Error"] = "Có lỗi xảy ra khi tải danh sách sản phẩm";
				return View(new List<Product>());
			}
		}

		// GET: Add product form
		public async Task<IActionResult> Add()
		{
			try
			{
				var categories = await _categoryRepository.GetAllAsync();
				ViewBag.Categories = new SelectList(categories, "Id", "Name");
				return View(new ProductViewModel());
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading add product page");
				TempData["Error"] = "Có lỗi xảy ra khi tải trang thêm sản phẩm";
				return RedirectToAction(nameof(Index));
			}
		}

		// POST: Add product
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Add(ProductViewModel productVM)
		{
			_logger.LogInformation("Adding product: {Name}", productVM?.Name ?? "NULL");

			try
			{
				// Kiểm tra null trước tiên
				if (productVM == null)
				{
					_logger.LogWarning("ProductViewModel is null in Add method");
					TempData["Error"] = "Dữ liệu sản phẩm không hợp lệ";

					// Reload categories và return view mới
					var categoriesForError = await _categoryRepository.GetAllAsync();
					ViewBag.Categories = new SelectList(categoriesForError, "Id", "Name");
					return View(new ProductViewModel());
				}

				// Validate required fields manually nếu cần
				if (string.IsNullOrWhiteSpace(productVM.Name))
				{
					ModelState.AddModelError("Name", "Tên sản phẩm là bắt buộc");
				}

				if (productVM.Price <= 0)
				{
					ModelState.AddModelError("Price", "Giá sản phẩm phải lớn hơn 0");
				}

				if (productVM.CategoryId <= 0)
				{
					ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục");
				}

				if (ModelState.IsValid)
				{
					// Tạo Product từ ProductViewModel
					var product = new Product
					{
						Name = productVM.Name?.Trim() ?? string.Empty,
						Price = productVM.Price,
						Description = productVM.Description?.Trim() ?? string.Empty,
						CategoryId = productVM.CategoryId
					};

					// Xử lý hình ảnh chính
					if (productVM.MainImage != null && productVM.MainImage.Length > 0)
					{
						var mainImagePath = await SaveImageAsync(productVM.MainImage);
						if (!string.IsNullOrEmpty(mainImagePath))
						{
							product.ImageUrl = mainImagePath;
						}
					}

					// Thêm sản phẩm vào database để lấy Id
					await _productRepository.AddAsync(product);
					_logger.LogInformation("Product added with ID: {Id}", product.Id);

					// Xử lý các hình ảnh bổ sung
					await ProcessAdditionalImagesAsync(productVM.AdditionalImages, product.Id);

					TempData["Success"] = "Thêm sản phẩm thành công!";
					return RedirectToAction(nameof(Index));
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error adding product");
				ModelState.AddModelError("", "Có lỗi xảy ra khi thêm sản phẩm: " + ex.Message);
			}

			// Reload categories nếu có lỗi
			try
			{
				var categories = await _categoryRepository.GetAllAsync();
				ViewBag.Categories = new SelectList(categories, "Id", "Name", productVM?.CategoryId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error reloading categories");
				ViewBag.Categories = new SelectList(new List<Category>(), "Id", "Name");
			}

			return View(productVM ?? new ProductViewModel());
		}

		// GET: Display single product
		public async Task<IActionResult> Display(int id)
		{
			try
			{
				var product = await _productRepository.GetByIdAsync(id);
				if (product == null)
				{
					TempData["Error"] = "Không tìm thấy sản phẩm";
					return RedirectToAction(nameof(Index));
				}

				// Lấy các hình ảnh bổ sung của sản phẩm
				var additionalImages = await _productRepository.GetProductImagesAsync(id);
				product.ImageUrls = additionalImages?.ToList() ?? new List<ProductImage>();

				return View(product);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error displaying product with ID: {Id}", id);
				TempData["Error"] = "Có lỗi xảy ra khi hiển thị sản phẩm";
				return RedirectToAction(nameof(Index));
			}
		}

		// GET: Update product form
		public async Task<IActionResult> Update(int id)
		{
			try
			{
				var product = await _productRepository.GetByIdAsync(id);
				if (product == null)
				{
					TempData["Error"] = "Không tìm thấy sản phẩm";
					return RedirectToAction(nameof(Index));
				}

				// Lấy các hình ảnh bổ sung của sản phẩm
				var additionalImages = await _productRepository.GetProductImagesAsync(id);
				product.ImageUrls = additionalImages?.ToList() ?? new List<ProductImage>();

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
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading update form for product ID: {Id}", id);
				TempData["Error"] = "Có lỗi xảy ra khi tải trang chỉnh sửa";
				return RedirectToAction(nameof(Index));
			}
		}

		// POST: Update product
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Update(int id, ProductViewModel productVM)
		{
			_logger.LogInformation("Updating product ID: {Id}, ViewModel: {ViewModel}", id, productVM?.Name ?? "NULL");

			try
			{
				if (productVM == null)
				{
					_logger.LogWarning("ProductViewModel is null in Update");
					TempData["Error"] = "Dữ liệu sản phẩm không hợp lệ";
					return RedirectToAction(nameof(Index));
				}

				if (id != productVM.Id)
				{
					_logger.LogWarning("ID mismatch in Update: URL={UrlId}, Model={ModelId}", id, productVM.Id);
					TempData["Error"] = "Dữ liệu không hợp lệ";
					return RedirectToAction(nameof(Index));
				}

				if (ModelState.IsValid)
				{
					// Lấy sản phẩm hiện tại từ database
					var product = await _productRepository.GetByIdAsync(id);
					if (product == null)
					{
						TempData["Error"] = "Không tìm thấy sản phẩm";
						return RedirectToAction(nameof(Index));
					}

					// Cập nhật thông tin sản phẩm
					product.Name = productVM.Name?.Trim() ?? string.Empty;
					product.Price = productVM.Price;
					product.Description = productVM.Description?.Trim() ?? string.Empty;
					product.CategoryId = productVM.CategoryId;

					// Xử lý hình ảnh chính
					if (productVM.MainImage != null && productVM.MainImage.Length > 0)
					{
						// Xóa hình ảnh cũ
						if (!string.IsNullOrEmpty(product.ImageUrl))
						{
							DeleteImageFile(product.ImageUrl);
						}

						// Lưu hình ảnh mới
						var newImagePath = await SaveImageAsync(productVM.MainImage);
						if (!string.IsNullOrEmpty(newImagePath))
						{
							product.ImageUrl = newImagePath;
						}
					}

					// Cập nhật sản phẩm vào database
					await _productRepository.UpdateAsync(product);

					// Xử lý các hình ảnh bổ sung
					await ProcessAdditionalImagesAsync(productVM.AdditionalImages, product.Id);

					TempData["Success"] = "Cập nhật sản phẩm thành công!";
					return RedirectToAction(nameof(Index));
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating product with ID: {Id}", id);
				ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật sản phẩm: " + ex.Message);
			}

			// Reload data for view khi có lỗi
			try
			{
				var categories = await _categoryRepository.GetAllAsync();
				ViewBag.Categories = new SelectList(categories, "Id", "Name", productVM?.CategoryId);

				if (productVM != null)
				{
					var additionalImages = await _productRepository.GetProductImagesAsync(productVM.Id);
					ViewBag.AdditionalImages = additionalImages?.ToList() ?? new List<ProductImage>();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error reloading data for Update view");
				ViewBag.Categories = new SelectList(new List<Category>(), "Id", "Name");
				ViewBag.AdditionalImages = new List<ProductImage>();
			}

			return View(productVM ?? new ProductViewModel());
		}

		// GET: Delete confirmation
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				var product = await _productRepository.GetByIdAsync(id);
				if (product == null)
				{
					TempData["Error"] = "Không tìm thấy sản phẩm";
					return RedirectToAction(nameof(Index));
				}
				return View(product);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading delete confirmation for product ID: {Id}", id);
				TempData["Error"] = "Có lỗi xảy ra";
				return RedirectToAction(nameof(Index));
			}
		}

		// POST: Delete product
		[HttpPost]
		[ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			try
			{
				var product = await _productRepository.GetByIdAsync(id);
				if (product != null)
				{
					// Xóa hình ảnh chính nếu có
					if (!string.IsNullOrEmpty(product.ImageUrl))
					{
						DeleteImageFile(product.ImageUrl);
					}

					// Lấy và xóa các hình ảnh bổ sung
					var additionalImages = await _productRepository.GetProductImagesAsync(id);
					foreach (var image in additionalImages ?? new List<ProductImage>())
					{
						DeleteImageFile(image.Url);
						await _productRepository.DeleteProductImageAsync(image.Id);
					}

					// Xóa sản phẩm
					await _productRepository.DeleteAsync(id);
					TempData["Success"] = "Xóa sản phẩm thành công!";
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting product with ID: {Id}", id);
				TempData["Error"] = "Có lỗi xảy ra khi xóa sản phẩm";
			}

			return RedirectToAction(nameof(Index));
		}

		// DELETE: Xóa một hình ảnh bổ sung
		[HttpPost]
		public async Task<IActionResult> DeleteImage(int id)
		{
			try
			{
				var image = await _productRepository.GetProductImageByIdAsync(id);
				if (image != null)
				{
					DeleteImageFile(image.Url);
					await _productRepository.DeleteProductImageAsync(id);
					return Json(new { success = true });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting image with ID: {Id}", id);
			}

			return Json(new { success = false });
		}

		// Private helper methods
		private async Task<string?> SaveImageAsync(IFormFile? imageFile)
		{
			try
			{
				if (imageFile == null || imageFile.Length == 0)
					return null;

				string wwwRootPath = _hostEnvironment.WebRootPath;
				string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
				string productPath = Path.Combine(wwwRootPath, "images", "products");

				// Tạo thư mục nếu chưa tồn tại
				if (!Directory.Exists(productPath))
				{
					Directory.CreateDirectory(productPath);
				}

				string fullPath = Path.Combine(productPath, fileName);

				// Lưu hình ảnh vào thư mục
				using (var fileStream = new FileStream(fullPath, FileMode.Create))
				{
					await imageFile.CopyToAsync(fileStream);
				}

				return "/images/products/" + fileName;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saving image file: {FileName}", imageFile?.FileName ?? "NULL");
				return null;
			}
		}

		private async Task ProcessAdditionalImagesAsync(IList<IFormFile>? additionalImages, int productId)
		{
			try
			{
				if (additionalImages == null || additionalImages.Count == 0)
				{
					_logger.LogInformation("No additional images to process for product {ProductId}", productId);
					return;
				}

				var productImages = new List<ProductImage>();

				foreach (var image in additionalImages)
				{
					if (image != null && image.Length > 0)
					{
						var imagePath = await SaveImageAsync(image);
						if (!string.IsNullOrEmpty(imagePath))
						{
							productImages.Add(new ProductImage
							{
								ProductId = productId,
								Url = imagePath
							});
						}
					}
				}

				// Lưu các hình ảnh bổ sung vào database
				foreach (var img in productImages)
				{
					await _productRepository.AddProductImageAsync(img);
				}

				_logger.LogInformation("Processed {Count} additional images for product {ProductId}",
					productImages.Count, productId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing additional images for product {ProductId}", productId);
				throw;
			}
		}

		private void DeleteImageFile(string? imagePath)
		{
			try
			{
				if (string.IsNullOrEmpty(imagePath))
					return;

				string wwwRootPath = _hostEnvironment.WebRootPath;
				string fullPath = Path.Combine(wwwRootPath, imagePath.TrimStart('/'));

				if (System.IO.File.Exists(fullPath))
				{
					System.IO.File.Delete(fullPath);
					_logger.LogInformation("Deleted image file: {Path}", fullPath);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting image file: {Path}", imagePath ?? "NULL");
			}
		}
	}
}