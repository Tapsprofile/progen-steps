using Procurement.Orchestration.Core.Exceptions;
using Procurement.Orchestration.Repositories;
using Procurement.Orchestration.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin());
});

// Repositories
builder.Services.AddSingleton<IPurchaseRequestRepository, InMemoryPurchaseRequestRepository>();
builder.Services.AddSingleton<IWorkflowStore, InMemoryWorkflowStore>();

// Services
builder.Services.Configure<HandwrittenWorkflowOptions>(builder.Configuration.GetSection("HandwrittenWorkflow"));
builder.Services.AddSingleton<IWorkflowRuntime, HandwrittenWorkflowRuntime>();
builder.Services.AddSingleton<IPurchaseRequestService, PurchaseRequestService>();

var app = builder.Build();

app.UseCors();

// Exception handling
app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
