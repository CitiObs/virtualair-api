var builder = WebApplication.CreateBuilder(args);

//start on port 5020
builder.WebHost.UseUrls("http://*:5020");

// Add services to the container.
builder.Services.AddControllers().AddNewtonsoftJson();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
	app.UseSwagger();
	app.UseSwaggerUI();
//}

//Enable cors
app.UseCors(builder => builder
	.AllowAnyOrigin()
		.AllowAnyMethod()
			.AllowAnyHeader());

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
