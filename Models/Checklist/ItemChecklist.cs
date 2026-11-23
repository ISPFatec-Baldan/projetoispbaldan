using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Checklist
{
    /// <summary>
    /// Pergunta do banco de perguntas do ISP. Uma pergunta pode estar em vários checklists
    /// (veja ChecklistPergunta) e pertence a um tópico.
    /// </summary>
    [FirestoreData]
    public class ItemChecklist
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string DescricaoPergunta { get; set; }

        [FirestoreProperty]
        public string TipoResposta { get; set; }

        [FirestoreProperty]
        public Timestamp DataCriacao { get; set; }

        [FirestoreProperty]
        public string? IdTopico { get; set; }

        // Pergunta desativada sai dos checklists, mas continua no histórico das inspeções.
        // (Documentos antigos, sem este campo, são lidos como ativos.)
        [FirestoreProperty]
        public bool Ativo { get; set; } = true;

        // Sobe 1 a cada alteração do texto. As respostas guardam a versão que responderam.
        [FirestoreProperty]
        public int Versao { get; set; } = 1;
    }
}
