# Phase 1 Pre-Deployment Checklist

**Date**: October 5, 2025  
**Reviewer**: _________________  
**Date Reviewed**: _________________

---

## ✅ Files Created/Modified

### New Files Created
- [ ] `backend/Dockerfile.ollama` - Ollama container configuration
- [ ] `backend/Models/EmbeddingGenerationJob.cs` - Foundatio job model
- [ ] `backend/Services/EmbeddingGenerationBackgroundService.cs` - Background processor
- [ ] `backend/Services/AzureOpenAIEmbeddingService.cs` - Azure implementation
- [ ] `backend/Controllers/SemanticSearchController.cs` - API endpoints
- [ ] `backend/Migrations/Phase1_01_InstallPgVector.sql` - pgvector setup
- [ ] `backend/Migrations/Phase1_02_AddEmbeddingColumns.sql` - Embedding columns
- [ ] `backend/setup-phase1.sh` - Automated setup script
- [ ] `backend/Phase1-README.md` - Usage documentation
- [ ] `backend/Phase1-IMPLEMENTATION-SUMMARY.md` - Implementation summary
- [ ] `backend/Phase1-CHECKLIST.md` - This checklist

### Files Modified
- [ ] `backend/docker-compose.yml` - Added Ollama service
- [ ] `backend/appsettings.Development.json` - Added embedding config
- [ ] `backend/Program.cs` - Registered services
- [ ] `backend/Services/IEmbeddingService.cs` - Added IsAvailableAsync
- [ ] `backend/Services/OllamaEmbeddingService.cs` - Added health check
- [ ] `backend/Services/ExcelImportService.cs` - Added embedding job queuing

---

## 🔍 Code Review

### Architecture
- [ ] Embedding service follows interface abstraction
- [ ] Can switch between Ollama and Azure OpenAI via config
- [ ] Foundatio queue properly configured
- [ ] Background service registered as hosted service
- [ ] No hardcoded values (all from config/environment)

### Error Handling
- [ ] Try-catch blocks in all async operations
- [ ] Logging at appropriate levels
- [ ] Graceful degradation if embedding service unavailable
- [ ] Retry logic with exponential backoff

### Performance
- [ ] HNSW indexes created for vector columns
- [ ] Background processing doesn't block imports
- [ ] Connection pooling configured
- [ ] Async/await used consistently

### Security
- [ ] API keys in configuration (not hardcoded)
- [ ] Environment variables supported
- [ ] No sensitive data in logs
- [ ] SQL injection prevented (parameterized queries)

---

## 🧪 Testing Checklist

### Unit Tests (Future - Not Required for Phase 1)
- [ ] EmbeddingGenerationBackgroundService tests
- [ ] OllamaEmbeddingService tests
- [ ] AzureOpenAIEmbeddingService tests
- [ ] SemanticSearchService tests
- [ ] SemanticSearchController tests

### Integration Tests

#### 1. Service Startup
```bash
docker compose up -d --build
```
- [ ] All containers start successfully
- [ ] No errors in logs
- [ ] API responds to health check
- [ ] Ollama service responds to health check

#### 2. Database Setup
```bash
./setup-phase1.sh
```
- [ ] pgvector extension installed
- [ ] Embedding columns created
- [ ] HNSW indexes created
- [ ] No migration errors

#### 3. Ollama Model
```bash
docker compose exec ollama ollama list
```
- [ ] nomic-embed-text model downloaded
- [ ] Model can generate embeddings

#### 4. API Health
```bash
curl http://localhost:8080/api/semanticsearch/health
```
- [ ] Returns 200 OK
- [ ] Shows "available": true
- [ ] Shows correct model name
- [ ] Shows correct dimension (768)

#### 5. Excel Import
```bash
# Upload test Excel file
curl -X POST http://localhost:8080/api/candidates/import -F "file=@test.xlsx"
```
- [ ] Import completes successfully
- [ ] Embedding jobs queued (check logs)
- [ ] Background service processes jobs
- [ ] Embeddings stored in database

#### 6. Semantic Search
```bash
curl -X POST http://localhost:8080/api/semanticsearch/search \
  -H "Content-Type: application/json" \
  -d '{"query": "React developer", "page": 1, "pageSize": 10}'
```
- [ ] Returns 200 OK
- [ ] Results have similarity scores
- [ ] Results ranked by relevance
- [ ] Response time < 500ms

