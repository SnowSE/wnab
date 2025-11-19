using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WNAB.API.Services;
using WNAB.Data;

namespace WNAB.API.Extensions;

public static class BudgetEndpoints
{
    public static void MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/budget").RequireAuthorization();

        group.MapGet("/snapshot", GetSnapshot);
        group.MapPost("/snapshot", SaveSnapshot);
        group.MapPost("/snapshot/invalidate", InvalidateSnapshots);
    }

    [Authorize]
    private static async Task<IResult> GetSnapshot(
        HttpContext context,
        [FromQuery] int month,
        [FromQuery] int year,
        [FromServices] IBudgetSnapshotDbService snapshotService,
        [FromServices] UserProvisioningService provisioningService)
    {
        if (month < 1 || month > 12 || year < 2000 || year > 2100)
        {
            return Results.BadRequest("Invalid month or year");
        }

        var user = await context.GetCurrentUserAsync(snapshotService.DbContext, provisioningService);
        if (user is null) return Results.Unauthorized();

        var snapshot = await snapshotService.GetSnapshotAsync(month, year, user.Id);

        if (snapshot == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(snapshot);
    }

    [Authorize]
    private static async Task<IResult> SaveSnapshot(
        HttpContext context,
        [FromBody] BudgetSnapshot snapshot,
        [FromServices] IBudgetSnapshotDbService snapshotService,
        [FromServices] UserProvisioningService provisioningService)
    {
        if (snapshot == null)
        {
            return Results.BadRequest("Snapshot is required");
        }

        if (snapshot.Month < 1 || snapshot.Month > 12 || snapshot.Year < 2000 || snapshot.Year > 2100)
        {
            return Results.BadRequest("Invalid month or year");
        }

        var user = await context.GetCurrentUserAsync(snapshotService.DbContext, provisioningService);
        if (user is null) return Results.Unauthorized();

        await snapshotService.SaveSnapshotAsync(snapshot, user.Id);

        return Results.Ok(new { snapshot.Id, snapshot.Month, snapshot.Year });
    }

    [Authorize]
    private static async Task<IResult> InvalidateSnapshots(
        HttpContext context,
        [FromQuery] int month,
        [FromQuery] int year,
        [FromServices] IBudgetSnapshotDbService snapshotService,
        [FromServices] UserProvisioningService provisioningService)
    {
        if (month < 1 || month > 12 || year < 2000 || year > 2100)
        {
            return Results.BadRequest("Invalid month or year");
        }

        var user = await context.GetCurrentUserAsync(snapshotService.DbContext, provisioningService);
        if (user is null) return Results.Unauthorized();

        await snapshotService.InvalidateSnapshotsFromMonthAsync(month, year, user.Id);

        return Results.Ok(new { Message = $"Invalidated snapshots from {month}/{year} onwards" });
    }
}
