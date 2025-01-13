using System;

namespace Commons
{
    public class LogNormalSampler
    {
        private readonly Random _random;
        private readonly double _mu;
        private readonly double _sigma;

        public LogNormalSampler(double median, double sigma)
        {
            if (median <= 0)
                throw new ArgumentException("Median must be positive.", nameof(median));
            if (sigma <= 0)
                throw new ArgumentException("Sigma must be positive.", nameof(sigma));

            _mu = Math.Log(median); // Convert median to log-scale
            _sigma = sigma;
            _random = new Random();
        }

        public double Sample()
        {
            // Box-Muller transform to generate a standard normal value
            double u1 = _random.NextDouble();
            double u2 = _random.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

            // Transform to log-normal
            return Math.Exp(_mu + _sigma * z);
        }
    }

    public static class RandomExtensions
    {
        /// <summary>
        /// Generates a log-normal random sample based on the specified median and sigma.
        /// </summary>
        /// <param name="random">The Random instance.</param>
        /// <param name="median">The median of the log-normal distribution.</param>
        /// <param name="sigma">The sigma (spread) of the log-normal distribution.</param>
        /// <returns>A random sample from the log-normal distribution.</returns>
        public static float NextLogNormal(this Random random, double median, double sigma=0.3)
        {
            if (median <= 0)
                throw new ArgumentException("Median must be positive.", nameof(median));
            if (sigma <= 0)
                throw new ArgumentException("Sigma must be positive.", nameof(sigma));

            // Calculate the log-normal parameters
            double mu = Math.Log(median);

            // Generate a standard normal random value using Box-Muller transform
            double u1 = random.NextDouble();
            double u2 = random.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

            // Transform to log-normal
            return (float)Math.Exp(mu + sigma * z);
        }
    }
}
