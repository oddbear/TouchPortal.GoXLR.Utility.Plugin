namespace TouchPortal.GoXLR.Utility.Plugin.Helpers
{
    public static class ValuesHelper
    {
        public static int FromValueToPercentage(int value, int min, int max)
        {
            var range = max - min;
            var multiplier = range / 100d;

            value -= min;
            var percentage = (int)Math.Round(value / multiplier);
            return EnforceRange(percentage, 0, 100);
        }
        
        public static int FromPercentageToValue(int amountPercentage, int min, int max)
        {
            var range = max - min;
            var multipier = range / 100d;

            var volume = (int)Math.Round(amountPercentage * multipier) + min;
            return EnforceRange(volume, min, max);
        }

        public static int FromVolumePercentageToVolume(int volumePercentage)
        {
            var volume = (int)Math.Round(volumePercentage * 2.55d);
            return EnforceRange(volume, byte.MinValue, byte.MaxValue);
        }

        public static int FromVolumeToVolumePercentage(int volume)
        {
            var volumePercentage = (int)Math.Round(volume / 2.55d);
            return EnforceRange(volumePercentage, 0, 100);
        }

        private static int EnforceRange(int value, int min, int max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }
    }
}
