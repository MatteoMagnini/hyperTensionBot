namespace HyperTensionBot.Server.Bot.Extensions {
    public class ExceptionExtensions {

        // exception to warn that there is no data for the plot
        public class InsufficientData : Exception {
            public InsufficientData() : base (
                "Data for plot are less than 2"){ }
        }

        // exception for data insert
        public class ImpossibleSystolic : Exception {
            public ImpossibleSystolic() : base(
                "upper or lower limit out of range") { }
        }

        public class ImpossibleDiastolic : Exception {
            public ImpossibleDiastolic() : base(
                "upper or lower limit out of range") { }
        }

        public class ImpossibleHeartRate : Exception {
            public ImpossibleHeartRate() : base(
                "upper or lower limit out of range") { }
        }
    }
}
