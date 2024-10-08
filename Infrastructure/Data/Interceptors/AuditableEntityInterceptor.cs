﻿using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using PCBuilder.Domain.Common;

namespace Infrastructure.Data.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
private readonly IUser _user;
private readonly DateTime _dateTime;

public AuditableEntityInterceptor(
    IUser user,
    DateTime dateTime
    )
{
    _user = user;
    _dateTime = dateTime;
}

public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
{
    UpdateEntities(eventData.Context);

    return base.SavingChanges(eventData, result);
}

public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
{
    UpdateEntities(eventData.Context);

    return base.SavingChangesAsync(eventData, result, cancellationToken);
}

public void UpdateEntities(DbContext? context)
{
    if (context == null) return;

    foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedBy = _user.Id;
            entry.Entity.Created = _dateTime;
        }

        if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
        {
            entry.Entity.LastModifiedBy = _user.Id;
            entry.Entity.LastModified = _dateTime;
        }
    }
}
}

public static class Extensions
{
public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
    entry.References.Any(r =>
        r.TargetEntry != null &&
        r.TargetEntry.Metadata.IsOwned() &&
        (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}

