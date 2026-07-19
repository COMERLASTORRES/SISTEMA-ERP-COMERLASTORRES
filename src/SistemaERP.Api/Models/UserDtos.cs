using System;
using System.Collections.Generic;

namespace SistemaERP.Api.Models;

public class RoleSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<RoleSummaryDto> Roles { get; set; } = new();
}

public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AssignUserRolesDto
{
    public List<string> RoleIds { get; set; } = new();
}
