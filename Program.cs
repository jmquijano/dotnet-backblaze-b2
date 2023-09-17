using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Configuration
    .AddJsonFile("appsettings.Backblaze.json", reloadOnChange: true, optional: false);

// AWS S3
AWSOptions awsOptions = new AWSOptions()
{
    Credentials = new BasicAWSCredentials(
        builder.Configuration["Backblaze:KeyID"],
        builder.Configuration["Backblaze:ApplicationKey"]
    ),
    Region = RegionEndpoint.USEast1,
    DefaultClientConfig =
    {
        ServiceURL = builder.Configuration["Backblaze:ServiceUrl"]
    }
};
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonS3>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

