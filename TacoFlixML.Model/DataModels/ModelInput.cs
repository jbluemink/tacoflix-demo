//*****************************************************************************************
//*                                                                                       *
//* This is an auto-generated file by Microsoft ML.NET CLI (Command-Line Interface) tool. *
//*                                                                                       *
//*****************************************************************************************

using Microsoft.ML.Data;

namespace TacoFlixML.Model.DataModels
{
    public class ModelInput
    {
        [ColumnName("movieid"), LoadColumn(0)]
        public float Movieid { get; set; }


        [ColumnName("rating"), LoadColumn(1)]
        public float Rating { get; set; }


    }
}
