using MathNet.Numerics.Distributions;
class Program
{
    static double ComputeConditionalExpectation(double w, int k = 4)
    {
        var gamma_k = new Gamma(k, 1.0);
        var gamma_k1 = new Gamma(k + 1, 1.0);
        double surv_k = 1 - gamma_k.CumulativeDistribution(w);
        double surv_k1 = 1 - gamma_k1.CumulativeDistribution(w);
        return k * surv_k1 / surv_k;
    }

    static void Main(string[] args)
    {
        double h = Math.Sqrt(3);      // height
        int N = 100;          // max number of grid cells
        int k = 3;           // shape parameter for Erlang
        int M = 200;        // number of intervals

        // Create Erlang distribution (which is just Gamma with integer shape parameter)
        var erlang = new Gamma(k, 1.0);

        // Calculate endpoints for M-1 intervals of equal probability 1/M
        var endpoints = new List<double>();
        Console.WriteLine("\nInterval right endpoints:");
        for (int i = 1; i < M; i++)  // Note: going to M-1 instead of M
        {
            double p = (double)i / M;
            double endpoint = erlang.InverseCumulativeDistribution(p);
            endpoints.Add(endpoint);
            Console.WriteLine($"Interval {i}: {endpoint}");
        }

        double totalAverageDistance = 0.0;

        // Evaluate function at all but the last endpoint
        for (int i = 0; i < endpoints.Count - 1; i++)
        {
            double w = endpoints[i];
            double averageDistanceForPoint = TspUpperBound(w, h * h, N, k);
            totalAverageDistance += averageDistanceForPoint;

            Console.WriteLine($"\nPoint {i + 1}: w = {w}");
            Console.WriteLine($"Average distance for this point: {averageDistanceForPoint}");
        }

        // For the last interval, just use conditional expectation + k*h
        double w0 = endpoints[endpoints.Count - 1];
        double lastValue = ComputeConditionalExpectation(w0) + k * h;
        totalAverageDistance += lastValue;

        Console.WriteLine($"\nLast interval using conditional expectation:");
        Console.WriteLine($"E(W|W>{w0}) + k*h = {lastValue}");

        // Compute the overall average
        double overallAverageDistance = totalAverageDistance / M;
        double beta = 1.0 / (k * h) * overallAverageDistance;
        Console.WriteLine($"\nBeta: {beta}");
    }

    private static double TspUpperBound(double w, double h, int N, int k)
    {
        // Step 1: Compute grid dimensions
        var (m, n) = RectangleGrid.ComputeGridDimensions(w, h, N);
        Console.WriteLine($"Grid Dimensions: m = {m}, n = {n}");

        // Step 2: Create the grid of sub-rectangles
        var grid = RectangleGrid.CreateGrid(m, n, w, h);

        // Step 3: Generate (k+1)-tuples of rectangles
        var tuples = RectangleGrid.GenerateTuples(grid, m, n, k);

        Console.WriteLine($"Number of (k+1)-tuples generated: {tuples.Count}");


        // Step 4: Compute the average minMaxDistance over all tuples
        double totalDistance = 0.0;
        var totalDistanceLock = new object();

        Parallel.ForEach(tuples, tuple =>
        {
            double minMaxDistance = RectangleGrid.ComputeCost(tuple);
            lock (totalDistanceLock)
            {
                totalDistance += minMaxDistance;
            }
        });

        double averageDistance = totalDistance / tuples.Count;


        // Output the average value of minMaxDistances
        Console.WriteLine($"\nAverage distance over all tuples: {averageDistance}");
        return averageDistance; // Return the average distance for this sample
    }
}
