using BrotherQlHub.Server;
using BrotherQlHub.Server.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure services
//========================================================================================================
// Cross origin requests allowed
builder.Services.AddCors(options => 
    options.AddPolicy("CorsPolicy", b =>
    {
        b.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials();
    }));

// Database and other services
builder.Services.UseDatabase(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<CategoryManager>();


// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddMudServices();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();

// Build the application
//========================================================================================================
var app = builder.Build();
app.ApplyMigrations();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseCors("CorsPolicy");

app.MapHub<PrinterHub>("/printer");
app.MapBlazorHub();
app.MapControllers();
app.MapFallbackToPage("/_Host");

app.Run();