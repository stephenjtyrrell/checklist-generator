# Rate Limiting Solutions for OpenRouter.ai

## Problem
The free tier of OpenRouter.ai models (especially `meta-llama/llama-3.2-3b-instruct:free`) has strict rate limits:
- **1 request per minute** during high demand periods
- Rate limit error: `Rate limit exceeded: limit_rpm/meta-llama/llama-3.2-3b-instruct/...`

## Solutions Implemented

### 1. **Multiple Fallback Models**
Added an array of fallback models that the system tries when the primary model is rate limited:

```csharp
private readonly string[] _fallbackModels = new[]
{
    "gryphe/mythomist-7b:free",
    "nousresearch/nous-capybara-7b:free", 
    "microsoft/dialoGPT-medium",
    "huggingface:microsoft/DialoGPT-medium"
};
```

### 2. **Smart Rate Limit Detection**
The service now detects rate limit errors and automatically tries the next available model:

```csharp
if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || 
    errorContent.Contains("rate limit") || 
    errorContent.Contains("limit_rpm"))
{
    _logger.LogInformation("Rate limit encountered, waiting 5 seconds before trying next fallback model");
    await Task.Delay(5000);
    continue;
}
```

### 3. **Configurable Primary Model**
Users can now specify their preferred model via configuration:

```json
{
  "OpenRouterApiKey": "your_key_here",
  "OpenRouterModel": "meta-llama/llama-3.2-3b-instruct:free"
}
```

### 4. **Graceful Error Messages**
When all models are rate limited, the service returns helpful user-facing messages:

```csharp
new ChecklistItem
{
    Id = "rate_limit_error",
    Text = "AI service temporarily unavailable due to high demand",
    Description = "The free AI models are experiencing high demand. Please try again in a few minutes, or consider upgrading to a paid OpenRouter plan for higher rate limits."
}
```

### 5. **Automatic Delays**
Added 5-second delays between rate-limited attempts to avoid overwhelming the API.

## How It Works

1. **Primary Attempt**: Tries the configured default model
2. **Rate Limit Detection**: Detects if rate limited
3. **Fallback Sequence**: Tries each fallback model in order
4. **Delay Strategy**: Waits 5 seconds between rate-limited attempts
5. **Graceful Failure**: Returns helpful error message if all models fail

## User Experience Improvements

### Before (Rate Limit Error)
```
Error: OpenRouter API error: 429 - Rate limit exceeded
```

### After (Graceful Handling)
```
AI service temporarily unavailable due to high demand. 
Please try again in a few minutes, or consider upgrading 
to a paid OpenRouter plan for higher rate limits.
```

## Configuration Options

### Basic Setup (Free Models)
```bash
dotnet user-secrets set "OpenRouterApiKey" "your_key_here"
```

### Advanced Setup (Custom Model)
```bash
dotnet user-secrets set "OpenRouterApiKey" "your_key_here"
dotnet user-secrets set "OpenRouterModel" "anthropic/claude-3-haiku:beta"
```

## Alternative Free Models

If Llama 3.2 is consistently rate limited, users can configure these alternatives:

1. **Mythomist 7B**: `gryphe/mythomist-7b:free`
2. **Nous Capybara 7B**: `nousresearch/nous-capybara-7b:free`
3. **Microsoft DialoGPT**: `microsoft/dialoGPT-medium`

## Paid Options for Heavy Usage

For production or heavy usage, consider OpenRouter paid models:
- **GPT-3.5 Turbo**: `openai/gpt-3.5-turbo` (~$0.002/1K tokens)
- **Claude 3 Haiku**: `anthropic/claude-3-haiku:beta` (~$0.00025/1K tokens)
- **Llama 3.1 8B**: `meta-llama/llama-3.1-8b-instruct` (~$0.0001/1K tokens)

## Monitoring and Logging

The enhanced service provides detailed logging:
- Model selection attempts
- Rate limit detection
- Fallback model usage
- Success/failure rates

Check logs for messages like:
```
Rate limit encountered with meta-llama/llama-3.2-3b-instruct:free, waiting 5 seconds before trying next fallback model
Attempting API call with model: gryphe/mythomist-7b:free
Received successful response from OpenRouter API using model gryphe/mythomist-7b:free
```

## Testing the Solution

To test rate limit handling:
1. Make multiple rapid requests
2. Observe automatic fallback behavior in logs
3. Verify graceful error messages in UI
4. Confirm application remains functional

This implementation ensures high availability and a better user experience even during peak demand periods for free AI models.
