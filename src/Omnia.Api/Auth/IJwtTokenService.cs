using Omnia.Api.Data;

namespace Omnia.Api.Auth;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
