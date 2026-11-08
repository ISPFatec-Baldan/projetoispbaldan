using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.Inspecoes
{
    [FirestoreData]
    public class InspecaoISP
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public Timestamp DataHoraInicio { get; set; }

        [FirestoreProperty]
        public Timestamp? DataHoraFim { get; set; }

        [FirestoreProperty]
        public string Status { get; set; }

        [FirestoreProperty]
        public string IdModeloChecklist { get; set; }

        [FirestoreProperty]
        public string IdInspetorUsuario { get; set; }

        [FirestoreProperty]
        public string IdSetor { get; set; }
    }
}