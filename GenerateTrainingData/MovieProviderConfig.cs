using System;
using System.Collections.Generic;
using System.Text;
using TacoFlix.Model.Configuration;

namespace GenerateTrainingData
{
    public class MovieProviderConfig : IMovieInfoProviderConfiguration
    {
        string IMovieInfoProviderConfiguration.MovieDbApiKey => "YourKeyHere";
    }
}
