using System.Net;

namespace CambioReal.Plaid.Http;

/// <summary>Erro devolvido pela Plaid API.</summary>
public class PlaidApiException : Exception
{
    /// <summary>Cria uma exceção sem contexto de resposta.</summary>
    public PlaidApiException()
    {
    }

    /// <summary>Cria uma exceção com mensagem.</summary>
    public PlaidApiException(string message)
        : base(message)
    {
    }

    /// <summary>Cria uma exceção com mensagem e causa.</summary>
    public PlaidApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Cria uma exceção a partir de uma resposta da API.</summary>
    public PlaidApiException(
        HttpStatusCode statusCode, string? errorType, string? errorCode, string message, string? requestId, string? responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorType = errorType;
        ErrorCode = errorCode;
        RequestId = requestId;
        ResponseBody = responseBody;
    }

    /// <summary>Status HTTP da resposta.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary><c>error_type</c> da Plaid (<c>INVALID_REQUEST</c>, <c>ITEM_ERROR</c>, …).</summary>
    public string? ErrorType { get; }

    /// <summary><c>error_code</c> da Plaid (<c>INVALID_API_KEYS</c>, <c>ITEM_NOT_FOUND</c>, …).</summary>
    public string? ErrorCode { get; }

    /// <summary><c>request_id</c> da Plaid — correlação para suporte.</summary>
    public string? RequestId { get; }

    /// <summary>Corpo bruto da resposta, para diagnóstico.</summary>
    public string? ResponseBody { get; }
}

/// <summary>Credenciais inválidas/revogadas (<c>INVALID_API_KEYS</c> e afins).</summary>
public sealed class PlaidAuthenticationException : PlaidApiException
{
    /// <inheritdoc/>
    public PlaidAuthenticationException()
    {
    }

    /// <inheritdoc/>
    public PlaidAuthenticationException(string message)
        : base(message)
    {
    }

    /// <inheritdoc/>
    public PlaidAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <inheritdoc/>
    public PlaidAuthenticationException(
        HttpStatusCode statusCode, string? errorType, string? errorCode, string message, string? requestId, string? responseBody)
        : base(statusCode, errorType, errorCode, message, requestId, responseBody)
    {
    }
}
