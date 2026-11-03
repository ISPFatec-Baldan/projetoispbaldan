using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Checklist
{
    [FirestoreData]
    public class ItemChecklist
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string DescricaoPergunta { get; set; }

        [FirestoreProperty]
        public string TipoResposta { get; set; }

        [FirestoreProperty]
        public Timestamp Data { get; set; }

        [FirestoreProperty]
        public string IdChecklist { get; set; } // Referência para o ModeloChecklist
    }
}