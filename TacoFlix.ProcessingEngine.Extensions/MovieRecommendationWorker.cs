using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML;
using Sitecore.Processing.Engine.Abstractions;
using Sitecore.Processing.Engine.Projection;
using Sitecore.Processing.Engine.Storage.Abstractions;
using TacoFlix.Model.Providers;
using TacoFlix.ProcessingEngine.Extensions.Providers;
using TacoFlixML.Model.DataModels;

namespace TacoFlix.ProcessingEngine.Extensions
{
    public class MovieRecommendationWorker : IDeferredWorker
    {
        public const string OptionSourceTableName = "sourceTableName";
        public const string OptionTargetTableName = "targetTableName";
        public const string OptionSchemaName = "schemaName";
        public const string OptionLimit = "limit";

        private readonly ITableStore _tableStore;
        private readonly IMovieRecommendationsProvider _movieRecommendationsProvider;
        private readonly string _sourceTableName;
        private readonly string _targetTableName;
        private readonly int _limit = 1;

        //Machine Learning model to load and use for predictions
        private const string MODEL_FILEPATH = @"MLModel.zip";

        public MovieRecommendationWorker(ITableStoreFactory tableStoreFactory, IMovieRecommendationsProvider movieRecommendationsProvider, IReadOnlyDictionary<string, string> options)
        {
            _sourceTableName = options[OptionSourceTableName];
            _targetTableName = options[OptionTargetTableName];
            _limit = int.Parse(options[OptionLimit]);

            _movieRecommendationsProvider = movieRecommendationsProvider;

            var schemaName = options[OptionSchemaName];
            _tableStore = tableStoreFactory.Create(schemaName);
        }

        public void Dispose()
        {
            _tableStore.Dispose();
        }

        public async Task RunAsync(CancellationToken token)
        {
            var sourceRows = await _tableStore.GetRowsAsync(_sourceTableName, CancellationToken.None);
            var targetRows = new List<DataRow>();
            var targetSchema = new RowSchema(
                new FieldDefinition("ContactId", FieldKind.Key, FieldDataType.Guid),
                new FieldDefinition("MovieId", FieldKind.Key, FieldDataType.Int64),
                new FieldDefinition("Title", FieldKind.Attribute, FieldDataType.String),
                new FieldDefinition("Overview", FieldKind.Attribute, FieldDataType.String),
                new FieldDefinition("PosterPath", FieldKind.Attribute, FieldDataType.String)
            );

            //ML context
            MLContext mlContext = new MLContext();
            ITransformer mlModel = mlContext.Model.Load(GetAbsolutePath(MODEL_FILEPATH), out DataViewSchema inputSchema);
            var predEngine = mlContext.Model.CreatePredictionEngine<TacoFlixML.Model.DataModels.ModelInput, ModelOutput>(mlModel);

            while (await sourceRows.MoveNext())
            {
                foreach (var row in sourceRows.Current)
                {
                    var movieId = int.Parse(row["LastMovieId"].ToString());
                    var recommendations = _movieRecommendationsProvider.GetRecommendations(movieId);

                    if (recommendations.Any())
                    {
                        for (var i = 0; i < _limit; i++)
                        {
                            if (i < recommendations.Count)
                            {
                                //ML
                                ModelInput input = new ModelInput();
                                input.Movieid = recommendations[i].Id;
                                ModelOutput predictionResult = predEngine.Predict(input);

                                var targetRow = new DataRow(targetSchema);
                                targetRow.SetGuid(0, row.GetGuid(0));
                                targetRow.SetInt64(1, recommendations[i].Id);
                                targetRow.SetString(2, recommendations[i].Title + "score="+predictionResult.Score);
                                targetRow.SetString(3, recommendations[i].Overview);
                                targetRow.SetString(4, recommendations[i].PosterPath);

                                targetRows.Add(targetRow);
                            }
                        }
                    }
                }
            }

            var tableDefinition = new TableDefinition(_targetTableName, targetSchema);
            var targetTable = new InMemoryTableData(tableDefinition, targetRows);
            await _tableStore.PutTableAsync(targetTable, TimeSpan.FromMinutes(30), CancellationToken.None);
        }

        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(MovieRecommendationWorker).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath, relativePath);

            return fullPath;
        }
    }
}
