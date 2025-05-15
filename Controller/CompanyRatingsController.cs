using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class CompanyRatingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CompanyRatingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/CompanyRatings/{companyId}
    [HttpGet("{companyId}")]
    public async Task<IActionResult> GetCompanyRating(string companyId)
    {
        // Validate CompanyId
        if (string.IsNullOrEmpty(companyId))
            return BadRequest("CompanyId bo‘sh bo‘lmasligi kerak.");

        // Query reviews for the company
        var reviews = await _context.Reviews
            .Where(r => r.CompanyId == companyId)
            .ToListAsync();

        // Check if reviews exist
        if (!reviews.Any())
            return Ok(new { Rating = 0.0, ReviewCount = 0, Message = "Bu kompaniya uchun sharhlar mavjud emas." });

        // Calculate average rating
        var averageRating = reviews.Average(r => r.Rating);
        var reviewCount = reviews.Count;

        // Round to one decimal place for readability
        averageRating = Math.Round(averageRating, 1);

        // Return the result
        return Ok(new
        {
            Rating = averageRating,
            ReviewCount = reviewCount
        });
    }
}