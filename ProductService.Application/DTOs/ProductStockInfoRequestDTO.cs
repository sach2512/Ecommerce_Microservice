using System.ComponentModel.DataAnnotations;

namespace ProductService.Application.DTOs
{
    public class ProductStockInfoRequestDTO
    {
        [Required(ErrorMessage = "ProductId is Required")]
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is Required")]
        public int Quantity { get; set; }
    }
}
