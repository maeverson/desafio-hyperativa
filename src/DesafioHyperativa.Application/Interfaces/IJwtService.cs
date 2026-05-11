using DesafioHyperativa.Domain.Entities;

namespace DesafioHyperativa.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
}
