namespace Bakalauras.data.dtos;

public record CommentDto(int Id, int EntityId, string EntityType, DateTime Date, string Content,string UserId, string Username);
public record CreateCommentDto(string Content);
public record UpdateCommentDto(string Content);
