using Serilog;
using IntranetPortal.API;
using IntranetPortal.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddResponseCaching();
builder.Services.AddEndpointsApiExplorer();

Startup.Start(builder);
builder.Services.AddHttpContextAccessor();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
    options.RoutePrefix = string.Empty;
});
app.UseHttpsRedirection();


app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware(typeof(ExceptionMiddleware));

app.MapControllers();
app.UseSerilogRequestLogging();
app.Run();
