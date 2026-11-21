using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Email;

/// <summary>
/// Fila de e-mails (padrão "outbox"). O backend grava aqui e avisa o serviço de e-mail com <c>NOTIFY email_outbox</c>;
/// o serviço de e-mail (projeto Email) monta o HTML a partir do template, envia por SMTP e registra o resultado.
/// Gravar no banco, em vez de chamar o serviço por HTTP, garante que nenhum e-mail se perde se ele estiver fora do ar.
/// </summary>
[Table("email_outbox")]
[Index(nameof(Status), nameof(NextAttemptAt))]
public class EmailOutboxModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("to_address")]
    [StringLength(256)]
    public string ToAddress { get; set; } = null!;

    [Required]
    [Column("subject")]
    [StringLength(300)]
    public string Subject { get; set; } = null!;

    /// <summary>Arquivo em <c>Email/Templates</c> (ex.: <c>reset-password.html</c>).</summary>
    [Required]
    [Column("template")]
    [StringLength(100)]
    public string Template { get; set; } = null!;

    /// <summary>Valores do template (<c>{chave}</c> → valor), em JSON. O serviço codifica em HTML ao montar.</summary>
    [Required]
    [Column("payload", TypeName = "jsonb")]
    public string Payload { get; set; } = "{}";

    [Required]
    [Column("status")]
    public EEmailStatus Status { get; set; } = EEmailStatus.Pending;

    [Required]
    [Column("attempts")]
    public int Attempts { get; set; }

    [Required]
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Quando o envio pode ser tentado (agora, ou depois de uma falha, com espera crescente).</summary>
    [Required]
    [Column("next_attempt_at")]
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("sent_at")]
    public DateTimeOffset? SentAt { get; set; }

    [Column("last_error")]
    [StringLength(1000)]
    public string? LastError { get; set; }

    /// <summary>Sessão de uso do aviso de check-in/check-out (nulo nos e-mails de conta).</summary>
    [Column("fk_usage_id")]
    public long? FkUsageId { get; set; }
    [ForeignKey(nameof(FkUsageId))]
    public virtual Models.Usage.ReserveUsageModel? Usage { get; set; }

    /// <summary>
    /// Depois deste instante o e-mail não é mais enviado (status Expirado): o link do aviso já teria vencido.
    /// Nulo = sem prazo (e-mails de conta).
    /// </summary>
    [Column("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }
}
