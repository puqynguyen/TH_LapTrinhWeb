using Microsoft.EntityFrameworkCore;
using WebsiteBanHang.Repositories;
using WebsiteBanHang.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Cấu hình file upload limits
builder.Services.Configure<IISServerOptions>(options =>
{
	options.MaxRequestBodySize = 52428800; // 50MB
});

// Cấu hình Kestrel server limits
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
	options.Limits.MaxRequestBodySize = 52428800; // 50MB
	options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
	options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
});

// Cấu hình form options
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
	options.ValueLengthLimit = int.MaxValue;
	options.MultipartBodyLengthLimit = 52428800; // 50MB
	options.MultipartHeadersLengthLimit = 16384;
	options.MemoryBufferThreshold = int.MaxValue;
});

// Add DbContext (không dùng Identity)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký các Repository
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();
builder.Services.AddScoped<IProductImageRepository, EFProductImageRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}
else
{
	app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Product}/{action=Index}/{id?}");

app.Run();