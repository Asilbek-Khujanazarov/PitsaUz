using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// CORS sozlamalari
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAngularApp",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

// appsettings.json dan ulanish satrini o‘qish
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// Identity sozlamalari
builder
    .Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// FileUploadService’ni qo‘shish
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// Google autentifikatsiyasini sozlash
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None; // CORS uchun
        options.Cookie.SecurePolicy = CookieSecurePolicy.None; // HTTP uchun (test uchun)
        options.Cookie.Name = "PitsaUzAuthCookie";
        options.Cookie.Path = "/";
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GoogleAuth:ClientId"];
        options.ClientSecret = builder.Configuration["GoogleAuth:ClientSecret"];
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });

// JWT tokenlarni qo‘llab-quvvatlash (API autentifikatsiyasi uchun)
builder.Services.AddAuthorization();

// Swagger xizmatini qo‘shish
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "PitsaUz API",
            Version = "v1",
            Description = "PitsaUz loyihasi uchun API hujjatlari",
        }
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // Swagger UI ni faqat development muhitida ishlatish
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PitsaUz API V1");
        c.RoutePrefix = string.Empty; // Swagger UI ni asosiy URL’da ochish (http://localhost:5233/)
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// CORS middleware’ni ishlatish
app.UseCors("AllowAngularApp");

// Statik fayllarni ishlatish (rasmlar uchun)
app.UseStaticFiles();

// Autentifikatsiya va avtorizatsiya middleware’lari
app.UseAuthentication();
app.UseAuthorization();

// Controller endpointlarini yoqish
app.MapControllers();

// Ma'lumotlar bazasini yaratish va migratsiya qilish
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();