using System.Text.Json;

namespace KnowledgeBase.Service.Domain;

public enum KnowledgeBaseStatus { Active, Deleting, Deleted }
public enum DocumentIndexingStatus { Pending, Indexing, Ready, Failed, Deleted }
public enum IndexingJobStatus { Pending, Processing, Ready, Failed, Superseded, Deleted }

public sealed class KnowledgeBaseRecord
{
    private KnowledgeBaseRecord() { }
    public const string EmbeddingModel = "BAAI/bge-small-en-v1.5";
    public const int EmbeddingDimensions = 384;
    public const int ChunkSizeTokens = 384;
    public const int ChunkOverlapTokens = 64;
    public KnowledgeBaseRecord(Guid id, Guid projectId, string name, string? description, DateTimeOffset now)
    { Id = id; ProjectId = projectId; Name = Validate(name, 200, "Knowledge base name"); Description = ValidateOptional(description, 4000); CreatedAt = UpdatedAt = now; }
    public Guid Id { get; } public Guid ProjectId { get; } public string Name { get; private set; } = null!; public string? Description { get; private set; }
    public string EmbeddingModelId { get; private set; } = EmbeddingModel; public int ChunkSizeTokensValue { get; private set; } = ChunkSizeTokens; public int ChunkOverlapTokensValue { get; private set; } = ChunkOverlapTokens;
    public KnowledgeBaseStatus Status { get; private set; } = KnowledgeBaseStatus.Active; public DateTimeOffset CreatedAt { get; } public DateTimeOffset UpdatedAt { get; private set; } public DateTimeOffset? DeletedAt { get; private set; }
    public uint RowVersion { get; private set; }
    public void Update(string name, string? description, DateTimeOffset now) { Name = Validate(name, 200, "Knowledge base name"); Description = ValidateOptional(description, 4000); UpdatedAt = now; }
    public void Delete(DateTimeOffset now) { Status = KnowledgeBaseStatus.Deleted; DeletedAt = UpdatedAt = now; }
    internal static string Validate(string? value, int max, string label) { var text = value?.Trim(); if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException($"{label} is required."); if (text.Length > max) throw new ArgumentException($"{label} must be {max} characters or fewer."); return text; }
    internal static string? ValidateOptional(string? value, int max) { if (value is null) return null; if (value.Length > max) throw new ArgumentException($"Value must be {max} characters or fewer."); return value; }
}

public sealed class KnowledgeBaseDocument
{
    private KnowledgeBaseDocument() { }
    public KnowledgeBaseDocument(Guid id, Guid projectId, Guid knowledgeBaseId, string title, string? externalId, string metadata, DateTimeOffset now)
    { Id=id; ProjectId=projectId; KnowledgeBaseId=knowledgeBaseId; Title=KnowledgeBaseRecord.Validate(title,512,"Document title"); ExternalId=externalId; Metadata=metadata; CreatedAt=UpdatedAt=now; }
    public Guid Id { get; } public Guid ProjectId { get; } public Guid KnowledgeBaseId { get; } public string? ExternalId { get; private set; } public string Title { get; private set; } = null!; public string Metadata { get; private set; } = null!;
    public Guid? ActiveRevisionId { get; private set; } public DocumentIndexingStatus IndexingStatus { get; private set; } = DocumentIndexingStatus.Pending; public DateTimeOffset? LastIndexedAt { get; private set; } public string? LastErrorCode { get; private set; } public string? LastErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; } public DateTimeOffset UpdatedAt { get; private set; } public DateTimeOffset? DeletedAt { get; private set; } public uint RowVersion { get; private set; }
    public void UpdateMetadata(string title, string metadata, DateTimeOffset now) { Title=KnowledgeBaseRecord.Validate(title,512,"Document title"); Metadata=metadata; UpdatedAt=now; }
    public void MarkPending(DateTimeOffset now) { IndexingStatus=DocumentIndexingStatus.Pending; LastErrorCode=LastErrorMessage=null; UpdatedAt=now; }
    public void MarkReady(Guid revisionId, DateTimeOffset now) { ActiveRevisionId=revisionId; IndexingStatus=DocumentIndexingStatus.Ready; LastIndexedAt=now; LastErrorCode=LastErrorMessage=null; UpdatedAt=now; }
    public void MarkFailed(string code, string message, DateTimeOffset now) { IndexingStatus=DocumentIndexingStatus.Failed; LastErrorCode=code; LastErrorMessage=message; UpdatedAt=now; }
    public void Delete(DateTimeOffset now) { IndexingStatus=DocumentIndexingStatus.Deleted; DeletedAt=UpdatedAt=now; }
}

