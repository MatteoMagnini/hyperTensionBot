using Microsoft.ML;

namespace HyperTensionBot.Server.ModelML {
    // model for classification a user message
    public class ClassificationModel {
        private readonly MLContext mlContext;
        private readonly ITransformer? model;
        private readonly ModelTrainer trainer;
        private string? pathFile;
        private string? pathModel;

        public ClassificationModel() {
            mlContext = new MLContext();
            trainer = new ModelTrainer(mlContext);
            ConfigurePath();
            if (!string.IsNullOrEmpty(pathModel) && !string.IsNullOrEmpty(pathFile)) {
                trainer.Train(pathFile, pathModel);
                model = mlContext.Model.Load(Path.Combine(pathModel, "model.zip"), out var modelInputSchema);
            }
        }

        // Defined path to ML model and training set 
        private void ConfigurePath() {
            var sep = Path.DirectorySeparatorChar;
            pathFile = Path.Combine(Directory.GetCurrentDirectory(), "ModelML" + sep + "trainingData.tsv");
            pathModel = Path.Combine(Directory.GetCurrentDirectory(), "bin" + sep + "Debug" + sep + "net7.0" + sep + "Model") ?? throw new ArgumentException("Configuration model: path model is not set");
            // delete old folder and create new
            if (Directory.Exists(pathModel)) {
                Directory.Delete(pathModel, true);
            }
            Directory.CreateDirectory(pathModel);
        }

        // method for predict
        public Intent Predict(ModelInput input) {
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
            // update training set with new input
            var result = predictionEngine.Predict(input).PredictedLabel;

            // update training set with new input
            if (pathFile != null) {
                using (StreamWriter file = new(pathFile, true)) {

                    file.WriteLine(input.Sentence + "\t" + result);
                }
            }

            return (Intent)Enum.Parse(typeof(Intent), result!);
        }
    }
}
