using Microsoft.ML.Data;

namespace HyperTensionBot.Server.ModelML {
    public class ModelOutput {
        [ColumnName(@"Sentence")]
        public float[] Sentence { get; set; }

        [ColumnName(@"Label")]
        public uint Label { get; set; }

        [ColumnName(@"Features")]
        public float[] Features { get; set; }

        [ColumnName(@"PredictedLabel")]
        public string PredictedLabel { get; set; }

        [ColumnName(@"Score")]
        public float[] Score { get; set; }
    }
}
