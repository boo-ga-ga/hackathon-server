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
        var createdAt = DateTime.UtcNow;
        if (existingFile != null)
        {
            createdAt = existingFile.CreatedAt;
            //return Results.BadRequest($"File with name {file.FileName} already exists");
        }

        //Check file size and return error if it is more than 200KB
        if (file.Length > 204800)
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
                Size = file.Length
            };

            db.Files.Add(newFile);
            await db.SaveChangesAsync();

            return Results.Created($"/files/{newFile.Id}", newFile);
        }

    })
    .DisableAntiforgery();
app.Run();
