using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaIntegration
{
    /// <summary>
    /// Proveedor de chat seleccionable (local o nube).
    /// </summary>
    public enum ChatProviderType
    {
        Ollama = 0,
        Gemini = 1
    }

    /// <summary>
    /// Cancelación esperada de una petición (reemplazo, cierre de la app o
    /// tiempo agotado). No debe mostrarse como error al usuario.
    /// </summary>
    public class ChatRequestCancelledException : Exception
    {
        public ChatRequestCancelledException(string message) : base(message) { }
    }

    /// <summary>
    /// Contrato común para proveedores de chat. El historial se conserva
    /// al cambiar de proveedor: la lista usa roles "user"/"assistant"
    /// (más "system" solo en la primera posición) y cada adaptador
    /// la convierte a su formato nativo.
    /// </summary>
    public interface IChatProvider
    {
        string ProviderId { get; }
        string DisplayName { get; }
        Task<string> SendChatAsync(IReadOnlyList<OllamaMessage> messages, CancellationToken ct);
        Task<string> TestConnectionAsync(CancellationToken ct);
    }
}
