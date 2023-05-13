namespace Bakalauras.Auth.Model
{
    public static class BookieRoles
    {
        public const string Admin = nameof(Admin);
        public const string BookieUser = nameof(BookieUser);
        public const string BookieReader = nameof(BookieReader);
        public const string BookieWriter = nameof(BookieWriter);

        public static readonly IReadOnlyCollection<string> All = new[] { Admin, BookieUser, BookieReader, BookieWriter };
    }
}
