using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PddTrainer.Api;
using PddTrainer.Api.AutoMapper;
using PddTrainer.Api.Data;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddDbContext<ApplicationDbContext>
    (options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient();
builder.Services.AddScoped<QuestionThemeMatcher>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactDevServer",
        builder => builder
            .WithOrigins("http://localhost:5173") // React dev-сервер
            .AllowAnyHeader()
            .AllowAnyMethod());
});


var app = builder.Build();

/* Разовый запуск для сопоставления вопроса-темы.
using (var scope = app.Services.CreateScope())
{
    var matcher = scope.ServiceProvider
        .GetRequiredService<QuestionThemeMatcher>();

    await matcher.MatchAsync();
}
*/

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactDevServer");

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
