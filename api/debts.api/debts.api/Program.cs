using application.Extensions;
using debts.api.Extensions;
using persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddPersistence();

var app = builder.Build();

app.UseApiPipeline();

app.Run();
