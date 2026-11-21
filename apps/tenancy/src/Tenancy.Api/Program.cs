using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Tenancy.Api;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Tenancy") ?? throw new InvalidOperationException("ConnectionStrings:Tenancy is required.");
var issuer = builder.Configuration["Clerk:Issuer"];
var audience = builder.Configuration["Clerk:Audience"];
if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
    throw new InvalidOperationException("Clerk:Issuer and Clerk:Audience are required.");
var expectedDatabase = builder.Configuration["Tenancy:DatabaseName"] ?? "tenancy";
if (!string.Equals(new NpgsqlConnectionStringBuilder(connection).Database, expectedDatabase, StringComparison.Ordinal))
    throw new InvalidOperationException("The tenancy connection must target the configured tenancy database.");
builder.Services.AddDbContext<TenancyDb>(options => options.UseNpgsql(connection));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.Authority = issuer;
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = issuer, ValidateAudience = true, ValidAudience = audience };
});
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<TenancyDb>().Database.MigrateAsync();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

var api = app.MapGroup("/api").RequireAuthorization();
// Endpoint filters run before a result is written, so commit failures cannot
// accidentally send a successful response to the caller.
api.AddEndpointFilter(async (invocation, next) =>
{
    var context = invocation.HttpContext;
    var subject = context.User.FindFirstValue("sub")?.Trim();
    if (string.IsNullOrEmpty(subject) || subject.Length > 255) return Results.Unauthorized();
    var db = context.RequestServices.GetRequiredService<TenancyDb>();
    await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, context.RequestAborted);
    try
    {
        var now = DateTimeOffset.UtcNow;
        var admins = (builder.Configuration["Tenancy:BootstrapAdminClerkUserIds"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var id = Guid.NewGuid();
        var isAdmin = admins.Contains(subject, StringComparer.Ordinal);
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO accounts (\"Id\", \"ClerkUserId\", \"IsPlatformAdministrator\", \"Status\", \"CreatedAt\", \"UpdatedAt\", \"LastSeenAt\") VALUES ({id}, {subject}, {isAdmin}, 'Active', {now}, {now}, {now}) ON CONFLICT (\"ClerkUserId\") DO NOTHING", context.RequestAborted);
        var actor = await db.Accounts.SingleAsync(x => x.ClerkUserId == subject, context.RequestAborted);
        if (actor.Status != "Active") return Results.Forbid();
        actor.LastSeenAt = now;
        if (inserted == 1) Events.Account(db, actor.Id, "account.created");
        context.Items["actor"] = actor;
        var result = await next(invocation);
        if (result is IStatusCodeHttpResult status && status.StatusCode >= 400) return result;
        await db.SaveChangesAsync(context.RequestAborted);
        await transaction.CommitAsync(context.RequestAborted);
        return result;
    }
    catch (Exception ex) when (ex is DbUpdateConcurrencyException || ex is DbUpdateException || ex is PostgresException)
    {
        return Results.Problem(statusCode: ex is DbUpdateConcurrencyException ? 412 : 409, title: "The request conflicts with current state.");
    }
});
api.MapGet("/accounts/me", (HttpContext ctx) => Results.Ok(Responses.Account(Actor(ctx))));
api.MapGet("/accounts", async (HttpContext ctx, TenancyDb db, int? limit, Guid? cursor, string? status, CancellationToken ct) =>
{
    if (!Actor(ctx).IsPlatformAdministrator) return Results.Forbid();
    if (limit is < 1 or > 100 || (status is not null && status is not ("Active" or "Deleted" or "Blocked"))) return Results.BadRequest();
    var query = db.Accounts.AsNoTracking().AsQueryable();
    if (cursor.HasValue) query = query.Where(x => x.Id.CompareTo(cursor.Value) > 0);
    if (status is not null) query = query.Where(x => x.Status == status);
    var rows = await query.OrderBy(x => x.Id).Take(limit ?? 25).ToListAsync(ct);
    return Results.Ok(new { items = rows.Select(Responses.Account), nextCursor = rows.Count == (limit ?? 25) ? rows[^1].Id.ToString() : null });
});
api.MapGet("/accounts/{accountId:guid}", async (Guid accountId, HttpContext ctx, TenancyDb db, CancellationToken ct) =>
{
    if (!Actor(ctx).IsPlatformAdministrator) return Results.Forbid();
    var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId, ct);
    return account is null ? Results.NotFound() : Results.Ok(Responses.Account(account));
});
api.MapPut("/accounts/{accountId:guid}/platform-administrator", async (Guid accountId, AdminRequest request, HttpContext ctx, TenancyDb db, CancellationToken ct) =>
{
    if (!Actor(ctx).IsPlatformAdministrator) return Results.Forbid();
    var account = await db.Accounts.SingleOrDefaultAsync(x => x.Id == accountId && x.Status == "Active", ct);
    if (account is null) return Results.NotFound();
    if (account.IsPlatformAdministrator && !request.IsAdmin && await db.Accounts.CountAsync(x => x.Status == "Active" && x.IsPlatformAdministrator, ct) <= 1) return Results.Conflict();
    if (account.IsPlatformAdministrator != request.IsAdmin) { account.IsPlatformAdministrator = request.IsAdmin; account.UpdatedAt = DateTimeOffset.UtcNow; Events.Account(db, account.Id, "account.platform_administrator.changed"); }
    return Results.Ok(Responses.Account(account));
});

