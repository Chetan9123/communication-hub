using System.Text;
using CommunicationHub.Infrastructure.Hubs;
using CommunicationHub.Application.Interfaces;
using CommunicationHub.Infrastructure.Data;
using CommunicationHub.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

// Add DbContext
builder.Services.AddDbContext<CommunicationHubDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Application Services
builder.Services.AddScoped<ICommunicationService, CommunicationService>();
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IAdjusterService, AdjusterService>();
builder.Services.AddScoped<IEmailService, MailKitEmailService>();
builder.Services.AddScoped<ISmsService, TwilioSmsService>();
builder.Services.AddScoped<IWhatsAppService, TwilioWhatsAppService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IS3Service, S3Service>();
builder.Services.AddScoped<IAutoReplyService, AutoReplyService>();

// Add AWS S3
var awsOptions = builder.Configuration.GetAWSOptions();
var awsSection = builder.Configuration.GetSection("AWS");
if (!string.IsNullOrEmpty(awsSection["AccessKey"]) && !string.IsNullOrEmpty(awsSection["SecretKey"]))
{
    awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(awsSection["AccessKey"], awsSection["SecretKey"]);
}
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<Amazon.S3.IAmazonS3>();
builder.Services.AddHostedService<ImapListeningService>();

// Add CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-super-secret-key-change-this-in-production";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CommunicationHub";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CommunicationHubClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// In local dev, keep HTTP working (Angular default is http://localhost:4200)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowAll");
app.UseStaticFiles(); // Serve media from wwwroot
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MessagingHub>("/hubs/communications");

app.Run();
