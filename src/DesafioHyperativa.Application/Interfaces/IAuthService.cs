using DesafioHyperativa.Application.DTOs.Requests;
using DesafioHyperativa.Application.DTOs.Responses;

namespace DesafioHyperativa.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
