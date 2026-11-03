using Google.Cloud.Firestore;

namespace ProjetoISP.Baldan.Models.PlanosAcao
{
    [FirestoreData]
    public class HistoricoPrazo5W2H
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public Timestamp PrazoAnterior { get; set; }

        [FirestoreProperty]
        public Timestamp NovoPrazo { get; set; }

        [FirestoreProperty]
        public string MotivoAlteracao { get; set; }

        [FirestoreProperty]
        public Timestamp DataAlteracao { get; set; }

        [FirestoreProperty]
        public string IdPlanosAcao { get; set; }
    }
}