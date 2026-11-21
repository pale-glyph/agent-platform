using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KnowledgeBase.Service.Domain;
using KnowledgeBase.Service.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Service.Services;

/// <summary>Control-plane persistence boundary for knowledge bases. All document writes create an auditable job and outbox record in one transaction.</summary>
public sealed class KnowledgeBaseService(KnowledgeBaseDbContext db, TimeProvider clock)
{
    public async Task<IReadOnlyList<KnowledgeBaseRecord>> ListAsync(Guid projectId, int limit, CancellationToken ct) => await db.KnowledgeBases.AsNoTracking().Where(x => x.ProjectId == projectId && x.DeletedAt == null).OrderBy(x => x.Name).Take(limit).ToArrayAsync(ct);
    public Task<KnowledgeBaseRecord?> FindAsync(Guid projectId, Guid id, CancellationToken ct) => db.KnowledgeBases.AsNoTracking().SingleOrDefaultAsync(x => x.ProjectId == projectId && x.Id == id && x.DeletedAt == null, ct);
    public async Task<KnowledgeBaseRecord?> CreateAsync(Guid projectId, string name, string? description, CancellationToken ct) { var item = new KnowledgeBaseRecord(Guid.NewGuid(), projectId, name, description, clock.GetUtcNow()); db.KnowledgeBases.Add(item); await db.SaveChangesAsync(ct); return item; }
    public async Task<KnowledgeBaseRecord?> UpdateAsync(Guid projectId, Guid id, string name, string? description, CancellationToken ct) { var item = await db.KnowledgeBases.SingleOrDefaultAsync(x => x.ProjectId == projectId && x.Id == id && x.DeletedAt == null, ct); if (item is null) return null; item.Update(name, description, clock.GetUtcNow()); await db.SaveChangesAsync(ct); return item; }
    public async Task<bool> DeleteAsync(Guid projectId, Guid id, CancellationToken ct) { var item = await db.KnowledgeBases.SingleOrDefaultAsync(x => x.ProjectId == projectId && x.Id == id && x.DeletedAt == null, ct); if (item is null) return false; var now = clock.GetUtcNow(); item.Delete(now); foreach (var document in await db.KnowledgeBaseDocuments.Where(x => x.KnowledgeBaseId == id && x.DeletedAt == null).ToListAsync(ct)) document.Delete(now); await db.SaveChangesAsync(ct); return true; }
    public async Task<IReadOnlyList<KnowledgeBaseDocument>> ListDocumentsAsync(Guid projectId, Guid kbId, CancellationToken ct) => await db.KnowledgeBaseDocuments.AsNoTracking().Where(x => x.ProjectId == projectId && x.KnowledgeBaseId == kbId && x.DeletedAt == null).OrderBy(x => x.Title).ToArrayAsync(ct);
    public Task<KnowledgeBaseDocument?> FindDocumentAsync(Guid projectId, Guid kbId, Guid id, CancellationToken ct) => db.KnowledgeBaseDocuments.AsNoTracking().SingleOrDefaultAsync(x => x.ProjectId == projectId && x.KnowledgeBaseId == kbId && x.Id == id && x.DeletedAt == null, ct);
    public async Task<(KnowledgeBaseDocument Document, KnowledgeBaseDocumentRevision Revision, KnowledgeBaseIndexingJob Job)?> CreateDocumentAsync(Guid projectId, Guid kbId, string title, string? externalId, string content, JsonElement metadata, CancellationToken ct)
    { if (!await db.KnowledgeBases.AnyAsync(x => x.ProjectId == projectId && x.Id == kbId && x.DeletedAt == null, ct)) return null; ValidateContent(content); var metadataJson = ValidateMetadata(metadata); var now = clock.GetUtcNow(); await using var transaction = await db.Database.BeginTransactionAsync(ct); var document = new KnowledgeBaseDocument(Guid.NewGuid(), projectId, kbId, title, externalId, metadataJson, now); var revision = new KnowledgeBaseDocumentRevision(Guid.NewGuid(), projectId, kbId, document.Id, 1, content, Sha(content), now); var job = new KnowledgeBaseIndexingJob(Guid.NewGuid(), projectId, kbId, document.Id, revision.Id, now); db.AddRange(document, revision, job, new KnowledgeBaseOutboxMessage(Guid.NewGuid(), job.Id, projectId, JsonSerializer.Serialize(new { jobId = job.Id, projectId, knowledgeBaseId = kbId, documentId = document.Id, revisionId = revision.Id, schemaVersion = 1 }), now)); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return (document, revision, job); }
    public async Task<(KnowledgeBaseDocument Document, KnowledgeBaseDocumentRevision Revision, KnowledgeBaseIndexingJob Job)?> ReplaceAsync(Guid projectId, Guid kbId, Guid documentId, string content, CancellationToken ct)
    { ValidateContent(content); var document = await db.KnowledgeBaseDocuments.SingleOrDefaultAsync(x => x.ProjectId == projectId && x.KnowledgeBaseId == kbId && x.Id == documentId && x.DeletedAt == null, ct); if (document is null) return null; var now = clock.GetUtcNow(); await using var tx = await db.Database.BeginTransactionAsync(ct); foreach (var old in await db.KnowledgeBaseIndexingJobs.Where(x => x.DocumentId == documentId && x.Status == IndexingJobStatus.Pending).ToListAsync(ct)) old.Supersede(); var number = (await db.KnowledgeBaseDocumentRevisions.Where(x => x.DocumentId == documentId).MaxAsync(x => (int?)x.RevisionNumber, ct) ?? 0) + 1; var revision = new KnowledgeBaseDocumentRevision(Guid.NewGuid(), projectId, kbId, documentId, number, content, Sha(content), now); var job = new KnowledgeBaseIndexingJob(Guid.NewGuid(), projectId, kbId, documentId, revision.Id, now); document.MarkPending(now); db.AddRange(revision, job, new KnowledgeBaseOutboxMessage(Guid.NewGuid(), job.Id, projectId, JsonSerializer.Serialize(new { jobId = job.Id, projectId, knowledgeBaseId = kbId, documentId, revisionId = revision.Id, schemaVersion = 1 }), now)); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return (document, revision, job); }
    public async Task<string?> ActiveContentAsync(Guid projectId, Guid kbId, Guid documentId, CancellationToken ct)
    {
        var doc = await db.KnowledgeBaseDocuments.AsNoTracking().SingleOrDefaultAsync(x => x.ProjectId == projectId && x.KnowledgeBaseId == kbId && x.Id == documentId && x.DeletedAt == null, ct);
        if (doc is null) return null;

        // The latest revision is the editable source of truth while it is waiting
        // to be indexed. ActiveRevisionId remains reserved for retrieval, so an
        // operator can immediately reopen and edit a newly-created document.
        return await db.KnowledgeBaseDocumentRevisions
            .Where(x => x.DocumentId == documentId)
            .OrderByDescending(x => x.RevisionNumber)
            .Select(x => x.Content)
            .FirstOrDefaultAsync(ct);
    }
    public async Task<bool> DeleteDocumentAsync(Guid projectId, Guid kbId, Guid id, CancellationToken ct) {
        var doc = await db.KnowledgeBaseDocuments.SingleOrDefaultAsync(x => x.ProjectId == projectId && x.KnowledgeBaseId == kbId && x.Id == id && x.DeletedAt == null, ct); if (doc is null) return false; doc.Delete(clock.GetUtcNow()); await db.SaveChangesAsync(ct); return true;
    }
    public async Task<KnowledgeBaseDocument?> UpdateDocumentAsync(Guid projectId, Guid kbId, Guid id, string title, JsonElement metadata, CancellationToken ct) { var doc = await db.KnowledgeBaseDocuments.SingleOrDefaultAsync(x => x.ProjectId == projectId && x.KnowledgeBaseId == kbId && x.Id == id && x.DeletedAt == null, ct); if (doc is null) return null; doc.UpdateMetadata(title, ValidateMetadata(metadata), clock.GetUtcNow()); await db.SaveChangesAsync(ct); return doc; }
    public async Task<IReadOnlyList<object>> SearchAsync(Guid projectId, IReadOnlyCollection<Guid> knowledgeBaseIds, string query, int limit, CancellationToken ct)
    {
        if (knowledgeBaseIds.Count is < 1 or > 20 || knowledgeBaseIds.Distinct().Count() != knowledgeBaseIds.Count) throw new ArgumentException("knowledgeBaseIds must contain 1–20 unique IDs.");
        if (string.IsNullOrWhiteSpace(query) || query.Length > 2000) throw new ArgumentException("query must contain at most 2,000 characters.");
        var terms = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var rows = await (from chunk in db.KnowledgeBaseDocumentChunks.AsNoTracking()
                          join document in db.KnowledgeBaseDocuments.AsNoTracking() on chunk.DocumentId equals document.Id
                          join revision in db.KnowledgeBaseDocumentRevisions.AsNoTracking() on chunk.DocumentRevisionId equals revision.Id
                          where chunk.ProjectId == projectId && knowledgeBaseIds.Contains(chunk.KnowledgeBaseId) && document.DeletedAt == null && document.ActiveRevisionId == revision.Id && revision.Status == DocumentIndexingStatus.Ready
                          select new { chunk, document }).ToArrayAsync(ct);
        return rows.Select(x => new { x.chunk.KnowledgeBaseId, x.chunk.DocumentId, documentTitle = x.document.Title, revisionId = x.chunk.DocumentRevisionId, x.chunk.ChunkIndex, x.chunk.Text, similarity = terms.Length == 0 ? 0d : (double)terms.Count(t => x.chunk.Text.Contains(t, StringComparison.OrdinalIgnoreCase)) / terms.Length, metadata = JsonSerializer.Deserialize<JsonElement>(x.document.Metadata) }).OrderByDescending(x => x.similarity).ThenBy(x => x.DocumentId).ThenBy(x => x.ChunkIndex).Take(Math.Clamp(limit, 1, 50)).Cast<object>().ToArray();
    }
    public static string Sha(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static void ValidateContent(string content) { if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("content is required."); if (Encoding.UTF8.GetByteCount(content) > 10 * 1024 * 1024) throw new ArgumentException("content must be 10 MiB or smaller."); }
    private static string ValidateMetadata(JsonElement metadata) { if (metadata.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return "{}"; if (metadata.ValueKind != JsonValueKind.Object) throw new ArgumentException("metadata must be a JSON object."); var text = metadata.GetRawText(); if (Encoding.UTF8.GetByteCount(text) > 32 * 1024) throw new ArgumentException("metadata must be 32 KiB or smaller."); if (metadata.EnumerateObject().Any(x => x.Name.StartsWith('_'))) throw new ArgumentException("metadata keys beginning with '_' are reserved."); return text; }
}
