using System;
using System.Collections.Generic;
using System.Text;

using System;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.ML.Data;
using Microsoft.ML;
using static Microsoft.ML.TrainCatalogBase;
using System.Diagnostics;

namespace commonDataModel
{

    public static class DebugHelper
    {
        public static void PrintPrediction(string prediction)
        {
            Debug.WriteLine($"*************************************************");
            Debug.WriteLine($"Predicted : {prediction}");
            Debug.WriteLine($"*************************************************");
        }

        public static void PrintRegressionPredictionVersusObserved(string predictionCount, string observedCount)
        {
            Debug.WriteLine($"-------------------------------------------------");
            Debug.WriteLine($"Predicted : {predictionCount}");
            Debug.WriteLine($"Actual:     {observedCount}");
            Debug.WriteLine($"-------------------------------------------------");
        }

        public static void PrintRegressionMetrics(string name, RegressionMetrics metrics)
        {
            Debug.WriteLine($"*************************************************");
            Debug.WriteLine($"*       Metrics for {name} regression model      ");
            Debug.WriteLine($"*------------------------------------------------");
            Debug.WriteLine($"*       LossFn:        {metrics.LossFunction:0.##}");
            Debug.WriteLine($"*       R2 Score:      {metrics.RSquared:0.##}");
            Debug.WriteLine($"*       Absolute loss: {metrics.MeanAbsoluteError:#.##}");
            Debug.WriteLine($"*       Squared loss:  {metrics.MeanSquaredError:#.##}");
            Debug.WriteLine($"*       RMS loss:      {metrics.RootMeanSquaredError:#.##}");
            Debug.WriteLine($"*************************************************");
        }

        public static void PrintBinaryClassificationMetrics(string name, CalibratedBinaryClassificationMetrics metrics)
        {
            Debug.WriteLine($"************************************************************");
            Debug.WriteLine($"*       Metrics for {name} binary classification model      ");
            Debug.WriteLine($"*-----------------------------------------------------------");
            Debug.WriteLine($"*       Accuracy: {metrics.Accuracy:P2}");
            Debug.WriteLine($"*       Area Under Curve:      {metrics.AreaUnderRocCurve:P2}");
            Debug.WriteLine($"*       Area under Precision recall Curve:  {metrics.AreaUnderPrecisionRecallCurve:P2}");
            Debug.WriteLine($"*       F1Score:  {metrics.F1Score:P2}");
            Debug.WriteLine($"*       LogLoss:  {metrics.LogLoss:#.##}");
            Debug.WriteLine($"*       LogLossReduction:  {metrics.LogLossReduction:#.##}");
            Debug.WriteLine($"*       PositivePrecision:  {metrics.PositivePrecision:#.##}");
            Debug.WriteLine($"*       PositiveRecall:  {metrics.PositiveRecall:#.##}");
            Debug.WriteLine($"*       NegativePrecision:  {metrics.NegativePrecision:#.##}");
            Debug.WriteLine($"*       NegativeRecall:  {metrics.NegativeRecall:P2}");
            Debug.WriteLine($"************************************************************");
        }

        public static void PrintAnomalyDetectionMetrics(string name, AnomalyDetectionMetrics metrics)
        {
            Debug.WriteLine($"************************************************************");
            Debug.WriteLine($"*       Metrics for {name} anomaly detection model      ");
            Debug.WriteLine($"*-----------------------------------------------------------");
            Debug.WriteLine($"*       Area Under ROC Curve:                       {metrics.AreaUnderRocCurve:P2}");
            Debug.WriteLine($"*       Detection rate at false positive count: {metrics.DetectionRateAtFalsePositiveCount}");
            Debug.WriteLine($"************************************************************");
        }