var organisations = api.MapGroup("/organisations");
organisations.MapGet("/", async (HttpContext ctx, TenancyDb db, int? limit, Guid? cursor, CancellationToken ct) =>
{
    if (limit is < 1 or > 100) return Results.BadRequest();
    var actor = Actor(ctx).Id;
    var query = db.Organisations.AsNoTracking().Where(x => x.Status == "Active" && (x.OwnerAccountId == actor || db.Members.Any(m => m.OrganisationId == x.Id && m.AccountId == actor && m.Status == "Active")));
    if (cursor.HasValue) query = query.Where(x => x.Id.CompareTo(cursor.Value) > 0);
    var rows = await query.OrderBy(x => x.Id).Take(limit ?? 25).ToListAsync(ct);
    return Results.Ok(new { items = rows.Select(Responses.Organisation), nextCursor = rows.Count == (limit ?? 25) ? rows[^1].Id.ToString() : null });
});
organisations.MapPost("/", (OrganisationRequest request, HttpContext ctx, TenancyDb db) =>
{
    if (!Valid(request)) return Results.BadRequest();
    var now = DateTimeOffset.UtcNow;
    var item = new OrganisationRow { Id = Guid.NewGuid(), Name = request.Name!.Trim(), Description = request.Description, OwnerAccountId = Actor(ctx).Id, CreatedAt = now, UpdatedAt = now };
    db.Organisations.Add(item); Events.Organisation(db, item, Actor(ctx).Id, "organisation.created");
    ctx.Response.Headers.ETag = Tag(item);
    return Results.Created($"/api/organisations/{item.Id}", Responses.Organisation(item));
});
organisations.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, TenancyDb db, CancellationToken ct) =>
{
    var item = await Accessible(db, id, Actor(ctx).Id, ct);
    if (item is null) return Results.NotFound();
    ctx.Response.Headers.ETag = Tag(item);
    if (item.OwnerAccountId != Actor(ctx).Id) return Results.Ok(new { organisation = Responses.Organisation(item), members = (object?)null });
    var members = await db.Members.Where(x => x.OrganisationId == id && x.Status == "Active").Select(x => x.AccountId).ToListAsync(ct);
    return Results.Ok(new { organisation = Responses.Organisation(item), members });
});
organisations.MapPatch("/{id:guid}", async (Guid id, OrganisationRequest request, HttpContext ctx, TenancyDb db, CancellationToken ct) =>
{
    var (item, error) = await Owned(db, id, ctx, ct); if (error is not null) return error;
    if (!Valid(request)) return Results.BadRequest();
    item!.Name = request.Name!.Trim(); item.Description = request.Description; Touch(item); Events.Organisation(db, item, Actor(ctx).Id, "organisation.updated");
    ctx.Response.Headers.ETag = Tag(item); return Results.Ok(Responses.Organisation(item));
});
organisations.MapPost("/{id:guid}/members", async (Guid id, MemberRequest request, HttpContext ctx, TenancyDb db, CancellationToken ct) =>
{
    var (item, error) = await Owned(db, id, ctx, ct, false); if (error is not null) return error;
    if (request.AccountId == Guid.Empty || request.AccountId == item!.OwnerAccountId) return Results.BadRequest();
    if (!await db.Accounts.AnyAsync(x => x.Id == request.AccountId && x.Status == "Active", ct)) return Results.NotFound();
    if (await db.Members.AnyAsync(x => x.OrganisationId == id && x.AccountId == request.AccountId && x.Status == "Active", ct)) return Results.Conflict();
    db.Members.Add(new MemberRow { Id = Guid.NewGuid(), OrganisationId = id, AccountId = request.AccountId, GrantedByAccountId = Actor(ctx).Id, GrantedAt = DateTimeOffset.UtcNow });
    item.AccessRevision++; Touch(item); Events.Organisation(db, item, Actor(ctx).Id, "organisation.member.granted", request.AccountId);
    ctx.Response.Headers.ETag = Tag(item); return Results.Created($"/api/organisations/{id}/members/{request.AccountId}", new { request.AccountId });
});
organisations.MapGet("/{id:guid}/members", async (Guid id, HttpContext ctx, TenancyDb db, int? limit, Guid? cursor, CancellationToken ct) =>
{
    if (limit is < 1 or > 100) return Results.BadRequest();
    var item = await Accessible(db, id, Actor(ctx).Id, ct); if (item is null) return Results.NotFound(); if (item.OwnerAccountId != Actor(ctx).Id) return Results.Forbid();
    var query = db.Members.AsNoTracking().Where(x => x.OrganisationId == id && x.Status == "Active");
    if (cursor.HasValue) query = query.Where(x => x.AccountId.CompareTo(cursor.Value) > 0);
    var rows = await query.OrderBy(x => x.AccountId).Take(limit ?? 25).ToListAsync(ct);
    return Results.Ok(new { items = rows.Select(x => new { x.AccountId, x.GrantedAt }), nextCursor = rows.Count == (limit ?? 25) ? rows[^1].AccountId.ToString() : null });
});
organisations.MapDelete("/{id:guid}/members/{accountId:guid}", async (Guid id, Guid accountId, HttpContext ctx, TenancyDb db, CancellationToken ct) =>
{
    var (item, error) = await Owned(db, id, ctx, ct); if (error is not null) return error;
    var member = await db.Members.SingleOrDefaultAsync(x => x.OrganisationId == id && x.AccountId == accountId && x.Status == "Active", ct); if (member is null) return Results.NotFound();
    Revoke(member, Actor(ctx).Id); item!.AccessRevision++; Touch(item); Events.Organisation(db, item, Actor(ctx).Id, "organisation.member.revoked", accountId);
    ctx.Response.Headers.ETag = Tag(item); return Results.NoContent();
});
organisations.MapPost("/{id:guid}/ownership-transfers", async (Guid id, MemberRequest request, HttpContext ctx, TenancyDb db, CancellationToken ct) =>
{
    var (item, error) = await Owned(db, id, ctx, ct); if (error is not null) return error;
    var member = await db.Members.SingleOrDefaultAsync(x => x.OrganisationId == id && x.AccountId == request.AccountId && x.Status == "Active", ct);
    if (member is null || !await db.Accounts.AnyAsync(x => x.Id == request.AccountId && x.Status == "Active", ct)) return Results.Conflict();
    var previous = item!.OwnerAccountId; Revoke(member, previous);
    db.Members.Add(new MemberRow { Id = Guid.NewGuid(), OrganisationId = id, AccountId = previous, GrantedByAccountId = previous, GrantedAt = DateTimeOffset.UtcNow });
    item.OwnerAccountId = request.AccountId; item.AccessRevision++; Touch(item); Events.Organisation(db, item, previous, "organisation.owner.transferred", request.AccountId);
    ctx.Response.Headers.ETag = Tag(item); return Results.Ok(Responses.Organisation(item));
});
organisations.MapDelete("/{id:guid}", async (Guid id, HttpContext ctx, TenancyDb db, CancellationToken ct) =>
{
    var (item, error) = await Owned(db, id, ctx, ct); if (error is not null) return error;
    item!.Status = "Deleting"; item.AccessRevision++; Touch(item);
    foreach (var member in await db.Members.Where(x => x.OrganisationId == id && x.Status == "Active").ToListAsync(ct)) Revoke(member, Actor(ctx).Id);
    Events.Organisation(db, item, Actor(ctx).Id, "organisation.deleting");
    return Results.Accepted();
});

