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


using HyperTensionBot.Server.Bot.Extensions;

namespace HyperTensionBot.Server.Bot {
    // Measurement class with all fields and property necessary
    public class Measurement {
        public double? SystolicPressure { get; init; }

        public double? DiastolicPressure { get; init; }

        public double? HeartRate { get; init; }

        public DateTime Date { get; init; }

        public Measurement(double? systolicPressure, double? diastolicPressure, double? heartRate, DateTime date) {
            Check(systolicPressure, diastolicPressure, heartRate);

            SystolicPressure = systolicPressure;
            DiastolicPressure = diastolicPressure;
            HeartRate = heartRate;
            Date = date;
        }
        // Check if the new Measurement is in possible range of values
        private void Check(double? systolicPressure, double? diastolicPressure, double? heartRate) {
            if (systolicPressure != null &&
                (systolicPressure < 50 || systolicPressure > 250))
                throw new ExceptionExtensions.ImpossibleSystolic();

            if (diastolicPressure != null &&
                (diastolicPressure < 40 || diastolicPressure > 150))
                throw new ExceptionExtensions.ImpossibleDiastolic();

            if (heartRate != null &&
                (heartRate < 30 || heartRate > 200))
                throw new ExceptionExtensions.ImpossibleSystolic();
        }
    }
}
