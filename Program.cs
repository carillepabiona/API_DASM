
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.SignalR;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region 🌐 KESTREL (LAN ACCESS - IMPORTANT FOR MAUI / MOBILE)
builder.WebHost.ConfigureKestrel(options =>
{
    // Allows access from ANY device in LAN
    options.ListenAnyIP(5043); // HTTP

    // OPTIONAL HTTPS (only if you configure certificates)
    // options.ListenAnyIP(7043, listenOptions => listenOptions.UseHttps());
});
#endregion

#region 🔗 CONTROLLERS + SIGNALR
builder.Services.AddControllers();
builder.Services.AddSignalR();
#endregion

#region 🌍 CORS (ALLOW MAUI / MOBILE / FRONTEND ACCESS)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin(); // DEV ONLY (tighten in production)
    });
});
#endregion

#region 🗄️ DATABASE (SQL SERVER + EF CORE)

#endregion

#region 🔐 JWT CONFIGURATION
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]);

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

        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),

        ClockSkew = TimeSpan.Zero
    };
});
#endregion

#region 🔒 AUTHORIZATION
builder.Services.AddAuthorization();
#endregion

#region 🧠 DEPENDENCY INJECTION (SERVICES)

#endregion

#region 📘 SWAGGER (API TESTING)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
#endregion

var app = builder.Build();

#region 🧪 DEVELOPMENT TOOLS
app.UseSwagger();
app.UseSwaggerUI();
#endregion

#region 🌍 MIDDLEWARE PIPELINE ORDER (IMPORTANT)
app.UseCors("AllowAll");

app.UseAuthentication();   // MUST be before Authorization
app.UseAuthorization();

app.MapControllers();

#endregion

#region 🔌 SIGNALR HUB MAPPING (ADD WHEN READY)

#endregion

app.Run();