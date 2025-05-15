using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

[Route("api/[controller]")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileUploadService _fileUploadService;

    public ReviewsController(ApplicationDbContext context, IFileUploadService fileUploadService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
    }

    // Sharh qoldirish (faqat autentifikatsiya qilingan foydalanuvchilar uchun)
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> PostReview([FromForm] ReviewDto reviewDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrEmpty(reviewDto.CompanyId))
            return BadRequest("CompanyId bo‘sh bo‘lmasligi kerak.");

        if (reviewDto.Rating < 1 || reviewDto.Rating > 5)
            return BadRequest("Reyting 1 dan 5 gacha bo‘lishi kerak.");

        if (reviewDto.Images != null && reviewDto.Images.Count > 5)
            return BadRequest("Bir sharhga 5 tadan ortiq rasm yuklab bo‘lmaydi.");

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Rasmlarni yuklash
        List<string> imageUrls = new List<string>();
        if (reviewDto.Images != null && reviewDto.Images.Any())
        {
            foreach (var image in reviewDto.Images)
            {
                var imageUrl = await _fileUploadService.UploadFileAsync(image);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    imageUrls.Add(imageUrl);
                }
            }
        }

        var review = new Review
        {
            Comment = reviewDto.Comment,
            ImageUrls = imageUrls, // Bir nechta rasm URL’lari
            Rating = reviewDto.Rating,
            CompanyId = reviewDto.CompanyId, // Frontend’dan keladi
            UserId = userId, // Autentifikatsiyadan olingan foydalanuvchi 
            UserName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value, // Autentifikatsiyadan olingan foydalanuvchi ismi
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return Ok(review);
    }

    // Muayyan kompaniyaga tegishli sharhlarni olish
    [HttpGet("{companyId}")]
    public async Task<IActionResult> GetReviews(string companyId)
    {
        if (string.IsNullOrEmpty(companyId))
            return BadRequest("CompanyId bo‘sh bo‘lmasligi kerak.");

        var reviews = await _context.Reviews
            .Where(r => r.CompanyId == companyId)
            .ToListAsync();
        return Ok(reviews);
    }

    // Muayyan foydalanuvchi sharhlarini olish
    [HttpGet("user")]
    [Authorize]
    public async Task<IActionResult> GetUserReviews()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var reviews = await _context.Reviews
            .Where(r => r.UserId == userId)
            .ToListAsync();
        return Ok(reviews);
    }

    // Sharhni o‘chirish (faqat sharh muallifi uchun)
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound("Sharh topilmadi.");

        if (review.UserId != userId)
            return Forbid("Siz faqat o‘zingizning sharhlaringizni o‘chira olasiz.");

        // Agar sharh bilan rasmlar bog‘langan bo‘lsa, rasmlarni o‘chirish
        if (review.ImageUrls != null && review.ImageUrls.Any())
        {
            foreach (var imageUrl in review.ImageUrls)
            {
                await _fileUploadService.DeleteFileAsync(imageUrl);
            }
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Sharh muvaffaqiyatli o‘chirildi." });
    }

    // Sharhni tahrirlash (faqat sharh muallifi uchun)
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateReview(int id, [FromForm] ReviewDto reviewDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrEmpty(reviewDto.CompanyId))
            return BadRequest("CompanyId bo‘sh bo‘lmasligi kerak.");

        if (reviewDto.Rating < 1 || reviewDto.Rating > 5)
            return BadRequest("Reyting 1 dan 5 gacha bo‘lishi kerak.");

        if (reviewDto.Images != null && reviewDto.Images.Count > 5)
            return BadRequest("Bir sharhga 5 tadan ortiq rasm yuklab bo‘lmaydi.");

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
            return NotFound("Sharh topilmadi.");

        if (review.UserId != userId)
            return Forbid("Siz faqat o‘zingizning sharhlaringizni tahrirlashingiz mumkin.");

        // Sharhni yangilash
        review.Comment = reviewDto.Comment;
        review.Rating = reviewDto.Rating;
        review.CompanyId = reviewDto.CompanyId;

        // Agar yangi rasmlar yuklangan bo‘lsa
        if (reviewDto.Images != null && reviewDto.Images.Any())
        {
            // Eski rasmlarni o‘chirish
            if (review.ImageUrls != null && review.ImageUrls.Any())
            {
                foreach (var imageUrl in review.ImageUrls)
                {
                    await _fileUploadService.DeleteFileAsync(imageUrl);
                }
            }

            // Yangi rasmlarni yuklash
            List<string> newImageUrls = new List<string>();
            foreach (var image in reviewDto.Images)
            {
                var imageUrl = await _fileUploadService.UploadFileAsync(image);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    newImageUrls.Add(imageUrl);
                }
            }
            review.ImageUrls = newImageUrls;
        }

        await _context.SaveChangesAsync();

        return Ok(review);
    }
}

