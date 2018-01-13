﻿using AA1_MLP.Activations;
using AA1_MLP.Entities;
using AA1_MLP.Entities.Trainers;
using AA1_MLP.Entities.TrainersParams;
using AA1_MLP.Enums;
using AA1_MLP.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AA1_CUP
{
    /// <summary>
    /// Performing an automated grid search for hyperparameters for the model for the Cup problem
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            //Loading and parsing cup dataset
            /* CupDataManager dm = new CupDataManager();
             DataSet wholeSet = dm.LoadData(Properties.Settings.Default.TrainingSetLocation, 10, 2, permute: true, seed: 1);
             List<double> momentums = new List<double> { 0, 0.5 };
             List<double> learningRates = new List<double> { 0.005, 0.01 };
             List<double> regularizationRates = new List<double> { 0, 0.001 };
             List<int> humberOfHiddenNeurons = new List<int> { 80 };
            //screening SGD+Momentum experiments
             GradientDescentParams passedParams = new GradientDescentParams();
             passedParams.nestrov = false;
             passedParams.resilient = false;
             passedParams.resilientUpdateAccelerationRate = 0.3;
             passedParams.resilientUpdateSlowDownRate = 0.1;
             new KFoldValidation().ScreenGD(wholeSet, 5, momentums, learningRates, regularizationRates, humberOfHiddenNeurons, passedParams,5000);*/
            //screening Adam
            //new KFoldValidation().ScreenAdam(wholeSet, 5, learningRates, regularizationRates, humberOfHiddenNeurons, 5000);


            AA1_MLP.DataManagers.CupDataManager dm = new AA1_MLP.DataManagers.CupDataManager();
            DataSet trainDS = dm.LoadData("D:\\dropbox\\Dropbox\\Master Course\\SEM-3\\ML\\CM_CUP_Datasets\\60percenttrain.txt", 10, 2);
            DataSet testDS = dm.LoadData("D:\\dropbox\\Dropbox\\Master Course\\SEM-3\\ML\\CM_CUP_Datasets\\60percenttest.txt", 10, 2);


            StandardizeData(trainDS);
            StandardizeData(testDS);



            /*AdamParams passedParams = new AdamParams();
            IOptimizer trainer = new Adam();*/
             GradientDescentParams passedParams = new GradientDescentParams();
             Gradientdescent trainer = new Gradientdescent();
            passedParams.numberOfEpochs = 30000;
            passedParams.batchSize = 10;
            passedParams.trainingSet = trainDS;
            passedParams.validationSet = testDS;
            passedParams.learningRate = 0.01;
            passedParams.regularization = Regularizations.L2;
            passedParams.regularizationRate = 0.001;
            passedParams.nestrov = false;
            passedParams.momentum = 0.5;
            passedParams.NumberOfHiddenUnits = 80;

            LastTrain(testDS, passedParams, trainer, "80_final_standardized_sgdNOnestrov_hdn");
           



            //Loading and parsing cup dataset

            //  CupDataManager dm = new CupDataManager();
            //Loading the test dataset
            //DataSet TestSet = dm.LoadData(Properties.Settings.Default.TestSetLocation, 10, reportOsutput: false);
            //Loading the trained model
            //var n = AA1_MLP.Utilities.ModelManager.LoadNetwork("Final_hidn18_reg0.01_mo0.5_lr9E-06_model.AA1");

            //double MEE = 0;
            //applying the model on the test data
            //var predictions = ModelManager.GeneratorCUP(TestSet, n);
            //writing the results
            // File.WriteAllText("OMG_LOC-OSM2-TS.txt", string.Join("\n", predictions.Select(s => string.Join(",", s))));



        }

        private static void StandardizeData(DataSet trainDS)
        {
            for (int idxdataFold = 0; idxdataFold < trainDS.Inputs.ColumnCount; idxdataFold++)
            {
                double mean = trainDS.Inputs.Column(idxdataFold).Average();
                double std = Math.Sqrt((trainDS.Inputs.Column(idxdataFold) - mean).PointwisePower(2).Sum() / trainDS.Inputs.Column(idxdataFold).Count);
                trainDS.Inputs.SetColumn(idxdataFold, (trainDS.Inputs.Column(idxdataFold) - mean) / std);


            }
        }

        private static void LastTrain(DataSet testDS, INeuralTrainerParams passedParams, IOptimizer trainer,string prefix)
        {

            string path = prefix + passedParams.NumberOfHiddenUnits + "_lr" + passedParams.learningRate + "_reg" + passedParams.regularizationRate;
            //building the architecture
            Network n = new Network(new List<Layer>() {
                     new Layer(new ActivationIdentity(),true,10),
                     new Layer(new ActivationTanh(),true,passedParams.NumberOfHiddenUnits),
                  //   new Layer(new ActivationLeakyRelu(),true,40),


                     new Layer(new ActivationIdentity(),false,2),
                     }, false, AA1_MLP.Enums.WeightsInitialization.Xavier);
            passedParams.network = n;
            List<double[]> learningCurve = trainer.Train(passedParams);
            double MEE = 0;
            double MSE = 0;

            var log = ModelManager.TesterCUPRegression(testDS, n, out MEE, out  MSE);

            File.WriteAllText(path + ".txt", string.Join("\n", learningCurve.Select(s => string.Join(",", s))));
            File.AppendAllText(path + ".txt", "\nMEE:" + MEE + "MSE:" + MSE);
            File.WriteAllText(path + "predVsActual.txt", string.Join("\n", log.Select(s => string.Join(",", s))));
            

            ModelManager.SaveNetowrk(n, path + ".n");

        }
    }
}
