using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using SistemaERP.Infrastructure.DependencyInjection;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Leer PORT para Railway/producción; en local usa defaults (5000/5001 desde launchSettings)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) && int.TryParse(port, out var p))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(p);
    });
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serializa en camelCase para coincidir con el contrato del frontend
        // (ej. CashRegisterResponseDto.Id -> "id"). Sin esto, System.Text.Json
        // usa PascalCase y el frontend recibe "Id", dejando register.id en undefined.
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "SistemaERP", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// JWT: Railway inyecta Jwt__Secret como variable de entorno. La config en appsettings.json
// solo contiene valores que no son secretos (Issuer, Audience, ExpiryHours, etc.).
var jwt = builder.Configuration.GetSection("Jwt");
var secret = Environment.GetEnvironmentVariable("Jwt__Secret")
    ?? jwt["Secret"]
    ?? throw new InvalidOperationException("JWT Secret is not configured. Establece Jwt__Secret como variable de entorno en Railway.");
var key = Encoding.UTF8.GetBytes(secret);
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
        ValidIssuer = jwt["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwt["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Connection string: Render inyecta DATABASE_URL (formato URL de Neon).
// Convierte URI a keyword=value para EF Core + Npgsql.
// Fallback a appsettings.json para desarrollo local (appsettings.Development.json).
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Convierte URI de Neon (postgresql://...) a keyword=value para Npgsql/EF Core
    connectionString = ParseAndValidateNeonConnectionString(databaseUrl);
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. " +
            "Establece DATABASE_URL como variable de entorno en Render/Neon.");
}

builder.Services.AddInfrastructureServices(connectionString);

// --- Autorización basada en permisos (RBAC, PARTE 3) ---
// El proveedor dinámico resuelve cualquier política cuyo nombre sea un código de permiso
// (ej. "sales.create") a RequireClaim("permission", "sales.create"). El claim "permission"
// se emite en el JWT durante el login (uno por permiso efectivo), por lo que la validación
// no toca la base de datos. No es necesario registrar cada política a mano.
//
// MIGRACIÓN GRADUAL DE CONTROLLERS (futura PARTE): en lugar de [Authorize] simple, usar
//   using SistemaERP.Domain;
//   [Authorize(Policy = PermissionCodes.SalesCreate)]
// Esto reemplaza progresivamente los [Authorize] actuales de Products/Sales/Purchases/etc.
// sin requerir cambios aquí. Por ahora los controllers existentes siguen con [Authorize].
builder.Services.AddSingleton<IAuthorizationPolicyProvider, SistemaERP.Api.Authorization.PermissionPolicyProvider>();

// CORS configurable por entorno (dev: appsettings.json, prod: variable de entorno Cors__AllowedOrigin)
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"]
    ?? (builder.Environment.IsDevelopment() ? "http://localhost:5173" : throw new InvalidOperationException("Cors:AllowedOrigin is not configured."));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Política global: 100 req/min por usuario autenticado (claim "userId") o IP anónima
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var userId = context.User.FindFirst("userId")?.Value;
        var partitionKey = userId ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Política estricta para login: 5 req/min por IP (ventana fija)
    options.AddFixedWindowLimiter("LoginPolicy", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });

    // Política estricta para forgot-password: 3 req/hora por IP (ventana fija)
    options.AddFixedWindowLimiter("ForgotPasswordPolicy", o =>
    {
        o.PermitLimit = 3;
        o.Window = TimeSpan.FromHours(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit = 0;
    });

    // Respuesta 429 personalizada
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
            ? retry.TotalSeconds.ToString("0") : "60";
        context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too Many Requests",
            message = "Se ha excedido el límite de peticiones. Intente nuevamente en unos segundos.",
            retryAfterSeconds = retryAfter
        }));
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Aplica migraciones pendientes y seed del catálogo de permisos al iniciar. Sin esta
// llamada el PermissionSeed nunca corre y la tabla Permissions queda vacía, dejando el
// formulario de roles sin checkboxes que mostrar. Idempotente: no duplica permisos.
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<SistemaERP.Infrastructure.DependencyInjection.DbInitializer>();
    await initializer.InitializeAsync();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

// Test connection to Neon (no-throw on fail)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await TestNeonConnectionAsync(connectionString, logger);
}

app.Run();

// Test parse Neon URI format with dummy data (ejecutado solo si DEBUG_TESTS está definido)
#if DEBUG
{
    var demoUri = "postgresql://neondb_user:neondb_password@ep-wild-???.us-east-2.aws.neon.tech:5432/neondb?sslmode=require";
    var parsed = ParseAndValidateNeonConnectionString(demoUri);
    Console.WriteLine($"URI NEON demo parseado: {parsed}");
}
#endif

// ---------------------------------------------------------------------------
// Helper: parsea la URI de conexión de Neon (postgresql://...) a keyword=value
// compatible con NpgsqlConnectionStringBuilder / EF Core, asegurando SSL.
static string ParseAndValidateNeonConnectionString(string databaseUrl)
{
    try
    {
        var uri = new Uri(databaseUrl);
        var query = uri.Query?.TrimStart('?') ?? string.Empty;
        var ps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length == 2)
                ps[kv[0]] = kv[1];
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port == -1 ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            // Uri.UnescapeDataString() decode %20, %40, %23 etc. before passing to Npgsql.
            Username = uri.UserInfo.Contains(':') ? Uri.UnescapeDataString(uri.UserInfo.Split(':')[0]) : string.Empty,
            Password = uri.UserInfo.Contains(':') ? Uri.UnescapeDataString(uri.UserInfo.Split(':')[1]) : string.Empty,
            SslMode = SslMode.Require,  // Forzado: Neon exige SSL
        };

        // Permite sobrescribir sslmode si viene en query params (ej: disable, prefer)
        if (ps.TryGetValue("sslmode", out var sslModeVal))
        {
            if (Enum.TryParse<SslMode>(sslModeVal, true, out var parsed))
                builder.SslMode = parsed;
        }

        // Parámetros extra (ej: channel_verbosity, sslcompression, etc.)
        foreach (var kv in ps)
        {
            var key = kv.Key.ToLowerInvariant();
            if (new[] { "sslmode", "host", "port", "database", "user", "password", "host", "port", "dbname", "userinfo", "username", "sslmode", "sslmode" }.Contains(key))
                continue;
            try { builder[kv.Key] = kv.Value; } catch { /* parámetro desconocido */ }
        }

        // Asegurar un CommandTimeout razonable (Neon free tier puede cold-start)
        if (builder.CommandTimeout <= 0)
            builder.CommandTimeout = 30;

        return builder.ToString();
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            "DATABASE_URL tiene un formato inválido. " +
            "Esperado: postgresql://user:pass@host:port/db?sslmode=require", ex);
    }
}

// Health check de conexión a Neon (se ejecuta al arrancar, no bloquea si falla)
static async Task TestNeonConnectionAsync(string connectionString, ILogger logger)
{
    try
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT version()";
        var version = await cmd.ExecuteScalarAsync();
        logger.LogInformation("✅ CONEXIÓN NEON VERIFICADA: {Version}", version);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ FALLO AL CONECTAR A NEON: {Message}");
        // No lanzamos para que la app siga iniciando; el health check de Render detectará fallos
    }
}
