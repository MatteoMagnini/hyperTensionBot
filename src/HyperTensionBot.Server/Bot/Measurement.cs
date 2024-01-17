using HyperTensionBot.Server.Bot.Extensions;

namespace HyperTensionBot.Server.Bot {
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
