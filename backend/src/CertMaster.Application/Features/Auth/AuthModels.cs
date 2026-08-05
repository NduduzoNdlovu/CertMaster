namespace CertMaster.Application.Common.Models;

public record UserDto(Guid Id, string FullName, string Email, string Role, bool IsPremium);

public record AuthResultDto(string AccessToken, string RefreshToken, UserDto User);

public record RegisterRequest(string FullName, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword);

public record ConfirmEmailRequest(string Token);
