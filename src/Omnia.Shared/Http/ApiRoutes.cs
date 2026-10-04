namespace Omnia.Shared.Http;

public static class ApiRoutes
{
    public const string Register = "auth/register";
    public const string Login = "auth/login";
    public const string Me = "auth/me";
    public const string Clips = "clips";

    public static string Clip(Guid id) => $"clips/{id}";
}
