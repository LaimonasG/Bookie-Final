using Microsoft.AspNetCore.Mvc;

namespace Bakalauras.data.dtos
{
    public class ChaptersDto
    {
        public record GetChapterDto(string name, string content,int bookId);
        public record CreateChapterDto(IFormFile File,string Name,string IsFinished);
        public record UpdateChapterDto(string? name, string? content);

    }
}
