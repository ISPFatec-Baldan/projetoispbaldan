using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Checklist
{
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
        public string IdModeloChecklist { get; set; } // Referência para o ModeloChecklist
    }
}