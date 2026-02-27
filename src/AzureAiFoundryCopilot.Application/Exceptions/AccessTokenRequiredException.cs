namespace AzureAiFoundryCopilot.Application.Exceptions;

public sealed class AccessTokenRequiredException : Exception
{
    public AccessTokenRequiredException(string message)
        : base(message)
    {
    }
}
