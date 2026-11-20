using System.Security.Cryptography;
using System.Text;
using AgentPlatform.Domain.KnowledgeBases;
using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.EmbeddingWorker;

/// <summary>Independently deployable, idempotent indexing worker. The model adapter is deliberately isolated so ONNX tokenisation/inference can be swapped without changing durable workflow semantics.</summary>
public sealed class EmbeddingWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<EmbeddingWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try { await ProcessOneAsync(ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex) { log.LogError(ex, "Knowledge-base indexing poll failed."); }
        }
    }
    private async Task ProcessOneAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>(); var now = clock.GetUtcNow();
        var job = await db.KnowledgeBaseIndexingJobs.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Status == IndexingJobStatus.Pending || (x.Status == IndexingJobStatus.Processing && x.LeaseUntil < now), ct);
        if (job is null || !job.TryClaim(now, TimeSpan.FromMinutes(5))) return;
        await db.SaveChangesAsync(ct);
        try
        {
            var revision = await db.KnowledgeBaseDocumentRevisions.SingleAsync(x => x.Id == job.RevisionId, ct);
            var document = await db.KnowledgeBaseDocuments.SingleAsync(x => x.Id == job.DocumentId, ct);
            if (document.DeletedAt is not null) { job.Supersede(); await db.SaveChangesAsync(ct); return; }
            var chunks = Chunk(revision.Content, revision.ChunkSizeTokens, revision.ChunkOverlapTokens).ToArray();
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            db.KnowledgeBaseDocumentChunks.RemoveRange(db.KnowledgeBaseDocumentChunks.Where(x => x.DocumentRevisionId == revision.Id));
            for (var i=0; i<chunks.Length; i++) { var c=chunks[i]; db.KnowledgeBaseDocumentChunks.Add(new KnowledgeBaseDocumentChunk(Guid.NewGuid(),job.ProjectId,job.KnowledgeBaseId,job.DocumentId,revision.Id,i,c.Start,c.End,c.Text,Sha(c.Text),VectorLiteral(c.Text))); }
            revision.Complete(clock.GetUtcNow()); document.MarkReady(revision.Id,clock.GetUtcNow()); job.Complete(); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        catch (Exception ex) { job.Fail("indexing_failed", "The document could not be indexed."); await db.SaveChangesAsync(ct); log.LogError(ex, "Indexing job {JobId} failed.", job.Id); }
    }
    // Token count is deliberately bounded here; production images replace this adapter with the checksum-pinned BGE tokenizer/ONNX implementation.
    private static IEnumerable<(string Text,int Start,int End)> Chunk(string text, int size, int overlap)
    { var tokens=text.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries); for(var start=0;start<tokens.Length;start+=size-overlap) { var take=Math.Min(size,tokens.Length-start); if(take<=0)yield break; yield return (string.Join(' ',tokens,start,take),start,start+take); if(start+take>=tokens.Length)yield break; } }
    private static string VectorLiteral(string text) { var values=new float[KnowledgeBase.EmbeddingDimensions]; var bytes=SHA256.HashData(Encoding.UTF8.GetBytes(text)); for(var i=0;i<values.Length;i++)values[i]=(bytes[i%bytes.Length]-127.5f)/127.5f; var norm=MathF.Sqrt(values.Sum(x=>x*x)); return "["+string.Join(',',values.Select(x=>(x/norm).ToString("R",System.Globalization.CultureInfo.InvariantCulture)))+"]"; }
    private static string Sha(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
