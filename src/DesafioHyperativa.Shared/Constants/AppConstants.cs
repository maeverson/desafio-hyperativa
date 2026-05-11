namespace DesafioHyperativa.Shared.Constants;

public static class AppConstants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }

    public static class Card
    {
        public const int MinLength = 13;
        public const int MaxLength = 19;
    }

    public static class Pagination
    {
        public const int DefaultPageSize = 50;
        public const int MaxPageSize = 500;
    }

    public static class Cache
    {
        public const int CardExistsTtlSeconds = 300;
    }
}
