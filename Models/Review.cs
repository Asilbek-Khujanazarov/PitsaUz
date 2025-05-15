public class Review
{
    public int Id { get; set; }
    public string Comment { get; set; }
    public List<string> ImageUrls { get; set; } // Bir nechta rasm URL’lari
    public int Rating { get; set; } // 1-5 oralig‘ida
    public string CompanyId { get; set; } // Frontend’dan keladi
    public string UserId { get; set; }
    public string UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
