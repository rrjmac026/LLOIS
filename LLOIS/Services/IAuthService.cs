namespace LLOIS.Services;

using LLOIS.Models;

public interface IAuthService
{
    User? Login(string username, string password);
    void LogAction(User user, string action, string details);
    IEnumerable<User> GetAllUsers();
    void CreateUser(string username, string password, UserRole role);
    void ResetPassword(int userId, string newPassword);
    void SetActiveStatus(int userId, bool isActive);
    IEnumerable<AuditLog> GetRecentLogs(int count = 200);
}
