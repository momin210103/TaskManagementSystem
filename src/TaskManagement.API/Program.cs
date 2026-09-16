using Microsoft.OpenApi;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Task Management API",
        Version = "v1",
        Description = "Role-based Team Task Management System API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter your JWT Bearer token (paste the raw token directly without typing 'Bearer ')",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Management API v1");
        options.RoutePrefix = "swagger";
    });
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await DatabaseSeeder.SeedAsync(app.Services).ConfigureAwait(false);

await app.RunAsync().ConfigureAwait(false);


