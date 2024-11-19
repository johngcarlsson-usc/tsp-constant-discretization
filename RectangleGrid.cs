using System;
using System.Collections.Generic;
using System.Linq;
using Combinatorics.Collections;

public class RectangleGrid
{
    public class Rectangle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public Rectangle(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        // Return all four corners of the rectangle (x,y) coordinates
        public (double, double)[] GetCorners()
        {
            return new (double, double)[]
            {
                (X, Y),                       // Top-left
                (X + Width, Y),               // Top-right
                (X, Y + Height),              // Bottom-left
                (X + Width, Y + Height)       // Bottom-right
            };
        }

        public override string ToString()
        {
            return $"Rectangle(X={X}, Y={Y}, Width={Width}, Height={Height})";
        }
    }

    public static (int m, int n) ComputeGridDimensions(double w, double h, int N)
    {
        int m = Math.Max(1, (int)Math.Floor(Math.Sqrt(h * N / w)));
        int n = Math.Max(1, (int)Math.Floor(Math.Sqrt(w * N / h)));
        return (m, n);
    }

    public static List<Rectangle> CreateGrid(int m, int n, double w, double h)
    {
        List<Rectangle> grid = new List<Rectangle>();
        double cellWidth = w / n;
        double cellHeight = h / m;

        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                double x = j * cellWidth;
                double y = i * cellHeight;
                grid.Add(new Rectangle(x, y, cellWidth, cellHeight));
            }
        }

        return grid;
    }

    // Generate tuples of (k+1) rectangles from the grid
    public static List<List<Rectangle>> GenerateTuples(List<Rectangle> grid, int m, int n, int k)
    {
        var leftRectangles = grid.Where((rect, index) => index % n == 0).ToList();  // Leftmost column
        var rightRectangles = grid.Where((rect, index) => (index + 1) % n == 0).ToList(); // Rightmost column

        var middleTuples = new Variations<Rectangle>(grid, k - 1, GenerateOption.WithRepetition);

        var allTuples = new List<List<Rectangle>>();

        foreach (var left in leftRectangles)
        {
            foreach (var middle in middleTuples)
            {
                foreach (var right in rightRectangles)
                {
                    var tuple = new List<Rectangle> { left };
                    tuple.AddRange(middle);
                    tuple.Add(right);
                    allTuples.Add(tuple);
                }
            }
        }

        return allTuples;
    }

    // Compute Euclidean distance between two points (x1, y1) and (x2, y2)
    public static double EuclideanDistance((double, double) point1, (double, double) point2)
    {
        double dx = point1.Item1 - point2.Item1;
        double dy = point1.Item2 - point2.Item2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // Compute the cost (minimum of maximum distances) for a given tuple of rectangles
    public static double ComputeCost(List<Rectangle> tuple)
    {
        double bestPermutationCost = double.MaxValue; // To track the best permutation's worst cost

        // For the first rectangle, select one of the two leftmost corners (x = 0)
        var firstRectangle = tuple.First();
        var firstCornerOptions = new (double, double)[]
        {
        (firstRectangle.X, firstRectangle.Y),                        // Top-left
        (firstRectangle.X, firstRectangle.Y + firstRectangle.Height) // Bottom-left
        };

        // For the last rectangle, select one of the two rightmost corners (x + Width)
        var lastRectangle = tuple.Last();
        var lastCornerOptions = new (double, double)[]
        {
        (lastRectangle.X + lastRectangle.Width, lastRectangle.Y),                        // Top-right
        (lastRectangle.X + lastRectangle.Width, lastRectangle.Y + lastRectangle.Height)  // Bottom-right
        };

        // Generate permutations of the middle corners with repetition
        var middleRectangles = tuple.Skip(1).Take(tuple.Count - 2).ToList();  // Middle rectangles
        var allMiddleCornerSelections = middleRectangles.Select(rect => rect.GetCorners()).ToList(); // All corners for middle rectangles

        // Iterate over all permutations of the middle rectangles
        foreach (var perm in new Variations<Rectangle>(middleRectangles, middleRectangles.Count, GenerateOption.WithoutRepetition))
        {
            double worstCostForPermutation = 0.0; // Track the worst cost for the current permutation

            // Now iterate over the two options for first and last corners
            foreach (var firstCorner in firstCornerOptions)
            {
                foreach (var lastCorner in lastCornerOptions)
                {
                    // Iterate over all possible corner combinations for the middle rectangles
                    foreach (var middleCornerCombination in CartesianProduct(allMiddleCornerSelections))
                    {
                        // Construct the full selection: permute middle corners, while varying first and last
                        var permutedSelection = new List<(double, double)>
                    {
                        firstCorner  // Fixed first corner
                    };

                        permutedSelection.AddRange(middleCornerCombination);  // All combinations of middle corners
                        permutedSelection.Add(lastCorner);  // Fixed last corner

                        // Compute the total distance for this permutation
                        double totalDistance = 0.0;
                        for (int i = 0; i < permutedSelection.Count - 1; i++)
                        {
                            double distance = EuclideanDistance(permutedSelection[i], permutedSelection[i + 1]);
                            totalDistance += distance;
                        }

                        // Track the worst cost for this permutation (i.e., the maximum total distance across all corner selections)
                        worstCostForPermutation = Math.Max(worstCostForPermutation, totalDistance);
                    }
                }
            }

            // Track the best permutation (i.e., the minimum of the worst costs)
            bestPermutationCost = Math.Min(bestPermutationCost, worstCostForPermutation);
        }

        return bestPermutationCost; // Return the best (minimum worst) cost across all permutations
    }

    public static IEnumerable<List<(double, double)>> CartesianProduct(List<(double, double)[]> selections)
    {
        // A helper function to generate all combinations of middle rectangle corners (Cartesian Product)
        var result = new List<List<(double, double)>> { new List<(double, double)>() };

        foreach (var options in selections)
        {
            result = result.SelectMany(r => options.Select(o =>
            {
                var newList = new List<(double, double)>(r);
                newList.Add(o);
                return newList;
            })).ToList();
        }
        return result;
    }
}