public sealed class KnowledgeBaseDocumentRevision
{
    private KnowledgeBaseDocumentRevision() { }
    public KnowledgeBaseDocumentRevision(Guid id, Guid projectId, Guid knowledgeBaseId, Guid documentId, int number, string content, string sha256, DateTimeOffset now)
    { Id=id; ProjectId=projectId; KnowledgeBaseId=knowledgeBaseId; DocumentId=documentId; RevisionNumber=number; Content=content; ContentSha256=sha256; CreatedAt=now; }
    public Guid Id { get; } public Guid ProjectId { get; } public Guid KnowledgeBaseId { get; } public Guid DocumentId { get; } public int RevisionNumber { get; } public string Content { get; } = null!; public string ContentSha256 { get; } = null!;
    public string EmbeddingModel { get; private set; } = KnowledgeBaseRecord.EmbeddingModel; public int ChunkSizeTokens { get; private set; } = KnowledgeBaseRecord.ChunkSizeTokens; public int ChunkOverlapTokens { get; private set; } = KnowledgeBaseRecord.ChunkOverlapTokens;
    public DocumentIndexingStatus Status { get; private set; } = DocumentIndexingStatus.Pending; public int AttemptCount { get; private set; } public DateTimeOffset CreatedAt { get; } public DateTimeOffset? IndexedAt { get; private set; }
    public void StartAttempt() { Status=DocumentIndexingStatus.Indexing; AttemptCount++; } public void Complete(DateTimeOffset now) { Status=DocumentIndexingStatus.Ready; IndexedAt=now; } public void Fail() => Status=DocumentIndexingStatus.Failed;
}

public sealed class KnowledgeBaseIndexingJob
{
    private KnowledgeBaseIndexingJob() { }
    public KnowledgeBaseIndexingJob(Guid id, Guid projectId, Guid knowledgeBaseId, Guid documentId, Guid revisionId, DateTimeOffset now) { Id=id; ProjectId=projectId; KnowledgeBaseId=knowledgeBaseId; DocumentId=documentId; RevisionId=revisionId; CreatedAt=now; }
    public Guid Id { get; } public Guid ProjectId { get; } public Guid KnowledgeBaseId { get; } public Guid DocumentId { get; } public Guid RevisionId { get; } public IndexingJobStatus Status { get; private set; } = IndexingJobStatus.Pending; public int AttemptCount { get; private set; } public DateTimeOffset CreatedAt { get; } public DateTimeOffset? LeaseUntil { get; private set; } public string? FailureCode { get; private set; } public string? FailureMessage { get; private set; }
    public bool TryClaim(DateTimeOffset now, TimeSpan lease) { if (Status is IndexingJobStatus.Ready or IndexingJobStatus.Superseded or IndexingJobStatus.Deleted || (Status == IndexingJobStatus.Processing && LeaseUntil > now)) return false; Status=IndexingJobStatus.Processing; LeaseUntil=now.Add(lease); AttemptCount++; return true; }
    public void Complete() { Status=IndexingJobStatus.Ready; LeaseUntil=null; } public void Fail(string code, string message) { Status=IndexingJobStatus.Failed; LeaseUntil=null; FailureCode=code; FailureMessage=message; } public void Supersede() { Status=IndexingJobStatus.Superseded; LeaseUntil=null; }
}

public sealed class KnowledgeBaseOutboxMessage
{
    private KnowledgeBaseOutboxMessage() { }
    public KnowledgeBaseOutboxMessage(Guid id, Guid jobId, Guid projectId, string payload, DateTimeOffset now) { Id=id; JobId=jobId; ProjectId=projectId; Payload=payload; CreatedAt=now; }
    public Guid Id { get; } public Guid JobId { get; } public Guid ProjectId { get; } public string EventType { get; private set; } = "knowledge_base.document.index.requested"; public string Payload { get; } = null!; public DateTimeOffset CreatedAt { get; } public DateTimeOffset? PublishedAt { get; private set; } public void MarkPublished(DateTimeOffset now) => PublishedAt=now;
}

public sealed class KnowledgeBaseDocumentChunk
{
    private KnowledgeBaseDocumentChunk() { }
    public KnowledgeBaseDocumentChunk(Guid id, Guid projectId, Guid knowledgeBaseId, Guid documentId, Guid revisionId, int index, int start, int end, string text, string sha256, string embedding) { Id=id; ProjectId=projectId; KnowledgeBaseId=knowledgeBaseId; DocumentId=documentId; DocumentRevisionId=revisionId; ChunkIndex=index; TokenStart=start; TokenEnd=end; Text=text; ContentSha256=sha256; Embedding=embedding; }
    public Guid Id { get; } public Guid ProjectId { get; } public Guid KnowledgeBaseId { get; } public Guid DocumentId { get; } public Guid DocumentRevisionId { get; } public int ChunkIndex { get; } public int TokenStart { get; } public int TokenEnd { get; } public string Text { get; } = null!; public string ContentSha256 { get; } = null!; public string Embedding { get; } = null!;
}
