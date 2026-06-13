using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Services;

using LLOIS.Models;

public interface IAuthService
{
    User? Login(string username, string password);
    void LogAction(User user, string action, string details);
}