namespace Bakalauras.data.dtos
{
    public record SetRoleDto(string roleName);
    public record UserAdminPageDto(string id,string userName,string email,int isBlocked,double points);

    public record DeleteCommentDto(int commentId, int entityId, string type);
    public record DeleteBookDto(int bookId);

    public record DeleteTextDto(int textId);
    public record UpdateBookStatus(int status,int bookId,string statusComment);
    public record UpdateTextStatus(int status, int textId, string statusComment);





}