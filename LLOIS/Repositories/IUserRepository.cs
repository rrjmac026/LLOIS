using System;
using System.Collections.Generic;
using System.Text;

namespace LLOIS.Repositories;

using LLOIS.Models;

public interface IUserRepository
{
    User? GetByUsername(string username);
}