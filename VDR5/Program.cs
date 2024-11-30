using VDR5;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<FileDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

var folder = Environment.SpecialFolder.LocalApplicationData;   //var folder = Environment.SpecialFolder.CommonApplicationData;
var path = Environment.GetFolderPath(folder);
var fileDirectoryPath = Path.Join(path, "vdr5_files");

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

    var result = await db.Files.Where(f => f.IsDeleted == false)
        .OrderBy(f => f.Id)
        .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    
    //Map the result to FileDto
    return Results.Ok(result.Select(f => new FileDto
    {
        Id = f.Id,
        Name = f.Name,
        ContentType = f.ContentType,
        Size = f.Size,
        UpdatedAt = f.UpdatedAt,
        CreatedAt = f.CreatedAt
    }));
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
        }

        //Check file size and return error if it is more than 200KB
        if (file.Length > 204800)
        {
            return Results.BadRequest("File size is more than 200KB");
        }

        //Save the file in folder
        var internalFileName = Guid.NewGuid().ToString();
        if (!Directory.Exists(fileDirectoryPath))
        {
            Directory.CreateDirectory(fileDirectoryPath);
        }
        var filePath = Path.Join(fileDirectoryPath, internalFileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        //Save the file in database
        if (existingFile != null)
        {
            existingFile.Size = file.Length;
            existingFile.UpdatedAt = DateTime.UtcNow;
            existingFile.InternalName = internalFileName;
            existingFile.IsDeleted = false;

            await db.SaveChangesAsync();

            //Map the result to FileDto
            return Results.Created("/files/{existingFile.Id}", new FileDto
            {
                Id = existingFile.Id,
                Name = existingFile.Name,
                ContentType = existingFile.ContentType,
                Size = existingFile.Size,
                UpdatedAt = existingFile.UpdatedAt,
                CreatedAt = existingFile.CreatedAt
            });
        }
        else
        {
            var newFile = new VDR5.File
            {
                Name = file.FileName,
                FullPath = file.FileName,
                UpdatedAt = DateTime.UtcNow,
                CreatedAt = createdAt,
                InternalName = internalFileName,
                Size = file.Length,
                ContentType = file.ContentType,
                IsDeleted = false
            };

            db.Files.Add(newFile);
            await db.SaveChangesAsync();

            //Map the result to FileDto
            return Results.Created($"/files/{newFile.Id}", new FileDto
            {
                Id = newFile.Id,
                Name = newFile.Name,
                ContentType = newFile.ContentType,
                Size = newFile.Size,
                UpdatedAt = newFile.UpdatedAt,
                CreatedAt = newFile.CreatedAt
            });
        }

    })
    .DisableAntiforgery();

//Create file download endpoint
app.MapGet("/files/download/{id}", async (int id, FileDbContext db) =>
{
    //Find not deleted file by id
    var file = await db.Files.FindAsync(id);
    if (file == null || file.IsDeleted)
    {
        return Results.NotFound();
    }

    var filePath = Path.Join(fileDirectoryPath, file.InternalName);

    if (!System.IO.File.Exists(filePath))
    {
        return Results.NotFound();
    }

    return Results.File(filePath, contentType: "application/binary", file.Name);
});

app.MapDelete("/files/{id}", async (int id, FileDbContext db) =>
{
    var file = await db.Files.FindAsync(id);
    if (file == null)
    {
        return Results.NotFound();
    }

    file.IsDeleted = true;
    file.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.Run();