#### 7. Hybrid Search
```bash
curl -X POST http://localhost:8080/api/semanticsearch/hybrid \
  -H "Content-Type: application/json" \
  -d '{"query": "Python engineer", "semanticWeight": 0.7, "keywordWeight": 0.3}'
```
- [ ] Returns 200 OK
- [ ] Combines semantic + keyword results
- [ ] Response time < 500ms

---

## 📊 Database Verification

### Check pgvector Installation
```sql
SELECT extname, extversion FROM pg_extension WHERE extname = 'vector';
```
- [ ] Extension installed
- [ ] Version displayed

### Check Embedding Columns
```sql
\d candidates
```
- [ ] `profile_embedding vector(768)` exists
- [ ] `embedding_generated_at timestamp` exists
- [ ] `embedding_model varchar(100)` exists

### Check Indexes
```sql
SELECT indexname FROM pg_indexes 
WHERE tablename = 'candidates' AND indexname LIKE '%embedding%';
```
- [ ] `idx_candidates_profile_embedding` exists
- [ ] `idx_candidates_embedding_status` exists

### Check Generated Embeddings
```sql
SELECT COUNT(*) FROM candidates WHERE profile_embedding IS NOT NULL;
```
- [ ] Count matches imported candidates
- [ ] Embeddings are not null

---

## 📝 Documentation Review

- [ ] Phase1-README.md is complete
- [ ] Setup instructions are clear
- [ ] API examples are correct
- [ ] Troubleshooting section is helpful
- [ ] Configuration examples are accurate

---

## 🔄 Configuration Verification

### appsettings.Development.json
- [ ] Embedding section exists
- [ ] Provider set to "Ollama"
- [ ] Ollama endpoint correct
- [ ] Ollama model name correct
- [ ] Dimension set to 768

### docker-compose.yml
- [ ] Ollama service defined
- [ ] Health check configured
- [ ] Volume for model storage
- [ ] Network configuration correct
- [ ] Environment variables passed to API

### Program.cs
- [ ] Foundatio queue registered
- [ ] Embedding service registered based on provider
- [ ] Background service registered
- [ ] HTTP client factory added
- [ ] SemanticSearchService registered

---

## 🚀 Deployment Readiness

### Prerequisites
- [ ] Docker installed
- [ ] Docker Compose installed
- [ ] PostgreSQL container running
- [ ] Port 11434 available (Ollama)
- [ ] Port 8080 available (API)

### Deployment Steps
- [ ] Run `./setup-phase1.sh` successfully
- [ ] All services healthy
- [ ] No errors in logs
- [ ] API endpoints accessible
- [ ] Embedding generation working

### Post-Deployment
- [ ] Import test data
- [ ] Verify embeddings generated
- [ ] Test search endpoints
- [ ] Monitor performance
- [ ] Check resource usage

---

## 🎯 Success Criteria

All must be checked before deployment:

- [ ] All files created/modified as listed
- [ ] No compilation errors
- [ ] All services start successfully
- [ ] Database migrations completed
- [ ] Ollama model downloaded
- [ ] API health checks pass
- [ ] Excel import triggers embedding jobs
- [ ] Background service processes jobs
- [ ] Embeddings stored in database
- [ ] Semantic search returns results
- [ ] Hybrid search returns results
- [ ] Documentation complete
- [ ] Setup script works
- [ ] Performance acceptable (< 500ms searches)

---

## 🐛 Known Issues / Limitations

Document any known issues:

1. _______________________________________________
2. _______________________________________________
3. _______________________________________________

---

## 📋 Final Sign-Off

### Developer Sign-Off
- **Name**: _________________
- **Date**: _________________
- **Comments**: _________________

### Reviewer Sign-Off
- **Name**: _________________
- **Date**: _________________
- **Comments**: _________________

---

## 🎉 Deployment Approval

- [ ] All checklist items completed
- [ ] All tests passing
- [ ] Documentation approved
- [ ] Ready for deployment

**Approved By**: _________________  
**Date**: _________________  
**Signature**: _________________

---

**Status**: 
- [ ] Ready for Deployment
- [ ] Needs Revision
- [ ] Blocked (specify reason): _________________

**Next Steps**:
1. _________________
2. _________________
3. _________________
