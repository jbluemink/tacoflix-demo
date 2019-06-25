using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TacoFlix.Providers.TheMovieDb;

namespace GenerateTrainingData
{
    class Program
    {
        static void Main(string[] args)
        {
            var log = new ConsoleLogger();
            var movieProviderConfig = new MovieProviderConfig();
            var movieProvider = new TheMovieDbMovieInfoProvider(movieProviderConfig, log);

            var movies = movieProvider.GetPopularMovies();

            Random random = new Random();
            using (StreamWriter outputFile = new StreamWriter("c:\\ml\\movie.csv"))
            {
                outputFile.WriteLine("movieid,rating");
                foreach (var movie in movies)
                {
                    int rating = random.Next(9);
                    outputFile.WriteLine(movie.Id + "," + (rating + random.Next(2)));
                    outputFile.WriteLine(movie.Id + "," + (rating + random.Next(2)));
                    outputFile.WriteLine(movie.Id + "," + (rating + random.Next(2)));
                    outputFile.WriteLine(movie.Id + "," + (rating + random.Next(2)));
                    outputFile.WriteLine(movie.Id + "," + (rating + random.Next(2)));
                }
            }
        }
    }
}
