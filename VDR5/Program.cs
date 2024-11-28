using VDR5;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<FileDbContext>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapGet("/ping", () => "pong");

app.MapGet("/files", async (FileDbContext db) => 
    await db.Files.ToListAsync()
    );

app.MapPost("files/upload", async (FileDbContext db, IFormFile file) =>
    {
        var allFiles = await db.Files.ToListAsync();
        
        //Find the file with the same name
        var existingFile = allFiles.FirstOrDefault(f => f.Name == file.FileName);
        if (existingFile != null)
        {
            return Results.BadRequest($"File with name {file.FileName} already exists");
        }

        var fileName = file.FileName;

        var newFile = new VDR5.File
        {
            Name = file.FileName,
            FullPath = file.FileName,
            UploadedAt = DateTime.UtcNow
        };
        db.Files.Add(newFile);
        await db.SaveChangesAsync();

        return Results.Created($"/files/{newFile.Id}", newFile);
    })
    .DisableAntiforgery();
app.Run();
