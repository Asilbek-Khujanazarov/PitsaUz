using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _fileUploadService;

    public ImagesController(ApplicationDbContext context, IFileUploadService fileUploadService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
    }

    // Kompaniya uchun rasm yuklash
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UploadImage([FromForm] ImageDto imageDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrEmpty(imageDto.CompanyId))
            return BadRequest("CompanyId bo‘sh bo‘lmasligi kerak.");

        if (imageDto.Image == null)
            return BadRequest("Rasm fayli bo‘sh bo‘lmasligi kerak.");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Kompaniya uchun jami rasm sonini tekshirish
        var existingImagesCount = await _context.Images
            .Where(i => i.CompanyId == imageDto.CompanyId)
            .CountAsync();

        if (existingImagesCount >= 15)
            return BadRequest("Bu kompaniya uchun maksimal 15 ta rasm yuklanishi mumkin.");

        // Rasmni yuklash
        var imageUrl = await _fileUploadService.UploadFileAsync(imageDto.Image);
        if (string.IsNullOrEmpty(imageUrl))
            return BadRequest("Rasm yuklashda xatolik yuz berdi.");

        var image = new Image
        {
            ImageUrl = imageUrl,
            CompanyId = imageDto.CompanyId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Images.Add(image);
        await _context.SaveChangesAsync();

        return Ok(image);
    }

    // Kompaniya uchun rasmlarni olish
    [HttpGet("{companyId}")]
    public async Task<IActionResult> GetImages(string companyId)
    {
        if (string.IsNullOrEmpty(companyId))
            return BadRequest("CompanyId bo‘sh bo‘lmasligi kerak.");

        var images = await _context.Images
            .Where(i => i.CompanyId == companyId)
            .ToListAsync();

        return Ok(images);
    }

    // Rasmni o‘chirish (faqat rasm muallifi uchun)
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteImage(int id)
    {
      
        var image = await _context.Images.FindAsync(id);
        if (image == null)
            return NotFound("Rasm topilmadi.");
            
        // Rasmni fayl tizimidan o‘chirish
        await _fileUploadService.DeleteFileAsync(image.ImageUrl);

        _context.Images.Remove(image);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Rasm muvaffaqiyatli o‘chirildi." });
    }
}

