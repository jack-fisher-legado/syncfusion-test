using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;
using SyncFusionTest.Controllers;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers(); 

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
};

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers(); // Map controller endpoints
app.MapProgramEndpoints();

app.Run();


public static class ProgramEndpoints
{
    public static void MapProgramEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Program").WithTags(nameof(Program));

        group.MapGet("/", () =>
        {
            return new[] { new Program() };
        })
        .WithName("GetAllPrograms")
        .WithOpenApi();

        group.MapGet("/{id}", (int id) =>
        {
            //return new Program { ID = id };
        })
        .WithName("GetProgramById")
        .WithOpenApi();

        group.MapPut("/{id}", (int id, Program input) =>
        {
            return TypedResults.NoContent();
        })
        .WithName("UpdateProgram")
        .WithOpenApi();

        group.MapPost("/", (Program model) =>
        {
            //return TypedResults.Created($"/api/Programs/{model.ID}", model);
        })
        .WithName("CreateProgram")
        .WithOpenApi();

        group.MapDelete("/{id}", (int id) =>
        {
            //return TypedResults.Ok(new Program { ID = id });
        })
        .WithName("DeleteProgram")
        .WithOpenApi();
    }
}