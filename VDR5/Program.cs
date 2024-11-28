using VDR5;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

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

//Now add pagination to the files endpoint
app.MapGet("/files", async (FileDbContext db, int page = 1, int pageSize = 20) => 
    {
    if (page < 1 || pageSize < 1)
    {
        return Results.BadRequest("Page and page size should be greater than 0");
    }
    if(pageSize > 100)
    {
        return Results.BadRequest("Page size should be less than 100");
    }

        var result = await db.Files.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Results.Ok(result);
    });

app.MapPost("files/upload", async (FileDbContext db, IFormFile file) =>
    {
        var allFiles = await db.Files.ToListAsync();

        //Find the file with the same name
        var existingFile = allFiles.FirstOrDefault(f => f.Name == file.FileName);
        var createdAt = DateTime.UtcNow;
        if (existingFile != null)
        {
            createdAt = existingFile.CreatedAt;
            //return Results.BadRequest($"File with name {file.FileName} already exists");
        }

        //Check file size and return error if it is more than 200KB
        if (file.Length > 504800)
        {
            return Results.BadRequest("File size is more than 200KB");
        }

        //Save the file in folder
        var internalFileName = Guid.NewGuid().ToString();
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var fileDirectoryPath = Path.Join(path, "vdr5_files");
        if (!Directory.Exists(fileDirectoryPath))
        {
            Directory.CreateDirectory(fileDirectoryPath);
        }
        var filePath = Path.Join(fileDirectoryPath, internalFileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        //Save the file in database
        if (existingFile != null)
        {
            existingFile.Size = file.Length;
            existingFile.UploadedAt = DateTime.UtcNow;
            existingFile.InternalName = internalFileName;

            await db.SaveChangesAsync();

            return Results.Created($"/files/{existingFile.Id}", existingFile);
        }
        else
        {
            var newFile = new VDR5.File
            {
                Name = file.FileName,
                FullPath = file.FileName,
                UploadedAt = DateTime.UtcNow,
                CreatedAt = createdAt,
                InternalName = internalFileName,
                Size = file.Length,
                ContentType = file.ContentType
            };

            db.Files.Add(newFile);
            await db.SaveChangesAsync();

            return Results.Created($"/files/{newFile.Id}", newFile);
        }

    })
    .DisableAntiforgery();

//Create file download endpoint
app.MapGet("/files/download/{id}", async (int id, FileDbContext db) =>
    {
        var file = await db.Files.FindAsync(id);
        if (file == null)
        {
            return Results.NotFound();
        }

        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var fileDirectoryPath = Path.Join(path, "vdr5_files");
        var filePath = Path.Join(fileDirectoryPath, file.InternalName);

        if (!System.IO.File.Exists(filePath))
        {
            return Results.NotFound();
        }

        return Results.File(filePath, contentType: "application/binary", file.Name);
    });

app.Run();
