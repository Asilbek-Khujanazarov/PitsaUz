using System.ComponentModel.DataAnnotations;

public class ReviewDto
{
    [Required]
    public string Comment { get; set; }

    public List<IFormFile> Images { get; set; } // Bir nechta rasm yuklash uchun

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    public string CompanyId { get; set; }
}