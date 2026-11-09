using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Checklist
{
    [FirestoreData]
    public class ModeloChecklist
    {
        [FirestoreDocumentId]
        public string? Id { get; set; }

        [FirestoreProperty]
        public string Nome { get; set; }

        [FirestoreProperty]
        public string Versao { get; set; }

        [FirestoreProperty]
        public string Status { get; set; }

        [FirestoreProperty]
        public Timestamp DataCriacao { get; set; }

        [FirestoreProperty]
        public string IdCategoria { get; set; }
    }
}