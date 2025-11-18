using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WNAB.Data;

namespace WNAB.API.Extensions;

public static class BudgetEndpoints
{
    public static void MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/budget").RequireAuthorization();

        group.MapGet("/snapshot", GetSnapshot);
        group.MapPost("/snapshot", SaveSnapshot);
    }

    [Authorize]
    private static async Task<IResult> GetSnapshot(
        [FromQuery] int month,
        [FromQuery] int year,
        [FromServices] IBudgetSnapshotDbService snapshotService)
    {
        if (month < 1 || month > 12 || year < 2000 || year > 2100)
        {
            return Results.BadRequest("Invalid month or year");
        }

        var snapshot = await snapshotService.GetSnapshotAsync(month, year);

        if (snapshot == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(snapshot);
    }

    [Authorize]
    private static async Task<IResult> SaveSnapshot(
        [FromBody] BudgetSnapshot snapshot,
        [FromServices] IBudgetSnapshotDbService snapshotService)
    {
        if (snapshot == null)
        {
            return Results.BadRequest("Snapshot is required");
        }

        if (snapshot.Month < 1 || snapshot.Month > 12 || snapshot.Year < 2000 || snapshot.Year > 2100)
        {
            return Results.BadRequest("Invalid month or year");
        }

        await snapshotService.SaveSnapshotAsync(snapshot);

        return Results.Ok(new { snapshot.Id, snapshot.Month, snapshot.Year });
    }
}
