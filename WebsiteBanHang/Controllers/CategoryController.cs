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

        public CategoryController(ICategoryRepository categoryRepository, IProductRepository productRepository)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
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
                // Log error
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

                // Lấy danh sách sản phẩm thuộc category này
                var products = await _productRepository.GetAllAsync();
                ViewBag.Products = products.Where(p => p.CategoryId == id).ToList();

                return View(category);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Category/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] Category category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _categoryRepository.AddAsync(category);
                    TempData["Success"] = "Thêm danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi thêm danh mục: " + ex.Message);
            }
            return View(category);
        }

        // GET: Category/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null)
                {
                    TempData["Error"] = "Không tìm thấy danh mục";
                    return RedirectToAction(nameof(Index));
                }
                return View(category);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Category category)
        {
            if (id != category.Id)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (ModelState.IsValid)
                {
                    await _categoryRepository.UpdateAsync(category);
                    TempData["Success"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
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

                // Kiểm tra xem category có chứa sản phẩm nào không
                var products = await _productRepository.GetAllAsync();
                ViewBag.HasProducts = products.Any(p => p.CategoryId == id);

                return View(category);
            }
            catch (Exception ex)
            {
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
                // Kiểm tra xem category có chứa sản phẩm nào không
                var products = await _productRepository.GetAllAsync();
                if (products.Any(p => p.CategoryId == id))
                {
                    TempData["Error"] = "Không thể xóa danh mục này vì có sản phẩm đang thuộc danh mục";
                    return RedirectToAction(nameof(Index));
                }

                await _categoryRepository.DeleteAsync(id);
                TempData["Success"] = "Xóa danh mục thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa danh mục: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}