#!/bin/bash

# Azure OpenAI Connectivity Test Script
# Tests connection and embedding generation before Phase 1 implementation

echo "=== Azure OpenAI Connectivity Test ==="
echo ""

# Load environment variables
if [ -f .env ]; then
    export $(cat .env | grep -v '^#' | xargs)
    echo "✅ Loaded .env file"
else
    echo "❌ .env file not found"
    exit 1
fi

# Test 1: Connection Test
echo ""
echo "1. Testing Azure OpenAI Connection..."
curl -s -X GET "${AZURE_OPENAI_ENDPOINT}/openai/deployments?api-version=2023-05-15" \
    -H "api-key: ${AZURE_OPENAI_API_KEY}" \
    | jq -r 'if .error then "❌ Error: " + .error.message else "✅ Connection successful!" end'

# Test 2: Embedding Generation
echo ""
echo "2. Testing Embedding Generation..."
RESPONSE=$(curl -s -X POST \
    "${AZURE_OPENAI_ENDPOINT}/openai/deployments/${AZURE_OPENAI_DEPLOYMENT}/embeddings?api-version=2023-05-15" \
    -H "Content-Type: application/json" \
    -H "api-key: ${AZURE_OPENAI_API_KEY}" \
    -d '{
        "input": "Senior Software Engineer with 10 years experience in C#, .NET, React, and Azure"
    }')

if echo "$RESPONSE" | jq -e '.data[0].embedding' > /dev/null 2>&1; then
    DIMENSIONS=$(echo "$RESPONSE" | jq '.data[0].embedding | length')
    FIRST_VALUES=$(echo "$RESPONSE" | jq '.data[0].embedding[0:5]')
    echo "✅ Embedding generated successfully!"
    echo "   Dimensions: $DIMENSIONS"
    echo "   First 5 values: $FIRST_VALUES"
else
    echo "❌ Embedding generation failed"
    echo "$RESPONSE" | jq '.'
fi

# Test 3: Batch Embeddings
echo ""
echo "3. Testing Batch Embedding Generation..."
START_TIME=$(date +%s%3N)
BATCH_RESPONSE=$(curl -s -X POST \
    "${AZURE_OPENAI_ENDPOINT}/openai/deployments/${AZURE_OPENAI_DEPLOYMENT}/embeddings?api-version=2023-05-15" \
    -H "Content-Type: application/json" \
    -H "api-key: ${AZURE_OPENAI_API_KEY}" \
    -d '{
        "input": [
            "Python developer with Django and Flask experience",
            "Java developer skilled in Spring Boot and microservices",
            "DevOps engineer with Kubernetes and AWS expertise"
        ]
    }')
END_TIME=$(date +%s%3N)
ELAPSED=$((END_TIME - START_TIME))

if echo "$BATCH_RESPONSE" | jq -e '.data' > /dev/null 2>&1; then
    COUNT=$(echo "$BATCH_RESPONSE" | jq '.data | length')
    AVG=$((ELAPSED / COUNT))
    echo "✅ Batch embeddings generated successfully!"
    echo "   Count: $COUNT embeddings"
    echo "   Time: ${ELAPSED}ms"
    echo "   Avg per embedding: ${AVG}ms"
else
    echo "❌ Batch embedding generation failed"
    echo "$BATCH_RESPONSE" | jq '.'
fi

echo ""
echo "=== Test Complete ==="
