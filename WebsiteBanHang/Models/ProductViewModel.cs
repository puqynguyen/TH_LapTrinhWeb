using System.ComponentModel.DataAnnotations;

namespace WebsiteBanHang.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Range(0.01, 10000.00)]
        public decimal Price { get; set; }

        public string Description { get; set; }

        public IFormFile? MainImage { get; set; }

        public string? ImageUrl { get; set; }

        public List<IFormFile>? AdditionalImages { get; set; }

        public int CategoryId { get; set; }
    }
}
