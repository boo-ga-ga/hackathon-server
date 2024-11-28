namespace VDR5
{
    public class FileUploadDto
    {
        public string FileName { get; set; }
        IFormFile File  { get; set; }
    }
}