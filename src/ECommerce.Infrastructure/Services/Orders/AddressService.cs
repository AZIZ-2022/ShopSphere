using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services.Orders;

public sealed class AddressService : IAddressService
{
    private readonly ApplicationDbContext _db;

    public AddressService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AddressDto>> ListAsync(string userId, CancellationToken ct = default)
    {
        var rows = await _db.Addresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.Id)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<AddressDto?> GetAsync(string userId, int id, CancellationToken ct = default)
    {
        var row = await _db.Addresses.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Id == id, ct);
        return row is null ? null : Map(row);
    }

    public async Task<Result<int>> CreateAsync(string userId, AddressEditModel model, CancellationToken ct = default)
    {
        var error = Validate(model);
        if (error is not null) return Result<int>.Failure(error);

        var isFirst = !await _db.Addresses.AnyAsync(a => a.UserId == userId, ct);
        var entity = new Address
        {
            UserId = userId,
            FullName = model.FullName.Trim(),
            Phone = model.Phone.Trim(),
            Line1 = model.Line1.Trim(),
            Line2 = string.IsNullOrWhiteSpace(model.Line2) ? null : model.Line2.Trim(),
            City = model.City.Trim(),
            State = string.IsNullOrWhiteSpace(model.State) ? null : model.State.Trim(),
            PostalCode = model.PostalCode.Trim(),
            Country = model.Country.Trim(),
            AddressType = AddressType.Both,
            IsDefault = model.IsDefault || isFirst
        };

        _db.Addresses.Add(entity);
        await _db.SaveChangesAsync(ct);

        if (entity.IsDefault)
            await ClearOtherDefaultsAsync(userId, entity.Id, ct);

        return Result<int>.Success(entity.Id);
    }

    public async Task<Result> UpdateAsync(string userId, AddressEditModel model, CancellationToken ct = default)
    {
        var entity = await _db.Addresses.FirstOrDefaultAsync(a => a.UserId == userId && a.Id == model.Id, ct);
        if (entity is null) return Result.Failure("Address not found.");

        var error = Validate(model);
        if (error is not null) return Result.Failure(error);

        entity.FullName = model.FullName.Trim();
        entity.Phone = model.Phone.Trim();
        entity.Line1 = model.Line1.Trim();
        entity.Line2 = string.IsNullOrWhiteSpace(model.Line2) ? null : model.Line2.Trim();
        entity.City = model.City.Trim();
        entity.State = string.IsNullOrWhiteSpace(model.State) ? null : model.State.Trim();
        entity.PostalCode = model.PostalCode.Trim();
        entity.Country = model.Country.Trim();
        entity.IsDefault = model.IsDefault;

        await _db.SaveChangesAsync(ct);
        if (entity.IsDefault)
            await ClearOtherDefaultsAsync(userId, entity.Id, ct);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string userId, int id, CancellationToken ct = default)
    {
        var entity = await _db.Addresses.FirstOrDefaultAsync(a => a.UserId == userId && a.Id == id, ct);
        if (entity is null) return Result.Failure("Address not found.");

        _db.Addresses.Remove(entity); // soft delete via interceptor
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetDefaultAsync(string userId, int id, CancellationToken ct = default)
    {
        var entity = await _db.Addresses.FirstOrDefaultAsync(a => a.UserId == userId && a.Id == id, ct);
        if (entity is null) return Result.Failure("Address not found.");

        entity.IsDefault = true;
        await _db.SaveChangesAsync(ct);
        await ClearOtherDefaultsAsync(userId, id, ct);
        return Result.Success();
    }

    private async Task ClearOtherDefaultsAsync(string userId, int keepId, CancellationToken ct) =>
        await _db.Addresses
            .Where(a => a.UserId == userId && a.Id != keepId && a.IsDefault)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), ct);

    private static string? Validate(AddressEditModel m)
    {
        if (string.IsNullOrWhiteSpace(m.FullName)) return "Full name is required.";
        if (string.IsNullOrWhiteSpace(m.Phone)) return "Phone number is required.";
        if (string.IsNullOrWhiteSpace(m.Line1)) return "Address line 1 is required.";
        if (string.IsNullOrWhiteSpace(m.City)) return "City is required.";
        if (string.IsNullOrWhiteSpace(m.PostalCode)) return "Postal code is required.";
        if (string.IsNullOrWhiteSpace(m.Country)) return "Country is required.";
        return null;
    }

    private static AddressDto Map(Address a) => new()
    {
        Id = a.Id,
        FullName = a.FullName,
        Phone = a.Phone,
        Line1 = a.Line1,
        Line2 = a.Line2,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
        AddressType = a.AddressType,
        IsDefault = a.IsDefault
    };
}