var internalApi = app.MapGroup("/internal");
internalApi.AddEndpointFilter(async (invocation, next) =>
{
    var configured = builder.Configuration["Tenancy:ServiceKey"];
    var provided = invocation.HttpContext.Request.Headers["X-Tenancy-Service-Key"].ToString();
    if (string.IsNullOrEmpty(configured) || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(configured), Encoding.UTF8.GetBytes(provided))) return Results.Unauthorized();
    return await next(invocation);
});
internalApi.MapPost("/accounts/resolve", async (ResolveRequest request, TenancyDb db, CancellationToken ct) =>
{
    var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.ClerkUserId == request.ClerkUserId && x.Status == "Active", ct);
    return account is null ? Results.NotFound() : Results.Ok(new { accountId = account.Id });
});
internalApi.MapPost("/organisations/lookup", async (LookupRequest request, TenancyDb db, CancellationToken ct) =>
{
    var item = await db.Organisations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.OrganisationId, ct);
    return item is null ? Results.NotFound() : Results.Ok(new { organisationId = item.Id, status = item.Status, accessRevision = item.AccessRevision });
});
internalApi.MapPost("/organisation-access-decisions", async (AccessRequest request, TenancyDb db, CancellationToken ct) =>
{
    var item = await db.Organisations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.OrganisationId, ct);
    var role = "None";
    if (item is not null && item.Status == "Active" && await db.Accounts.AnyAsync(x => x.Id == request.AccountId && x.Status == "Active", ct))
        role = item.OwnerAccountId == request.AccountId ? "Owner" : await db.Members.AnyAsync(x => x.OrganisationId == item.Id && x.AccountId == request.AccountId && x.Status == "Active", ct) ? "Member" : "None";
    return Results.Ok(new { role, accessRevision = item?.AccessRevision ?? 0, expiresAt = DateTimeOffset.UtcNow.AddSeconds(15) });
});
app.Run();

