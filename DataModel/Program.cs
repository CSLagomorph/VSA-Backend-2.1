﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace DataModel
{
    using System.Linq;
    using Microsoft.ML;
    using commonDataModel;

    public class Program
    {
        //normal results path
        //static readonly string _dataPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "ClusteringData.csv");
        //static readonly string _modelPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "ClusteringModel.zip");        
        //static readonly string _resultsPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Results", "ClusteringData.csv");
        //associates  path
        //static readonly string _resultsPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Results", "AssociatesClusteringData.csv");
        static readonly string _resultsPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Results", "AssociatesClusteringData-2.csv");
        static readonly string _dataPath = System.IO.Path.Combine(Environment.CurrentDirectory, "AssociatesData", "ClusteringData.csv");
        static readonly string _modelPath = System.IO.Path.Combine(Environment.CurrentDirectory, "AssociatesData", "ClusteringModel.zip");

        static readonly System.Collections.Generic.List<NormalizedData> normalizedData = new System.Collections.Generic.List<NormalizedData>();
        public void test()
        {           

            /* GenerateNormalizedData();
             PerformPCA();
             PerformClustering(); */




            //using (var fileStream = new System.IO.FileStream(_modelPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Write))
            //{
            //    mlContext.Model.Save(model, dataView.Schema, fileStream);
            //}
            //var predictor = mlContext.Model.CreatePredictionEngine<InputData, ClusterPrediction>(model);

        }
        public void executeClustering()
        {
            Console.WriteLine("Executing the Clustering program");
            Console.WriteLine("Executing the Clustering program inside datamodel.....");
            Debug.WriteLine("Executing the Clustering program inside datamodel.....");

          //GenerateNormalizedData();
          PerformPCA();
           // PerformClustering();
        }

        private static void PerformPCA()
        {
            // Create a new context for ML.NET operations. It can be used for
            // exception tracking and logging, as a catalog of available operations
            // and as the source of randomness. Setting the seed to a fixed number
            // in this example to make outputs deterministic.
            var mlContext = new MLContext(seed: 0);
            //var file = System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "normalizedData.csv");
            var file = _resultsPath;
            var filedata = System.IO.File.ReadAllLines(file);
            var list = new System.Collections.Generic.List<DataPoint>();
            for (int i = 1; i < filedata.Length; i++)
            {
                var features = filedata[i].Split(',');
                int features_length = features.Length;
                var dataPoint = new DataPoint()
                {
                    Id = int.Parse(features[0]),
                    Label = float.Parse(features[1]),
                    //Features = new float[152]
                    Features = new float[features_length-2]
                };

                for (int j = 2; j < features.Length; j++)
                {
                    dataPoint.Features[j - 2] = (int.Parse(features[j]));
                }
                list.Add(dataPoint);
            }
            // Training data.


            // Convert the List<DataPoint> to IDataView, a consumable format to
            // ML.NET functions.
            var data = mlContext.Data.LoadFromEnumerable(list);
            string[] featureColumnNames =
                data.Schema
                    .Select(column => column.Name)
                    .Where(columnName => columnName != "Label").ToArray();
            IEstimator<ITransformer> dataPrepEstimator =
                mlContext.Transforms.Concatenate("selectedFeatures", "Features")
                    .Append(mlContext.Transforms.NormalizeMinMax("selectedFeatures"));
            ITransformer dataPrepTransformer = dataPrepEstimator.Fit(data);
            // 4. Pre-process the training data
            IDataView preprocessedTrainData = dataPrepTransformer.Transform(data);

            
            
            // 5. Define Stochastic Dual Coordinate Ascent machine learning estimator
            var sdcaEstimator = mlContext.Regression.Trainers.Sdca();

            // 6. Train machine learning model
            var sdcaModel = sdcaEstimator.Fit(preprocessedTrainData);

            System.Collections.Immutable.ImmutableArray<Microsoft.ML.Data.RegressionMetricsStatistics> permutationFeatureImportance =
                mlContext
                    .Regression
                    .PermutationFeatureImportance(sdcaModel, preprocessedTrainData, permutationCount: 3);

            // Order features by importance
            var featureImportanceMetrics =
                permutationFeatureImportance
                    .Select((metric, index) => new { index, metric.RSquared })
                    .OrderByDescending(myFeatures => Math.Abs(myFeatures.RSquared.Mean));

            Console.WriteLine("Feature\tPFI");


            Debug.WriteLine("Feature\tPFI");

            foreach (var feature in featureImportanceMetrics)
            {
                 //Debug.WriteLine($"{featureColumnNames[feature.index],-20}|\t{feature.RSquared.Mean:F6}");
            }

    
            var options = new Microsoft.ML.Trainers.RandomizedPcaTrainer.Options()
            {
                FeatureColumnName = nameof(DataPoint.Features),
                Rank = 1,
                Seed = 10,
            };


            // (Optional) Peek data in training DataView after applying the ProcessPipeline's transformations

            var est = mlContext.Transforms.ProjectToPrincipalComponents("pca", "Features", rank: 75, seed: 10);
            var pcaResult = est.Fit(data);


            commonDataModel.DebugHelper.PeekDataViewInDebug(mlContext, data, est, 10);
            commonDataModel.DebugHelper.PeekVectorColumnDataInDebug(mlContext, "pca", data, est, 10);

            
            //var transformed = result.Transform(data);
            //var results = mlContext.Data.CreateEnumerable<Result>(transformed,
            //                                                      reuseRowObject: false).ToList();
            //foreach (Result result1 in results)
            //{
            //    Console.WriteLine($"result1.PredictedLabel + result1.Score");
            //}

    
            var trainer = mlContext.Clustering.Trainers.KMeans(featureColumnName: "Features", numberOfClusters: 4);
            var trainingPipeline = est.Append(trainer);

            //STEP 4: Train the model fitting to the pivotDataView
            Console.WriteLine("=============== Training the model ===============");
            ITransformer trainedModel = trainingPipeline.Fit(data);

            //STEP 5: Evaluate the model and show accuracy stats
            Console.WriteLine("===== Evaluating Model's accuracy with Test data =====");
            var predictions = trainedModel.Transform(data);
            var metrics = mlContext.Clustering.Evaluate(predictions, scoreColumnName: "Score", featureColumnName: "Features");

            //Common.ConsoleHelper.PrintPrediction();
            commonDataModel.DebugHelper.PrintClusteringMetrics(trainer.ToString(), metrics);

            //STEP 6: Save/persist the trained model to a .ZIP file
            mlContext.Model.Save(trainedModel, data.Schema, "data\\out.zip");

            // Read ML.NET predictions into IEnumerable<Result>.
            var results = mlContext.Data.CreateEnumerable<Result>(predictions,
                reuseRowObject: false).ToList();
            Debug.WriteLine($"PlanId, SchoolId, MajorId, ClusterId, Distances");

            //string builder for storing associates data
            System.Text.StringBuilder sb_associates  = new System.Text.StringBuilder();
            sb_associates.Append("GeneratedPlanId,SchoolId,MajorId,Label,Maxquarters,CorecoursesQuarter,CreditsperQuarter,SummerPref,PredictedCluster");

            // Let's go through all predictions.
            for (int i = 0; i < results.Count; ++i)
            {
                // The i-th example's prediction result.
                var result = results[i];
                Debug.WriteLine($"{result.Id},{result.Features[0]}, {result.Features[1]}, {result.PredictedLabel}");

                //associates results to be stored in csv file
                sb_associates.AppendLine();
                sb_associates.Append($"{ result.Id},{ result.Features[0]}, { result.Features[1]},{result.Label}, { result.Features[2]}, { result.Features[4]}, { result.Features[6]}, { result.Features[5]}, { result.PredictedLabel}");
                
            }

            //write the data into csv file
            string _associatesresultsPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Results", "AssociatesClusteringResultsRefinedv1.csv");
            System.IO.File.WriteAllText(_associatesresultsPath, sb_associates.ToString());
            
            //Write the results-Idataview into the results file
            //using (System.IO.FileStream fs = new System.IO.FileStream(_resultsPath, System.IO.FileMode.Create))
            //{
            //    mlContext.Data.SaveAsText(predictions, fs);
            //}
        }

        // Example with 3 feature values. A training data set is a collection of
        // such examples.
        private class DataPoint
        {
            //1293-associates, 152-normal -for vector attributes length, 1297-with some extra preferences for associates
            public int Id { get; set; }
            [Microsoft.ML.Data.VectorTypeAttribute(1297)]
            public float[] Features { get; set; }

            public float Label { get; set; }
        }

        // Class used to capture prediction of DataPoint.
        private class Result
        {
            public int Id { get; set; }
            [Microsoft.ML.Data.VectorTypeAttribute(147)]
            public float[] Features { get; set; }
            [Microsoft.ML.Data.VectorTypeAttribute(100)]
            public float[] pca { get; set; }
            // Outlier gets true while inlier has false.
            public uint PredictedLabel { get; set; }
            // Inlier gets smaller score. Score is between 0 and 1.
            [Microsoft.ML.Data.VectorTypeAttribute(100)]
            public float[] Score { get; set; }
            //label
            public Single Label { get; set; }
        }




        private static void PerformClustering()
        {
            var mlContext = new Microsoft.ML.MLContext(seed: 0);
            Microsoft.ML.IDataView dataView = mlContext.Data.LoadFromTextFile<InputData>(_dataPath, hasHeader: true, separatorChar: ',');
            //var trainTestData = context.Clustering.TrainTestSplit(data, testFraction: 0.2);

            // Define trainer options.
            var options = new Microsoft.ML.Trainers.KMeansTrainer.Options
            {
                NumberOfClusters = 20,
                OptimizationTolerance = 1e-6f,
                NumberOfThreads = 10
            };


            string featuresColumnName = "Features";
            var pipeline = mlContext.Transforms
                .Concatenate(featuresColumnName, "MajorId", "SchoolId", "CourseId", "DepartmentId")
                .Append(mlContext.Clustering.Trainers.KMeans(featuresColumnName, numberOfClusters: 4));
            var model = pipeline.Fit(dataView);

            var predictions = model.Transform(dataView);
            var metrics = mlContext.Clustering.Evaluate(predictions, scoreColumnName: "Score", featureColumnName: "Features");

            //Common.ConsoleHelper.PrintPrediction();
            commonDataModel.DebugHelper.PrintClusteringMetrics("CLUSTERING 2", metrics);
        }

        private static void GenerateNormalizedData()
        {
            var courseFeatures = GetCourseFeatures();
            var normalizedStudyPlan = GetNormalizedStudyPlans(courseFeatures);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            //add headers
            //sb.Append("GeneratedPlanId,Label,SchoolId,MajorId");
            //associates
            sb.Append("GeneratedPlanId,Label,SchoolId,MajorId,Maxquarters,CorecoursesQuarter,CreditsperQuarter,SummerPref");
            foreach (System.Collections.Generic.KeyValuePair<string, bool> courseFeature in courseFeatures)
            {
                sb.Append($",{courseFeature.Key}");
            }

            foreach (NormalizedData data in normalizedStudyPlan)
            {
                sb.AppendLine();
                //sb.Append($"{data.PlanId},{data.Label}, {data.SchoolId}, {data.MajorId}");
                //associates
                sb.Append($"{data.PlanId},{data.Label}, {data.SchoolId}, {data.MajorId}, {data.max_quarters}, {data.core_courses_per_quarter}, {data.credits_per_quarter}, {data.summer_pref}");
                foreach (System.Collections.Generic.KeyValuePair<string, bool> keyValuePair in data.Courses)
                {
                    var val = keyValuePair.Value ? 1 : 0;
                    sb.Append($",{val}");
                }
            }

            //System.IO.File.WriteAllText("output.csv", sb.ToString()); // writes the normalized data into output.csv present inside bin/debug - inside DataGenerator when testing
            System.IO.File.WriteAllText(_resultsPath, sb.ToString()); // writes the normalized data into output.csv present inside bin/debug - inside DataGenerator when testing
            Debug.WriteLine("Finished writing normalized data");

            //Debug.WriteLine("OUTPUT:", sb.ToString());

        }

        private static System.Collections.Generic.List<NormalizedData> GetNormalizedStudyPlans(System.Collections.Generic.Dictionary<string, bool> courseFeatures)
        {
            var studyPlanDict = new System.Collections.Generic.Dictionary<string, NormalizedData>();
            //normal data path
            //var studyPlans = System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "allcourses.csv");
            //asscociates data path
            var studyPlans = System.IO.Path.Combine(Environment.CurrentDirectory, "AssociatesData", "allcourses.csv");

            var studyPlandata = System.IO.File.ReadAllLines(studyPlans);
            for (int i = 1; i < studyPlandata.Length; i++)
            {
                var line = studyPlandata[i].Split(',');
                var planId = line[1];
                var majorId = line[5];
                var schoolId = line[6];
                var courseId = line[4];
                //var label = line[5];
                var label = line[7];
                //specifically for associates
                var max_quarters = line[8];
                var core_courses_per_quarter = line[9];
                var credits_per_quarter = line[10];
                var summer_pref = line[11];

                if (!studyPlanDict.ContainsKey(planId))
                {
                    studyPlanDict.Add(planId, new NormalizedData(planId, majorId, schoolId, label, courseFeatures,max_quarters, core_courses_per_quarter, credits_per_quarter, summer_pref));
                }
                var data = studyPlanDict[planId];
                data.Courses[courseId] = true;

            }
            return studyPlanDict.Select(s => s.Value).ToList();

        }

        private static System.Collections.Generic.Dictionary<string, bool> GetCourseFeatures()
        {
            //normal data path
            //var coursesData = System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "courses.csv");
            //var studyPlans = System.IO.Path.Combine(Environment.CurrentDirectory, "Data", "allcourses.csv");
            //associates data
            var coursesData = System.IO.Path.Combine(Environment.CurrentDirectory, "AssociatesData", "courses.csv");
            var studyPlans = System.IO.Path.Combine(Environment.CurrentDirectory, "AssociatesData", "allcourses.csv");

            System.Collections.Generic.Dictionary<string, bool> courseList = new System.Collections.Generic.Dictionary<string, bool>();

            var courses = System.IO.File.ReadAllLines(coursesData);
            for (int i = 1; i < courses.Length; i++)
            {
                courseList.Add(courses[i], false);
            }

            return courseList;
        }
    }

    public class NormalizedData
    {
        public string PlanId { get; set; }
        public string SchoolId { get; set; }
        public string MajorId { get; set; }
        public float Label { get; set; }
        public System.Collections.Generic.Dictionary<string, bool> Courses = new System.Collections.Generic.Dictionary<string, bool>();

        //associates sp-ecific
        public string max_quarters { get; set; }

        public string core_courses_per_quarter { get; set; }
        public string credits_per_quarter { get; set; }

        public string summer_pref { get; set; }

        public NormalizedData(string planId, string majorId, string schoolId, string label, System.Collections.Generic.Dictionary<string, bool> features, string max_quarters, string core_courses_per_quarter, string credits_per_quarter, string summerpref)
        {
            this.PlanId = planId;
            this.MajorId = majorId;
            this.SchoolId = schoolId;
            this.Label = float.Parse(label);
            this.Courses = new System.Collections.Generic.Dictionary<string, bool>();
            foreach (System.Collections.Generic.KeyValuePair<string, bool> keyValuePair in features)
            {
                this.Courses.Add(keyValuePair.Key, keyValuePair.Value);
            }
            //associates specific
            this.max_quarters = max_quarters;
            if (core_courses_per_quarter.Equals("NULL"))
                this.core_courses_per_quarter = "0";
            else
                this.core_courses_per_quarter = core_courses_per_quarter;
            if (credits_per_quarter.Equals("NULL"))
                this.credits_per_quarter = "0";
            else
                this.credits_per_quarter = credits_per_quarter;
            if (summerpref.Equals("Yes") || summerpref.Equals("Y"))
                this.summer_pref = "1";
            else
                this.summer_pref = "0";

        }
    }

    public class InputData
    {
        [Microsoft.ML.Data.LoadColumnAttribute(0)]
        public float MajorId { get; set; }
        [Microsoft.ML.Data.LoadColumnAttribute(1)]
        public float SchoolId { get; set; }
        [Microsoft.ML.Data.LoadColumnAttribute(2)]
        public float WeakLabelScore { get; set; }
        [Microsoft.ML.Data.LoadColumnAttribute(3)]
        public float CourseId { get; set; }
        [Microsoft.ML.Data.LoadColumnAttribute(4)]
        public float DepartmentId { get; set; }
    }

    public class ClusterPrediction
    {
        [Microsoft.ML.Data.ColumnNameAttribute("PredictedLabel")]
        public uint PredictedClusterId;

        [Microsoft.ML.Data.ColumnNameAttribute("Score")]
        public float[] Distances;
    }
}