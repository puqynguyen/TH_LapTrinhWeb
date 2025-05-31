using System.ComponentModel.DataAnnotations;

namespace WebsiteBanHang.Models
{
	public class ProductViewModel
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
		[StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá 100 ký tự")]
		public string Name { get; set; } = string.Empty;

		[Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
		[Range(0.01, 10000.00, ErrorMessage = "Giá sản phẩm phải từ 0.01 đến 10,000")]
		public decimal Price { get; set; }

		public string Description { get; set; } = string.Empty;

		[Display(Name = "Hình ảnh chính")]
		public IFormFile? MainImage { get; set; }

		public string? ImageUrl { get; set; }

		[Display(Name = "Hình ảnh bổ sung")]
		public List<IFormFile>? AdditionalImages { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn danh mục")]
		[Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục hợp lệ")]
		public int CategoryId { get; set; }

		// Custom validation method
		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			var results = new List<ValidationResult>();

			// Validate main image
			if (MainImage != null)
			{
				if (!IsValidImageFile(MainImage))
				{
					results.Add(new ValidationResult("Hình ảnh chính phải là file ảnh hợp lệ (.jpg, .jpeg, .png, .gif, .bmp)", new[] { nameof(MainImage) }));
				}

				if (MainImage.Length > 10 * 1024 * 1024) // 10MB
				{
					results.Add(new ValidationResult("Hình ảnh chính không được vượt quá 10MB", new[] { nameof(MainImage) }));
				}
			}

			// Validate additional images
			if (AdditionalImages != null && AdditionalImages.Count > 0)
			{
				if (AdditionalImages.Count > 10)
				{
					results.Add(new ValidationResult("Không được upload quá 10 hình ảnh bổ sung", new[] { nameof(AdditionalImages) }));
				}

				long totalSize = 0;
				foreach (var image in AdditionalImages)
				{
					if (image != null && image.Length > 0)
					{
						if (!IsValidImageFile(image))
						{
							results.Add(new ValidationResult($"File {image.FileName} không phải là file ảnh hợp lệ", new[] { nameof(AdditionalImages) }));
						}

						if (image.Length > 10 * 1024 * 1024) // 10MB per file
						{
							results.Add(new ValidationResult($"File {image.FileName} không được vượt quá 10MB", new[] { nameof(AdditionalImages) }));
						}

						totalSize += image.Length;
					}
				}

				if (totalSize > 50 * 1024 * 1024) // 50MB total
				{
					results.Add(new ValidationResult("Tổng dung lượng các hình ảnh không được vượt quá 50MB", new[] { nameof(AdditionalImages) }));
				}
			}

			return results;
		}

		private bool IsValidImageFile(IFormFile file)
		{
			if (file == null) return false;

			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
			var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

			return allowedExtensions.Contains(fileExtension);
		}
	}
}