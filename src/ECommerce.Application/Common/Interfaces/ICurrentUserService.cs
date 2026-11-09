namespace ECommerce.Application.Common.Interfaces;

/// <summary>
/// Ambient information about the user making the current request.
/// Implemented in the Web layer over <c>IHttpContextAccessor</c>.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
