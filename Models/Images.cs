public class Image
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } // Yuklangan rasmning URL’i
    public string CompanyId { get; set; } // Kompaniya identifikatori
    public string UserId { get; set; } // Rasmni yuklagan foydalanuvchi
    public DateTime CreatedAt { get; set; }
}