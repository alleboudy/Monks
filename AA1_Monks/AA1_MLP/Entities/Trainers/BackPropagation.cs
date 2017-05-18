﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomExtensionMethods;
using MathNet.Numerics.LinearAlgebra;
using AA1_MLP.Enums;

namespace AA1_MLP.Entities.Trainers
{
    public class BackPropagation : IOptimizer
    {
        public override List<double[]> Train(Network network, DataSet trainingSet, double learningRate, int numberOfEpochs, bool shuffle = false, int? batchSize = null, bool debug = false, double regularizationRate = 0, Regularizations regularization = Regularizations.None, double momentum = 0, bool resilient = false, double resilientUpdateAccelerationRate = 1, double resilientUpdateSlowDownRate = 1, DataSet validationSet = null, double? trueThreshold = 0.5)
        {
            //int valSplitSize = 0;
            List<double[]> learningCurve = new List<double[]>();
            List<int> indices = Enumerable.Range(0, trainingSet.Labels.RowCount).ToList();
            List<int> test_indices = null;
            DataSet test = new DataSet(null, null);
            if (validationSet != null)
            {
                test_indices = Enumerable.Range(0, validationSet.Labels.RowCount).ToList();
                if (shuffle)
                {
                    test_indices.Shuffle();
                }

                test.Inputs = CreateMatrix.Dense(test_indices.Count, validationSet.Inputs.ColumnCount, 0.0);
                test.Labels = CreateMatrix.Dense(test_indices.Count, validationSet.Labels.ColumnCount, 0.0);
                for (int i = 0; i < test_indices.Count; i++)
                {
                    test.Inputs.SetRow(i, validationSet.Inputs.Row(test_indices[i]));//, 1, 0, trainingSet.Inputs.ColumnCount));
                    test.Labels.SetRow(i, validationSet.Labels.Row(test_indices[i]));//.SubMatrix(indices[i], 1, 0, trainingSet.Labels.ColumnCount));

                }
            }
            if (shuffle)
            {
                indices.Shuffle();
            }

            /*   DataSet validation = new DataSet(null, null);
               DataSet trainingSet = new DataSet(null, null);
               */
            //if (validationSplit != null)
            //{
            //    valSplitSize = (int)(indices.Count * validationSplit);

            //    validation.Inputs = CreateMatrix.Dense(valSplitSize, trainingSet.Inputs.ColumnCount, 0.0);
            //    validation.Labels = CreateMatrix.Dense(valSplitSize, trainingSet.Labels.ColumnCount, 0.0);

            //    for (int i = 0; i < valSplitSize; i++)
            //    {

            //        validation.Inputs.SetRow(i, trainingSet.Inputs.Row(indices[i]));// .SubMatrix(indices[i], 1, 0, trainingSet.Inputs.ColumnCount));
            //        validation.Labels.SetRow(i, trainingSet.Labels.Row(indices[i]));//SubMatrix(indices[i], 1, 0, trainingSet.Labels.ColumnCount));

            //    }

            //    trainingSet.Inputs = CreateMatrix.Dense(indices.Count - valSplitSize, trainingSet.Inputs.ColumnCount, 0.0);
            //    trainingSet.Labels = CreateMatrix.Dense(indices.Count - valSplitSize, trainingSet.Labels.ColumnCount, 0.0);

            //    for (int i = valSplitSize; i < indices.Count; i++)
            //    {
            //        trainingSet.Inputs.SetRow(i - valSplitSize, trainingSet.Inputs.Row(indices[i]));//, 1, 0, trainingSet.Inputs.ColumnCount));
            //        trainingSet.Labels.SetRow(i - valSplitSize, trainingSet.Labels.Row(indices[i]));//.SubMatrix(indices[i], 1, 0, trainingSet.Labels.ColumnCount));

            //    }
            //}
            //else
            /* {
                 trainingSet.Inputs = trainingSet.Inputs;
                 //test.Inputs = trainingSet.Inputs;
                 trainingSet.Labels = trainingSet.Labels;
                 //test.Labels = trainingSet.Labels;

             }*/

            Matrix<double> batchesIndices = null;//a 2d matrix of shape(nmberOfBatches,2), rows are batches, row[0] =barchstart, row[1] = batchEnd 
            Dictionary<int, Matrix<double>> previousWeightsUpdate = null;
            Dictionary<int, Matrix<double>> PreviousUpdateSigns = new Dictionary<int, Matrix<double>>();

            for (int epoch = 0; epoch < numberOfEpochs; epoch++)
            {


                if (batchSize != null)
                {
                    var numberOfBatches = (int)Math.Ceiling(((trainingSet.Labels.RowCount / (double)(batchSize))));

                    batchesIndices = CreateMatrix.Dense(numberOfBatches, 2, 0.0);

                    for (int j = 0; j < numberOfBatches; j++)
                    {
                        batchesIndices.SetRow(j, new double[] { j * (double)batchSize, Math.Min(trainingSet.Inputs.RowCount - 1, (j + 1) * (double)batchSize - 1) });
                    }


                }
                else
                {
                    batchesIndices = CreateMatrix.Dense(1, 2, 0.0);
                    batchesIndices.SetRow(0, new double[] { 0, trainingSet.Inputs.RowCount - 1 });
                }

                double iterationLoss = 0;

                for (int i = 0; i < batchesIndices.RowCount; i++)//for each batch
                {

                    double batchLoss = 0;
                    Dictionary<int, Matrix<double>> weightsUpdates = new Dictionary<int, Matrix<double>>();
                    int numberOfBatchExamples = (((int)batchesIndices.Row(i).At(1) - (int)batchesIndices.Row(i).At(0)) + 1);
                    var batchIndices = Enumerable.Range((int)batchesIndices.Row(i).At(0), (int)batchesIndices.Row(i).At(1) - (int)batchesIndices.Row(i).At(0) + 1).ToList();
                    if (shuffle)
                    {
                        batchIndices.Shuffle();
                    }
                    foreach (int k in batchIndices)//for each elemnt in th batch
                    {
                        var nwOutput = network.ForwardPropagation(trainingSet.Inputs.Row(k));
                        // network.Layers[0].LayerActivationsSumInputs = trainingSet.Inputs.Row(k);
                        var label = trainingSet.Labels.Row(k);
                        //comute the loss 
                        //batchLoss += ((label - nwOutput.Map(s => s >= 0.5 ? 1.0 : 0.0)).PointwiseMultiply(label - nwOutput.Map(s => s > 0.5 ? 1.0 : 0.0))).Sum();
                        batchLoss += ((label - nwOutput).PointwiseMultiply(label - nwOutput)).Sum();

                        /*

                                                if (regularization != Regularizations.None)
                                                    for (int s = 0; s < network.Weights.Count; s++)
                                                    {
                                                        batchLoss += regularizationRate * network.Weights[s].PointwiseMultiply(network.Weights[s]).ColumnSums().Sum();
                                                    }
                                                */


                        //batchLoss += -1 * label * (nwOutput.Map(f => Math.Log(f))) - (1 - label) * (1 - nwOutput.Map(f => Math.Log(f)));
                        var residual = label - nwOutput;
                        //var residual = -((label.PointwiseMultiply(nwOutput.Map(f => Math.Log(f)))) + (1 - label).PointwiseMultiply((nwOutput.Map(f => Math.Log(1 - f)))));

                        if (debug)
                        {
                            Console.WriteLine("Target:{0}", label);
                            Console.WriteLine("Calculated:{0}", nwOutput);
                            Console.WriteLine("Target-calculated (residual):{0}", residual);
                        }

                        residual = residual.Map(r => double.IsNaN(r) ? 0 : r);
                        //compute the error and backpropagate it 
                        //batchLoss += residual.Sum();
                        // network.Layers.Last().Delta = residual;
                        //compute the delta of previous layer
                        //Matrix<double> previousDeltaOptSum = null;
                        for (int layerIndex = network.Layers.Count - 1; layerIndex >= 1; layerIndex--)
                        {
                            if (debug)
                                Console.WriteLine("##### enting backpropagation layer index: {0} ######", layerIndex);
                            Vector<double> derivative = network.Layers[layerIndex].Activation.CalculateDerivative(network.Layers[layerIndex].LayerActivationsSumInputs);
                            /* if (network.Layers[layerIndex].Bias)
                             {
                                  derivative = network.Layers[layerIndex].Activation.CalculateDerivative(network.Layers[layerIndex].LayerActivationsSumInputs);

                             }
                             else
                             {
                                  derivative = network.Layers[layerIndex].Activation.CalculateDerivative(network.Layers[layerIndex].LayerActivationsSumInputs);

                             }*/
                            //  var residualTimesDerivative = residual.PointwiseMultiply(derivative);
                            if (debug)
                            {
                                Console.WriteLine("output sum(the sum inputted to the activation(LayerActivationsSumInputs)): {0}", network.Layers[layerIndex].LayerActivationsSumInputs);
                                Console.WriteLine("derivative: {0}", derivative);
                                Console.WriteLine("output sum margin of error(residual): {0}", residual);
                                Console.WriteLine("Delta output sum of Layer(residual*derivative): {0}", layerIndex);
                                //    Console.WriteLine(residualTimesDerivative);

                            }
                            if (layerIndex == network.Layers.Count - 1)
                            {
                                network.Layers[layerIndex].Delta = residual.PointwiseMultiply(derivative);

                            }
                            else
                            {
                                Matrix<double> wei = network.Weights[layerIndex];
                                if (wei.RowCount > derivative.Count)
                                {
                                    wei = wei.SubMatrix(0, wei.RowCount - 1, 0, wei.ColumnCount);
                                }

                                network.Layers[layerIndex].Delta = (wei * network.Layers[layerIndex + 1].Delta).PointwiseMultiply(derivative);
                            }
                            //if (layerIndex != 1)
                            //{
                            //    /*    Matrix<double> nwWeights = null;
                            //        if (network.Layers[layerIndex - 1].Bias)
                            //        {
                            //            nwWeights = network.Weights[layerIndex - 1].SubMatrix(1, network.Weights[layerIndex - 1].RowCount - 1, 0, network.Weights[layerIndex - 1].ColumnCount);
                            //        }
                            //        else
                            //        {
                            //            nwWeights = network.Weights[layerIndex - 1];

                            //        }*/
                            //    var der = (network.Layers[layerIndex].Activation.CalculateDerivative(network.Layers[layerIndex].LayerActivationsSumInputs));
                            //    network.Layers[layerIndex - 1].Delta = network.Layers[layerIndex].Delta.PointwiseMultiply( der);


                            //}

                            //   residual = network.Layers[layerIndex - 1].Delta;

                            // network.Weights[layerIndex - 1] -= LearningRate * network.Layers[layerIndex].Delta.OuterProduct( network.Layers[layerIndex - 1].LayerActivationsSumInputs);
                            if (!weightsUpdates.ContainsKey(layerIndex - 1))
                            {
                                weightsUpdates.Add(layerIndex - 1, CreateMatrix.Dense(network.Weights[layerIndex - 1].RowCount, network.Weights[layerIndex - 1].ColumnCount, 0.0));
                            }
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Weights layer:{0} {1}", layerIndex - 1, network.Weights[layerIndex - 1]);
                            Console.ResetColor();
                            Matrix<double> weightsUpdate = null;
                            var acti = network.Layers[layerIndex - 1].LayerActivations;
                            if (network.Layers[layerIndex - 1].Bias && acti.Count - network.Layers[layerIndex - 1].NumberOfNeurons < 1)
                            {
                                var l = acti.ToList();
                                l.Add(1);

                                acti = CreateVector.Dense(l.ToArray());
                            }
                            weightsUpdate = acti.OuterProduct(network.Layers[layerIndex].Delta);
                            //  if (layerIndex == network.Layers.Count - 1)
                            {
                                //delta output sum * hidden layer results

                                // outrprod = network.Layers[layerIndex].Delta.Vec2Vecmultiply(network.Layers[layerIndex - 1].LayerActivations);
                            }
                            // if (layerIndex == network.Layers.Count - 1)
                            {
                                // previousDeltaOptSum = network.Layers[layerIndex].Activation.CalculateDerivative(network.Layers[layerIndex].LayerActivationsSumInputs).Vec2Vecmultiply(network.Layers[layerIndex].Delta);
                                //weightsUpdate = network.Layers[layerIndex - 1].LayerActivations.OuterProduct(network.Layers[layerIndex].Delta);
                                //(network.Layers[layerIndex].LayerActivations).Vec2Mtrxmultiply(previousDeltaOptSum).Transpose();

                            }
                            /*   else
                               {
                                   previousDeltaOptSum = previousDeltaOptSum * network.Weights[layerIndex].Transpose().Mtrx2Vecmultiply(network.Layers[layerIndex].Activation.CalculateDerivative(network.Layers[layerIndex].LayerActivationsSumInputs));
                                   weightsUpdate = network.Layers[layerIndex].LayerActivationsSumInputs.Vec2Mtrxmultiply(previousDeltaOptSum);
                               }*/


                            // outrprod = network.Layers[layerIndex].Delta.Vec2Mtrxmultiply(network.Weights[layerIndex].Mtrx2Vecmultiply((network.Layers[layerIndex].Activation.CalculateDerivative(network.Layers[layerIndex].LayerActivationsSumInputs)))).Mtrx2Vecmultiply(network.Layers[layerIndex - 1].LayerActivations);

                            // else if (layerIndex == 1) { break; }
                            /*  else
                              {
                                  //delta output sum * hidden-to-outer weights * S'(hidden sum)
                              }
                              */

                            weightsUpdates[layerIndex - 1] = weightsUpdates[layerIndex - 1].Add(weightsUpdate);
                            // weightsUpdates[layerIndex - 1] = weightsupdatematrix;
                            if (debug)
                            {
                                Console.WriteLine("weights updates of weightsMatrix(learning rate* outerproduct(Layer{1} delta,layer{2} output from activations) ): {0} ", layerIndex - 1, layerIndex, layerIndex - 1);
                                Console.WriteLine("learning rate:{0}", learningRate);
                                Console.WriteLine("Layer:{0} delta: {1}", layerIndex, network.Layers[layerIndex].Delta);
                                Console.WriteLine("layer{0} output from activations:{1}", layerIndex - 1, network.Layers[layerIndex - 1].LayerActivations);
                                Console.WriteLine(weightsUpdate);
                                Console.WriteLine("----------- BackPropagation LayerIndex{0} ------------", layerIndex);

                            }

                        }

                        if (debug)
                            Console.WriteLine("-------- Batch:{0} element:{1} end-----", i, k);
                    }
                    if (debug)
                        Console.WriteLine("batch end");

                    //EpochBatchesLosses.Add(new double[] { batchLoss / numberOfBatchExamples });
                    // batchLoss /= (((int)batchesIndices.Row(i).At(1) - (int)batchesIndices.Row(i).At(0)) + 1);

                    for (int y = 0; y < weightsUpdates.Keys.Count; y++)
                    {
                        //weightsUpdates[y] /= numberOfBatchExamples;
                        var resilientLearningRates = CreateMatrix.Dense(network.Weights[y].RowCount, network.Weights[y].ColumnCount, (epoch == 0) && resilient ? resilientUpdateSlowDownRate * learningRate : learningRate);
                        if (resilient && PreviousUpdateSigns.ContainsKey(y))
                        {
                            var currentUpdateSigns = weightsUpdates[y].PointwiseSign();
                            resilientLearningRates = PreviousUpdateSigns[y].PointwiseMultiply(currentUpdateSigns).Map(s => s > 0 ? learningRate * resilientUpdateAccelerationRate : learningRate * resilientUpdateSlowDownRate);
                        }


                        var momentumUpdate = CreateMatrix.Dense(network.Weights[y].RowCount, network.Weights[y].ColumnCount, 0.0);
                        if (previousWeightsUpdate != null)
                        {
                            momentumUpdate += momentum * previousWeightsUpdate[y];
                        }
                        if (regularization != Regularizations.None)
                        {
                            //network.Weights[y] = (((1 - resilientLearningRates * regularizationRate / (((int)batchesIndices.Row(i).At(1) - (int)batchesIndices.Row(i).At(0)) + 1))).PointwiseMultiply(network.Weights[y]) + resilientLearningRates.PointwiseMultiply(momentumUpdate + weightsUpdates[y]));
                            //network.Weights[y] = 2 * regularizationRate * network.Weights[y] + resilientLearningRates.PointwiseMultiply(momentumUpdate + weightsUpdates[y]);
                            if (network.Layers[y].Bias)
                            {
                                Matrix<double> w = network.Weights[y].Clone();
                                w.ClearRow(w.RowCount - 1);
                                network.Weights[y] += resilientLearningRates.PointwiseMultiply(momentumUpdate + (weightsUpdates[y] - 2 * regularizationRate * w));
                            }
                            else
                            {
                                network.Weights[y] += resilientLearningRates.PointwiseMultiply(momentumUpdate + (weightsUpdates[y] - 2 * regularizationRate * network.Weights[y]));
                            }
                        }
                        else
                            network.Weights[y] += resilientLearningRates.PointwiseMultiply(momentumUpdate + weightsUpdates[y]);

                        if (!PreviousUpdateSigns.ContainsKey(y))
                        {
                            PreviousUpdateSigns.Add(y, null);
                        }
                        PreviousUpdateSigns[y] = weightsUpdates[y].PointwiseSign();
                    }
                    previousWeightsUpdate = weightsUpdates;

                    iterationLoss += batchLoss / ((int)batchesIndices.Row(i).At(1) - (int)batchesIndices.Row(i).At(0) + 1);
                    Console.WriteLine("Batch: {0} Error: {1}", i, batchLoss);
                }


                iterationLoss /= batchesIndices.RowCount;

                //if (regularization != Regularizations.None)
                //    for (int s = 0; s < network.Weights.Count; s++)
                //    {
                //        if (network.Layers[s].Bias)
                //        {
                //            iterationLoss += regularizationRate * network.Weights[s].SubMatrix(0, network.Weights[s].RowCount - 1, 0, network.Weights[s].ColumnCount).PointwiseMultiply(network.Weights[s].SubMatrix(0, network.Weights[s].RowCount - 1, 0, network.Weights[s].ColumnCount)).ColumnSums().Sum();

                //        }
                //        else
                //        {
                //            iterationLoss += regularizationRate * network.Weights[s].PointwiseMultiply(network.Weights[s]).ColumnSums().Sum();

                //        }
                //    }

                // computing the validation loss:
                /*  double validationLoss = 0;

                  if (validationSplit != null)
                  {
                      for (int i = 0; i < valSplitSize; i++)
                      {
                          var nwOutput = network.ForwardPropagation(validation.Inputs.Row(i));
                          validationLoss += ((validation.Labels.Row(i) - nwOutput).PointwiseMultiply(validation.Labels.Row(i) - nwOutput)).Sum();

                      }
                      validationLoss /= valSplitSize;


                  }*/

                // computing the test loss:
                double validationError = 0;
                if (validationSet != null)
                {
                    for (int i = 0; i < test_indices.Count; i++)
                    {
                        var nwOutput = network.ForwardPropagation(test.Inputs.Row(i));
                        validationError += ((test.Labels.Row(i) - nwOutput).PointwiseMultiply(test.Labels.Row(i) - nwOutput)).Sum();

                    }
                    validationError /= test_indices.Count;


                }
                double trainingAccuracy = 0, validationSetAccuracy = 0;

                if (trueThreshold != null)
                {
                    trainingAccuracy = Utilities.Tools.ComputeAccuracy(network, trainingSet, trueThreshold);
                    validationSetAccuracy = Utilities.Tools.ComputeAccuracy(network, validationSet, trueThreshold);
                }


                learningCurve.Add(new double[] { iterationLoss, validationSet != null ? validationError : 0, trueThreshold != null ? trainingAccuracy : 0, trueThreshold != null ? validationSetAccuracy : 0 });
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Epoch:{0} loss:{1}", epoch, iterationLoss);
                Console.ResetColor();
            }
            return learningCurve;
        }



    }
}