        public static void PrintMultiClassClassificationMetrics(string name, MulticlassClassificationMetrics metrics)
        {
            Debug.WriteLine($"************************************************************");
            Debug.WriteLine($"*    Metrics for {name} multi-class classification model   ");
            Debug.WriteLine($"*-----------------------------------------------------------");
            Debug.WriteLine($"    AccuracyMacro = {metrics.MacroAccuracy:0.####}, a value between 0 and 1, the closer to 1, the better");
            Debug.WriteLine($"    AccuracyMicro = {metrics.MicroAccuracy:0.####}, a value between 0 and 1, the closer to 1, the better");
            Debug.WriteLine($"    LogLoss = {metrics.LogLoss:0.####}, the closer to 0, the better");
            Debug.WriteLine($"    LogLoss for class 1 = {metrics.PerClassLogLoss[0]:0.####}, the closer to 0, the better");
            Debug.WriteLine($"    LogLoss for class 2 = {metrics.PerClassLogLoss[1]:0.####}, the closer to 0, the better");
            Debug.WriteLine($"    LogLoss for class 3 = {metrics.PerClassLogLoss[2]:0.####}, the closer to 0, the better");
            Debug.WriteLine($"************************************************************");
        }

        public static void PrintRegressionFoldsAverageMetrics(string algorithmName, IReadOnlyList<CrossValidationResult<RegressionMetrics>> crossValidationResults)
        {
            var L1 = crossValidationResults.Select(r => r.Metrics.MeanAbsoluteError);
            var L2 = crossValidationResults.Select(r => r.Metrics.MeanSquaredError);
            var RMS = crossValidationResults.Select(r => r.Metrics.RootMeanSquaredError);
            var lossFunction = crossValidationResults.Select(r => r.Metrics.LossFunction);
            var R2 = crossValidationResults.Select(r => r.Metrics.RSquared);

            Debug.WriteLine($"*************************************************************************************************************");
            Debug.WriteLine($"*       Metrics for {algorithmName} Regression model      ");
            Debug.WriteLine($"*------------------------------------------------------------------------------------------------------------");
            Debug.WriteLine($"*       Average L1 Loss:    {L1.Average():0.###} ");
            Debug.WriteLine($"*       Average L2 Loss:    {L2.Average():0.###}  ");
            Debug.WriteLine($"*       Average RMS:          {RMS.Average():0.###}  ");
            Debug.WriteLine($"*       Average Loss Function: {lossFunction.Average():0.###}  ");
            Debug.WriteLine($"*       Average R-squared: {R2.Average():0.###}  ");
            Debug.WriteLine($"*************************************************************************************************************");
        }

        public static void PrintMulticlassClassificationFoldsAverageMetrics(
                                         string algorithmName,
                                       IReadOnlyList<CrossValidationResult<MulticlassClassificationMetrics>> crossValResults
                                                                           )
        {
            var metricsInMultipleFolds = crossValResults.Select(r => r.Metrics);

            var microAccuracyValues = metricsInMultipleFolds.Select(m => m.MicroAccuracy);
            var microAccuracyAverage = microAccuracyValues.Average();
            var microAccuraciesStdDeviation = CalculateStandardDeviation(microAccuracyValues);
            var microAccuraciesConfidenceInterval95 = CalculateConfidenceInterval95(microAccuracyValues);

            var macroAccuracyValues = metricsInMultipleFolds.Select(m => m.MacroAccuracy);
            var macroAccuracyAverage = macroAccuracyValues.Average();
            var macroAccuraciesStdDeviation = CalculateStandardDeviation(macroAccuracyValues);
            var macroAccuraciesConfidenceInterval95 = CalculateConfidenceInterval95(macroAccuracyValues);

            var logLossValues = metricsInMultipleFolds.Select(m => m.LogLoss);
            var logLossAverage = logLossValues.Average();
            var logLossStdDeviation = CalculateStandardDeviation(logLossValues);
            var logLossConfidenceInterval95 = CalculateConfidenceInterval95(logLossValues);

            var logLossReductionValues = metricsInMultipleFolds.Select(m => m.LogLossReduction);
            var logLossReductionAverage = logLossReductionValues.Average();
            var logLossReductionStdDeviation = CalculateStandardDeviation(logLossReductionValues);
            var logLossReductionConfidenceInterval95 = CalculateConfidenceInterval95(logLossReductionValues);

            Debug.WriteLine($"*************************************************************************************************************");
            Debug.WriteLine($"*       Metrics for {algorithmName} Multi-class Classification model      ");
            Debug.WriteLine($"*------------------------------------------------------------------------------------------------------------");
            Debug.WriteLine($"*       Average MicroAccuracy:    {microAccuracyAverage:0.###}  - Standard deviation: ({microAccuraciesStdDeviation:#.###})  - Confidence Interval 95%: ({microAccuraciesConfidenceInterval95:#.###})");
            Debug.WriteLine($"*       Average MacroAccuracy:    {macroAccuracyAverage:0.###}  - Standard deviation: ({macroAccuraciesStdDeviation:#.###})  - Confidence Interval 95%: ({macroAccuraciesConfidenceInterval95:#.###})");
            Debug.WriteLine($"*       Average LogLoss:          {logLossAverage:#.###}  - Standard deviation: ({logLossStdDeviation:#.###})  - Confidence Interval 95%: ({logLossConfidenceInterval95:#.###})");
            Debug.WriteLine($"*       Average LogLossReduction: {logLossReductionAverage:#.###}  - Standard deviation: ({logLossReductionStdDeviation:#.###})  - Confidence Interval 95%: ({logLossReductionConfidenceInterval95:#.###})");
            Debug.WriteLine($"*************************************************************************************************************");

        }

