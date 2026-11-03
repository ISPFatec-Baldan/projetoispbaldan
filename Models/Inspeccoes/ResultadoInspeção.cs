using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Inspeccoes
{
    [FirestoreData]
    public class ResultadoInspecao
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string RespostaColetada { get; set; }

        [FirestoreProperty]
        public string Observacoes { get; set; }

        [FirestoreProperty]
        public string UrlFotoEvidencia { get; set; }

        [FirestoreProperty]
        public string IdInspecao { get; set; }

        [FirestoreProperty]
        public string IdItemChecklist { get; set; }
    }
}