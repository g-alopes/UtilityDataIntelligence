using UtilityData.Infrastructure.Identity;
namespace UtilityData.Api.Authentication;
public interface ITokenService
{
    string CreateAccessToken(ApplicationUser user, Guid tenantId, string tenantRole);
}
