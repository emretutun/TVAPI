using Microsoft.AspNetCore.HttpLogging;
using TVAPI.Application;
using TVAPI.ErrorHandling;
using TVAPI.Filters;
using TVAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Katmanların kaydı. Infrastructure'a yalnızca burada dokunulur (composition root); controller'lar onu tanımaz.
builder.Services.AddApplication();

// YEREL GELİŞTİRME: "ConnectionStrings:Broadage" (spor verisi, zorunlu) ile isteğe bağlı "ConnectionStrings:Channel"
// (kanalın kendi veritabanı) ve "ConnectionStrings:News" (haber veritabanı) değerleri şu an bu bilgisayardaki user-secrets deposundan
// (localhost MySQL) okunuyor. Canlıya geçerken bu değer sunucunun ortam değişkeninden ya da gizli ayar
// deposundan verilecek; kod değişmeyecek, bu not kaldırılabilir.
builder.Services.AddInfrastructure(builder.Configuration);

// Hata sözleşmesi: bütün hatalar tek biçimde (ProblemDetails) döner, yakalanmamış hatalar tek yerde ele alınır.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// İstek logu: her istek için tek log kaydında metot, adres, durum kodu ve süre. Gövde ve başlıklar loglanmaz.
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.RequestQuery |
        HttpLoggingFields.ResponseStatusCode |
        HttpLoggingFields.Duration;
    options.CombineLogs = true;
});

// Her controller cevabına veri tazeliği başlıkları eklenir (X-Data-Fetched-At, X-Data-Stale).
builder.Services.AddControllers(options => options.Filters.Add<DataFreshnessHeaderFilter>());
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.

// Sıra önemlidir: en dıştaki log her isteği (hatalı olanlar dahil) görür,
// hata işleyici ise kendisinden sonra gelen her şeyin hatasını yakalar.
app.UseHttpLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
