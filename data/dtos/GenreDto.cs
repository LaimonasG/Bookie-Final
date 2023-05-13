namespace Bakalauras.data.dtos
{
    public record GenreDto(int Id, string? Name);
    public record CreateGenreDto(string? name);
    public record UpdateGenreDto(string? name);


}
