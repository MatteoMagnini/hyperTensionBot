/*
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


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