static AccountRow Actor(HttpContext ctx) => (AccountRow)ctx.Items["actor"]!;
static bool Valid(OrganisationRequest request) => !string.IsNullOrWhiteSpace(request.Name) && request.Name.Trim().Length <= 256 && (request.Description is null || request.Description.Length <= 4000);
static string Tag(OrganisationRow item) => $"\"{item.Version}\"";
static void Touch(OrganisationRow item) { item.Version++; item.UpdatedAt = DateTimeOffset.UtcNow; }
static void Revoke(MemberRow member, Guid actor) { member.Status = "Revoked"; member.RevokedAt = DateTimeOffset.UtcNow; member.RevokedByAccountId = actor; }
static async Task<OrganisationRow?> Accessible(TenancyDb db, Guid id, Guid actor, CancellationToken ct) => await db.Organisations.SingleOrDefaultAsync(x => x.Id == id && x.Status == "Active" && (x.OwnerAccountId == actor || db.Members.Any(m => m.OrganisationId == x.Id && m.AccountId == actor && m.Status == "Active")), ct);
static async Task<(OrganisationRow? Item, IResult? Error)> Owned(TenancyDb db, Guid id, HttpContext ctx, CancellationToken ct, bool match = true)
{
    var item = await Accessible(db, id, Actor(ctx).Id, ct);
    if (item is null) return (null, Results.NotFound());
    if (item.OwnerAccountId != Actor(ctx).Id) return (null, Results.Forbid());
    if (match && ctx.Request.Headers.IfMatch != Tag(item)) return (null, Results.StatusCode(StatusCodes.Status412PreconditionFailed));
    return (item, null);
}
public sealed record OrganisationRequest(string? Name, string? Description);
public sealed record MemberRequest(Guid AccountId);
public sealed record AdminRequest(bool IsAdmin);
public sealed record ResolveRequest(string ClerkUserId);
public sealed record LookupRequest(Guid OrganisationId);
public sealed record AccessRequest(Guid OrganisationId, Guid AccountId);
public static class Responses
{
    public static object Account(AccountRow x) => new { x.Id, x.ClerkUserId, x.IsPlatformAdministrator, x.Status, x.CreatedAt, x.UpdatedAt, x.LastSeenAt };
    public static object Organisation(OrganisationRow x) => new { x.Id, x.Name, x.Description, x.OwnerAccountId, x.Status, x.AccessRevision, rowVersion = x.Version, x.CreatedAt, x.UpdatedAt };
}
public static class Events
{
    public static void Account(TenancyDb db, Guid id, string type) => Add(db, id, type, null, null);
    public static void Organisation(TenancyDb db, OrganisationRow item, Guid actor, string type, Guid? accountId = null)
    {
        var now = DateTimeOffset.UtcNow;
        db.Audit.Add(new AuditRow { Id = Guid.NewGuid(), OrganisationId = item.Id, ActorAccountId = actor, EventType = type, OccurredAt = now });
        Add(db, item.Id, type, item.AccessRevision, accountId);
    }
    private static void Add(TenancyDb db, Guid id, string type, long? revision, Guid? accountId)
    {
        var eventId = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var prefix = revision.HasValue ? "organisation" : "account";
        db.Outbox.Add(new OutboxRow
        {
            Id = eventId,
            EventType = type,
            Subject = $"agent-platform.{prefix}.{id}.events",
            OccurredAt = now,
            Payload = JsonSerializer.Serialize(new { eventId, eventType = type, schemaVersion = 1, occurredAt = now, correlationId = (string?)null, causationId = (string?)null, organisationId = revision.HasValue ? id : (Guid?)null, accountId = revision.HasValue ? accountId : id, data = new { accessRevision = revision } })
        });
    }
}
