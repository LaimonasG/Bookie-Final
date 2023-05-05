using Microsoft.AspNetCore.Mvc;

namespace Bakalauras.data.dtos
{
    public class ChaptersDto
    {
        public record GetChapterDto(int Id,int BookId,string UserId, string Name,string Content);
        public record CreatedChapterDto(string name, string content, int bookId,int chargedUsersCount);
        public record CreateChapterDto(IFormFile File,string Name,string IsFinished);
    }
}
