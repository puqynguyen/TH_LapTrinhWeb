using Microsoft.AspNetCore.Mvc;
using WebsiteBanHang.Models;
using WebsiteBanHang.Repositories;
using System.Threading.Tasks;

namespace WebsiteBanHang.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            ICategoryRepository categoryRepository,
            IProductRepository productRepository,
            ILogger<CategoryController> logger)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _logger = logger;
        }

        // GET: Category
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _categoryRepository.GetAllAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                TempData["Error"] = "Có lỗi xảy ra khi tải danh sách danh mục: " + ex.Message;
                return View(new List<Category>());
            }
        }

        // GET: Category/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    TempData["Error"] = "Không tìm thấy danh mục";
                    return RedirectToAction(nameof(Index));
                }

                var products = await _productRepository.GetAllAsync();
                ViewBag.Products = products.Where(p => p.CategoryId == id).ToList();

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category details for ID: {Id}", id);
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Category/Create
        public IActionResult Create()
        {
            return View(new Category());
        }

        // POST: Category/Create
        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Category category)
        {
            _logger.LogInformation("Creating category with name: {Name}", category?.Name);

            // Loại bỏ validation errors cho Products field
            ModelState.Remove("Products");

            try
            {
                if (ModelState.IsValid)
                {
                    await _categoryRepository.AddAsync(category);
                    _logger.LogInformation("Category created successfully: {Name}", category.Name);
                    TempData["Success"] = "Thêm danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid for Create");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                ModelState.AddModelError("", "Có lỗi xảy ra khi thêm danh mục: " + ex.Message);
            }
            return View(category);
        }

        // GET: Category/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation("Getting category for edit with ID: {Id}", id);

            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Category not found for ID: {Id}", id);
                    TempData["Error"] = "Không tìm thấy danh mục";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Found category: {Name} with ID: {Id}", category.Name, category.Id);
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category for edit with ID: {Id}", id);
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Category category)
        {
            _logger.LogInformation("Editing category - URL ID: {UrlId}, Model ID: {ModelId}, Name: {Name}",
                id, category?.Id, category?.Name);

            if (id != category.Id)
            {
                _logger.LogWarning("ID mismatch - URL ID: {UrlId}, Model ID: {ModelId}", id, category.Id);
                TempData["Error"] = "Dữ liệu không hợp lệ";
                return RedirectToAction(nameof(Index));
            }

            // Loại bỏ validation errors cho Products field
            ModelState.Remove("Products");

            try
            {
                if (ModelState.IsValid)
                {
                    _logger.LogInformation("ModelState is valid, updating category...");
                    await _categoryRepository.UpdateAsync(category);
                    _logger.LogInformation("Category updated successfully: {Name}", category.Name);
                    TempData["Success"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid for Edit");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Validation error: {Error}", error.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật danh mục: " + ex.Message);
            }

            return View(category);
        }

        // GET: Category/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    TempData["Error"] = "Không tìm thấy danh mục";
                    return RedirectToAction(nameof(Index));
                }

                var products = await _productRepository.GetAllAsync();
                ViewBag.HasProducts = products.Any(p => p.CategoryId == id);

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category for delete with ID: {Id}", id);
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var products = await _productRepository.GetAllAsync();
                if (products.Any(p => p.CategoryId == id))
                {
                    TempData["Error"] = "Không thể xóa danh mục này vì có sản phẩm đang thuộc danh mục";
                    return RedirectToAction(nameof(Index));
                }

                await _categoryRepository.DeleteAsync(id);
                _logger.LogInformation("Category deleted successfully with ID: {Id}", id);
                TempData["Success"] = "Xóa danh mục thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID: {Id}", id);
                TempData["Error"] = "Có lỗi xảy ra khi xóa danh mục: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}