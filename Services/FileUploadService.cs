public interface IFileUploadService
{
    Task<string> UploadFileAsync(IFormFile file);
    Task DeleteFileAsync(string filePath);
}

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;

    public FileUploadService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return null;

        // wwwroot/uploads jildini ishlatamiz
        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{fileName}";
    }

    public async Task DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            // filePath = "/uploads/filename.jpg" shaklida keladi
            var fullPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, filePath.TrimStart('/'));
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"Fayl topilmadi: {fullPath}");
                return;
            }

            await Task.Run(() => File.Delete(fullPath));
            Console.WriteLine($"Fayl muvaffaqiyatli o‘chirildi: {fullPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Rasmni o‘chirishda xato: {ex.Message}, Yo‘l: {filePath}");
        }
    }
}