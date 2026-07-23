using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace services.Logging
{
    public static class AppLogger
    {
        public static void Auth(
            ILogger logger,
            string action,
            Guid? userId = null,
            string? email = null,
            string? ip = null,
            bool success = true,
            string? details = null,
            [CallerMemberName] string caller = "")
        {
            var message = BuildMessage(action, success, caller, userId, email, ip, details);

            if (success)
                logger.LogInformation(message);
            else
                logger.LogWarning(message);
        }

        public static void PasswordChange(
            ILogger logger,
            Guid userId,
            string? ip = null,
            bool success = true,
            string? reason = null,
            [CallerMemberName] string caller = "")
        {
            var action = "Password change";
            var details = reason;
            var message = BuildMessage(action, success, caller, userId, null, ip, details);

            if (success)
                logger.LogInformation(message);
            else
                logger.LogWarning(message);
        }

        public static void AccountDeletion(
            ILogger logger,
            Guid userId,
            string? email = null,
            string? ip = null,
            [CallerMemberName] string caller = "")
        {
            var message = BuildMessage("Account deletion", true, caller, userId, email, ip);
            logger.LogWarning(message);
        }

        public static void Error(
            ILogger logger,
            Exception ex,
            string? details = null,
            Guid? userId = null,
            [CallerMemberName] string caller = "")
        {
            var message = details != null
                ? $"[{caller}] Error: {details} | {ex.Message}"
                : $"[{caller}] Error: {ex.Message}";

            logger.LogError(ex, message);
        }

        public static void Info(
            ILogger logger,
            string message,
            Guid? userId = null,
            [CallerMemberName] string caller = "")
        {
            var formatted = userId.HasValue
                ? $"[{caller}] {message} | User: {userId}"
                : $"[{caller}] {message}";

            logger.LogInformation(formatted);
        }

        private static string BuildMessage(
            string action,
            bool success,
            string caller,
            Guid? userId = null,
            string? email = null,
            string? ip = null,
            string? details = null)
        {
            var status = success ? "SUCCESS" : "FAILED";
            var parts = new List<string> { $"[{caller}] {action} [{status}]" };

            if (userId.HasValue)
                parts.Add($"User: {userId}");

            if (!string.IsNullOrEmpty(email))
                parts.Add($"Email: {email}");

            if (!string.IsNullOrEmpty(ip))
                parts.Add($"IP: {ip}");

            if (!string.IsNullOrEmpty(details))
                parts.Add(details);

            return string.Join(" | ", parts);
        }
    }
}
