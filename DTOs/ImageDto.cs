using System.ComponentModel.DataAnnotations;

public class ImageDto
{
    [Required]
    public string CompanyId { get; set; }

    [Required]
    public IFormFile Image { get; set; } // Faqat bitta rasm yuklash
}