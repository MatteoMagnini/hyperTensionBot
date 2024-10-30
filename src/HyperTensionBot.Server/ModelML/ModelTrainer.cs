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
using Microsoft.ML.Trainers;

namespace HyperTensionBot.Server.ModelML {

    // Responsible of training ML model
    public class ModelTrainer {
        private readonly MLContext mlContext;

        public ModelTrainer(MLContext ml) {
            mlContext = ml;
        }

        public void Train(string pathFile, string pathModel) {

            // Loading data training
            ControlFile(pathFile);

            IDataView dataView = mlContext.Data.LoadFromTextFile<ModelInput>(
                pathFile,
                separatorChar: '\t',
                hasHeader: true
            );

            // create pipeline
            var pipeline = mlContext.Transforms.Text.FeaturizeText(inputColumnName: @"Sentence", outputColumnName: @"Sentence")
                                    .Append(mlContext.Transforms.Concatenate(@"Features", new[] { @"Sentence" }))
                                    .Append(mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: @"Label", inputColumnName: @"Label"))
                                    .Append(mlContext.Transforms.NormalizeMinMax(@"Features", @"Features"))
                                    .Append(mlContext.MulticlassClassification.Trainers.OneVersusAll(binaryEstimator: mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(new LbfgsLogisticRegressionBinaryTrainer.Options() { L1Regularization = 0.03125F, L2Regularization = 0.06764677F, LabelColumnName = @"Label", FeatureColumnName = @"Features" }), labelColumnName: @"Label"))
                                    .Append(mlContext.Transforms.Conversion.MapKeyToValue(outputColumnName: @"PredictedLabel", inputColumnName: @"PredictedLabel"));

            // training
            var model = pipeline.Fit(dataView);

            // save model
            try {
                if (!Directory.Exists(pathModel))
                    Directory.CreateDirectory(pathModel);
                else
                    File.Delete(Path.Combine(pathModel, "model.zip"));
            }
            catch (ArgumentNullException) { }
            finally { mlContext.Model.Save(model, dataView.Schema, Path.Combine(pathModel, "model.zip")); }
        }

        private void ControlFile(string pathFile) {
            using (var sr = new StreamReader(pathFile)) {
                string? line;
                while ((line = sr.ReadLine()) != null) {
                    string[] columns = line.Split('\t');
                    if (columns.Length < 2 || string.IsNullOrWhiteSpace(columns[1])) {
                        throw new Exception("training data is not complete");
                    }
                }
            }

        }
    }
}