        public static double CalculateStandardDeviation(IEnumerable<double> values)
        {
            double average = values.Average();
            double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / (values.Count() - 1));
            return standardDeviation;
        }

        public static double CalculateConfidenceInterval95(IEnumerable<double> values)
        {
            double confidenceInterval95 = 1.96 * CalculateStandardDeviation(values) / Math.Sqrt((values.Count() - 1));
            return confidenceInterval95;
        }

        public static void PrintClusteringMetrics(string name, ClusteringMetrics metrics)
        {
            Debug.WriteLine($"*************************************************");
            Debug.WriteLine($"*       Metrics for {name} clustering model      ");
            Debug.WriteLine($"*------------------------------------------------");
            Debug.WriteLine($"*       Average Distance: {metrics.AverageDistance}");
            Debug.WriteLine($"*       Davies Bouldin Index is: {metrics.DaviesBouldinIndex}");
            Debug.WriteLine($"*************************************************");
        }

        public static void ShowDataViewInDebug(MLContext mlContext, IDataView dataView, int numberOfRows = 4)
        {
            string msg = string.Format("Show data in DataView: Showing {0} rows with the columns", numberOfRows.ToString());
            DebugWriteHeader(msg);

            var preViewTransformedData = dataView.Preview(maxRows: numberOfRows);

            foreach (var row in preViewTransformedData.RowView)
            {
                var ColumnCollection = row.Values;
                string lineToPrint = "Row--> ";
                foreach (KeyValuePair<string, object> column in ColumnCollection)
                {
                    lineToPrint += $"| {column.Key}:{column.Value}";
                }
                Debug.WriteLine(lineToPrint + "\n");
            }
        }

        [Conditional("DEBUG")]
        // This method using 'DebuggerExtensions.Preview()' should only be used when debugging/developing, not for release/production trainings
        public static void PeekDataViewInDebug(MLContext mlContext, IDataView dataView, IEstimator<ITransformer> pipeline, int numberOfRows = 4)
        {
            string msg = string.Format("Peek data in DataView: Showing {0} rows with the columns", numberOfRows.ToString());
            DebugWriteHeader(msg);

            //https://github.com/dotnet/machinelearning/blob/master/docs/code/MlNetCookBook.md#how-do-i-look-at-the-intermediate-data
            var transformer = pipeline.Fit(dataView);
            var transformedData = transformer.Transform(dataView);

            // 'transformedData' is a 'promise' of data, lazy-loading. call Preview  
            //and iterate through the returned collection from preview.

            var preViewTransformedData = transformedData.Preview(maxRows: numberOfRows);

            foreach (var row in preViewTransformedData.RowView)
            {
                var ColumnCollection = row.Values;
                string lineToPrint = "Row--> ";
                foreach (KeyValuePair<string, object> column in ColumnCollection)
                {
                    lineToPrint += $"| {column.Key}:{column.Value}";
                }
                Debug.WriteLine(lineToPrint + "\n");
            }
        }

