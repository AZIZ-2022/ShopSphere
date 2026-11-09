using ECommerce.Application.Admin;
using ECommerce.Application.Common.Models;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Admin;

public sealed class UserAdminService : IUserAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<PagedResult<AdminUserListItem>> ListAsync(UserAdminFilter filter, CancellationToken ct = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 100);

        var adminRoleId = await _db.Roles.Where(r => r.NormalizedName == "ADMIN").Select(r => r.Id).FirstOrDefaultAsync(ct);
        var adminUserIds = adminRoleId is null
            ? new HashSet<string>()
            : (await _db.UserRoles.Where(ur => ur.RoleId == adminRoleId).Select(ur => ur.UserId).ToListAsync(ct)).ToHashSet();

        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(u => EF.Functions.Like(u.FullName, $"%{term}%") || EF.Functions.Like(u.Email!, $"%{term}%"));
        }

        if (string.Equals(filter.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            query = query.Where(u => adminUserIds.Contains(u.Id));
        else if (string.Equals(filter.Role, "Customer", StringComparison.OrdinalIgnoreCase))
            query = query.Where(u => !adminUserIds.Contains(u.Id));

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(u => new
            {
                u.Id, u.FullName, u.Email, u.PhoneNumber, u.IsActive, u.CreatedAt, u.LastLoginAt,
                OrderCount = _db.Orders.Count(o => o.UserId == u.Id)
            })
            .ToListAsync(ct);

        var items = rows.Select(u => new AdminUserListItem
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? "",
            PhoneNumber = u.PhoneNumber,
            IsActive = u.IsActive,
            IsAdmin = adminUserIds.Contains(u.Id),
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt,
            OrderCount = u.OrderCount
        }).ToList();

        return new PagedResult<AdminUserListItem>(items, page, size, total);
    }

    public async Task<Result> SetActiveAsync(string userId, bool active, string actingUserId, CancellationToken ct = default)
    {
        if (userId == actingUserId && !active)
            return Result.Failure("You can't deactivate your own account.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Result.Failure("User not found.");

        user.IsActive = active;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result.Failure(string.Join(" ", result.Errors.Select(e => e.Description)));

        if (!active)
            await _userManager.UpdateSecurityStampAsync(user); // invalidates existing sessions

        return Result.Success();
    }

    public async Task<Result> SetAdminRoleAsync(string userId, bool isAdmin, string actingUserId, CancellationToken ct = default)
    {
        if (userId == actingUserId && !isAdmin)
            return Result.Failure("You can't remove your own admin access.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Result.Failure("User not found.");

        var inRole = await _userManager.IsInRoleAsync(user, Roles.Admin);
        if (isAdmin && !inRole)
        {
            await _userManager.AddToRoleAsync(user, Roles.Admin);
        }
        else if (!isAdmin && inRole)
        {
            var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            if (admins.Count <= 1)
                return Result.Failure("There must be at least one administrator.");
            await _userManager.RemoveFromRoleAsync(user, Roles.Admin);
        }

        await _userManager.UpdateSecurityStampAsync(user);
        return Result.Success();
    }
}
