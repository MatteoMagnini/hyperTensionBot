using Microsoft.ML;

namespace HyperTensionBot.Server.ModelML {
    // model for classification a user message
    public class ClassificationModel {
        private readonly MLContext mlContext;
        private ITransformer? model;
        private ModelTrainer trainer;
        private string? pathFile;
        private string? pathModel;

        public ClassificationModel(WebApplicationBuilder builder) {
            mlContext = new MLContext();
            trainer = new ModelTrainer(mlContext);
            if (ConfigurePath(builder)) {
                if (!string.IsNullOrEmpty(pathModel) && !string.IsNullOrEmpty(pathFile)) {
                    trainer.Train(pathFile, pathModel);
                    model = mlContext.Model.Load(Path.Combine(pathModel, "model.zip"), out var modelInputSchema);
                }
            }
            else { throw new Exception("Model is not set"); }
        }

        private bool ConfigurePath(WebApplicationBuilder builder) {

            var confModel = builder.Configuration.GetSection("ClassificationModel");
            if (confModel.Exists() && !string.IsNullOrEmpty(confModel["trainingData"]) && !string.IsNullOrEmpty(confModel["model"])) {
                pathFile = confModel["trainingData"];
                pathModel = confModel["model"] ?? throw new ArgumentException("Configuration model: path model is not set");
                // delete old folder and create new
                if (Directory.Exists(pathModel)) {
                    Directory.Delete(pathModel, true);
                }
                Directory.CreateDirectory(pathModel);
                return true;
            }
            else { return false; }
        }

        // method for predict
        public Intent Predict(ModelInput input) {
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
            // update training set with new input
            var result = predictionEngine.Predict(input).PredictedLabel;
            return (Intent)Enum.Parse(typeof(Intent), result);
        }
    }
}
