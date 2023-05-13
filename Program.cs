using Bakalauras.Auth;
using Bakalauras.Auth.Model;
using Bakalauras.data;
using Bakalauras.data.repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
// Add services to the container.
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://127.0.0.1:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddControllers();
builder.Services.AddIdentity<BookieUser, IdentityRole>(options =>
{
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ĄąČčĘęĖėĮįŠšŲųŪūŽž";
}).AddEntityFrameworkStores<BookieDBContext>().AddDefaultTokenProviders();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}
).AddJwtBearer(options =>
{
    options.TokenValidationParameters.ValidAudience = builder.Configuration["JWT:ValidAudience"];
    options.TokenValidationParameters.ValidIssuer = builder.Configuration["JWT:ValidIssuer"];
    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]));
});

builder.Services.AddDbContext<BookieDBContext>();
builder.Services.AddTransient<IBookRepository, BookRepository>();
builder.Services.AddTransient<IGenreRepository, GenreRepository>();
builder.Services.AddTransient<ICommentRepository, CommentRepository>();
builder.Services.AddTransient<IChaptersRepository, ChaptersRepository>();
builder.Services.AddTransient<ITextRepository, TextsRepository>();
builder.Services.AddTransient<IProfileRepository, ProfileRepository>();
builder.Services.AddTransient<IDailyQuestionRepository, DailyQuestionRepository>();
builder.Services.AddTransient<IWriterPlatformRepository, WriterPlatformRepository>();
builder.Services.AddTransient<IAdminRepository, AdminRepository>();
builder.Services.AddTransient<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<AuthDbSeeder>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.ResourceOwner, policy => policy.Requirements.Add(new ResourceOwnerRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, ResourceOwnerAuthorizationHandler>();
var app = builder.Build();

// Configure the HTTP request pipeline.



app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

var dbseeder = app.Services.CreateScope().ServiceProvider.GetRequiredService<AuthDbSeeder>();
await dbseeder.SeedAsync();
app.Run();
