namespace Bakalauras.data.dtos
{
    public class ChaptersDto
    {
        public record GetChapterDto(int chapterId, string name, string content,int bookId);
        public record CreateChapterDto(string name, string content);
        public record UpdateChapterDto(string? name, string? content);

    }
}
