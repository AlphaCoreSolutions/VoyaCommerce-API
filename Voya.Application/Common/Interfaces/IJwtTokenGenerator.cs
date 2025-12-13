namespace Voya.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
	string GenerateAccessToken(Guid userId, string email, bool isGoldMember);
	string GenerateRefreshToken();
}