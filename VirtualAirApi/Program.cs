using VirtualAirApi.Common;

var builder = WebApplication.CreateBuilder(args);

//start on port 6020
builder.WebHost.UseUrls("http://*:6020");

// Add services to the container.
builder.Services.AddControllers().AddNewtonsoftJson();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddCors(); // Add this line

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Add environment variables
var connectionString = Environment.GetEnvironmentVariable("VirtualAirConnection");

if (connectionString is null)
{
	Db.ConnectionString = string.Empty;
}
else
{
	Db.ConnectionString = connectionString;
}

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
	app.UseSwagger();
	app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseCors(policy => 
	policy.AllowAnyOrigin()
		  .AllowAnyMethod()
		  .AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<JwtMiddleware>();

app.MapControllers();

app.Run();