        [Conditional("DEBUG")]
        // This method using 'DebuggerExtensions.Preview()' should only be used when debugging/developing, not for release/production trainings
        public static void PeekVectorColumnDataInDebug(MLContext mlContext, string columnName, IDataView dataView, IEstimator<ITransformer> pipeline, int numberOfRows = 4)
        {
            string msg = string.Format("Peek data in DataView: : Show {0} rows with just the '{1}' column", numberOfRows, columnName);
            DebugWriteHeader(msg);

            var transformer = pipeline.Fit(dataView);
            var transformedData = transformer.Transform(dataView);

            // Extract the 'Features' column.
            var someColumnData = transformedData.GetColumn<float[]>(columnName)
                                                        .Take(numberOfRows).ToList();

            // print to Debug the peeked rows

            int currentRow = 0;
            someColumnData.ForEach(row => {
                currentRow++;
                String concatColumn = String.Empty;
                foreach (float f in row)
                {
                    concatColumn += f.ToString();
                }

                Debug.WriteLine("\n");
                string rowMsg = string.Format("**** Row {0} with '{1}' field value ****", currentRow, columnName);
                Debug.WriteLine(rowMsg);
                Debug.WriteLine(concatColumn);
                Debug.WriteLine("\n");
            });
        }

        public static void DebugWriteHeader(params string[] lines)
        {
            //var defaultColor = Debug.ForegroundColor;
            //Debug.ForegroundColor = DebugColor.Yellow;
            Debug.WriteLine(" ");
            foreach (var line in lines)
            {
                Debug.WriteLine(line);
            }
            var maxLength = lines.Select(x => x.Length).Max();
            Debug.WriteLine(new string('#', maxLength));
            //Debug.ForegroundColor = defaultColor;
        }

        public static void DebugWriterSection(params string[] lines)
        {
            //var defaultColor = Debug.ForegroundColor;
            //Debug.ForegroundColor = DebugColor.Blue;
            Debug.WriteLine(" ");
            foreach (var line in lines)
            {
                Debug.WriteLine(line);
            }
            var maxLength = lines.Select(x => x.Length).Max();
            Debug.WriteLine(new string('-', maxLength));
            //Debug.ForegroundColor = defaultColor;
        }

        public static void DebugPressAnyKey()
        {
            //var defaultColor = Debug.ForegroundColor;
            //Debug.ForegroundColor = DebugColor.Green;
            Debug.WriteLine(" ");
            Debug.WriteLine("Press any key to finish.");
            //Debug.ReadKey();
        }

        public static void DebugWriteException(params string[] lines)
        {
            //var defaultColor = Debug.ForegroundColor;
           // Debug.ForegroundColor = DebugColor.Red;
            const string exceptionTitle = "EXCEPTION";
            Debug.WriteLine(" ");
            Debug.WriteLine(exceptionTitle);
            Debug.WriteLine(new string('#', exceptionTitle.Length));
            //Debug.ForegroundColor = defaultColor;
            foreach (var line in lines)
            {
                Debug.WriteLine(line);
            }
        }

        public static void DebugWriteWarning(params string[] lines)
        {
           // var defaultColor = Debug.ForegroundColor;
            //Debug.ForegroundColor = DebugColor.DarkMagenta;
            const string warningTitle = "WARNING";
            Debug.WriteLine(" ");
            Debug.WriteLine(warningTitle);
            Debug.WriteLine(new string('#', warningTitle.Length));
            //Debug.ForegroundColor = defaultColor;
            foreach (var line in lines)
            {
                Debug.WriteLine(line);
            }
        }
    }
}



