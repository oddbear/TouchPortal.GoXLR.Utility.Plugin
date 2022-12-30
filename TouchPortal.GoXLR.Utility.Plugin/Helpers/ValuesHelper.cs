namespace TouchPortal.GoXLR.Utility.Plugin.Helpers
{
    public static class ValuesHelper
    {
        public static int FromVolumePercentageToVolume(int volumePercentage)
        {
            var volume = (int)Math.Round(volumePercentage * 2.55d);
            return EnforceRange(volume, byte.MinValue, byte.MaxValue);
        }

        public static int FromVolumeToVolumePercentage(int volume)
            => (int)Math.Round(volume / 2.55d);

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
