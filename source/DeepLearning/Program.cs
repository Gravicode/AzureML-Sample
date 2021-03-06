﻿using Accord.Neuro;
using Accord.Neuro.ActivationFunctions;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using Accord.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Neuro.Learning;
using System.IO;

namespace DeepLearning
{
    class Program
    {
        static void Main(string[] args)
        {
            double[][] inputs;
            double[][] outputs;
            double[][] testInputs;
            double[][] testOutputs;

            const int SampleTrainingCount = 120;
            const int SampleTestCount = 30;

            // Load ascii digits dataset.
            inputs = DataManager.LoadCSV(@"../../../data/iris.data", out outputs);
            //inputs = DataManager.Load(@"../../../data/data.txt", out outputs);

            // The first SampleTrainingCount data rows will be for training. The rest will be for testing.
            testInputs = inputs.Skip(SampleTrainingCount).ToArray();
            testOutputs = outputs.Skip(SampleTrainingCount).ToArray();
            inputs = inputs.Take(SampleTrainingCount).ToArray();
            outputs = outputs.Take(SampleTrainingCount).ToArray();

            // Setup the deep belief network and initialize with random weights.
            DeepBeliefNetwork network = new DeepBeliefNetwork(inputs.First().Length, 10, 1);
            new GaussianWeights(network, 0.1).Randomize();
            network.UpdateVisibleWeights();
            
            // Setup the learning algorithm.
            DeepBeliefNetworkLearning teacher = new DeepBeliefNetworkLearning(network)
            {
                Algorithm = (h, v, i) => new ContrastiveDivergenceLearning(h, v)
                {
                    LearningRate = 0.1,
                    Momentum = 0.5,
                    Decay = 0.001,
                }
            };

            // Setup batches of input for learning.
            int batchCount = Math.Max(1, inputs.Length / 100);
            // Create mini-batches to speed learning.
            int[] groups = Accord.Statistics.Tools.RandomGroups(inputs.Length, batchCount);
            double[][][] batches = inputs.Subgroups(groups);
            // Learning data for the specified layer.
            double[][][] layerData;

            // Unsupervised learning on each hidden layer, except for the output layer.
            for (int layerIndex = 0; layerIndex < network.Machines.Count - 1; layerIndex++)
            {
                teacher.LayerIndex = layerIndex;
                layerData = teacher.GetLayerInput(batches);
                for (int i = 0; i < 200; i++)
                {
                    double error = teacher.RunEpoch(layerData) / inputs.Length;
                    if (i % 10 == 0)
                    {
                        Console.WriteLine(i + ", Error = " + error);
                    }
                }
            }

            // Supervised learning on entire network, to provide output classification.
            var teacher2 = new BackPropagationLearning(network)
            {
                LearningRate = 0.1,
                Momentum = 0.5
            };

            // Run supervised learning.
            for (int i = 0; i < SampleTrainingCount; i++)
            {
                double error = teacher2.RunEpoch(inputs, outputs) / inputs.Length;
                if (i % 10 == 0)
                {
                    Console.WriteLine(i + ", Error = " + error);
                }
            }

            // Test the resulting accuracy. SampleTestCount item
            int correct = 0;
            for (int i = 0; i < SampleTestCount; i++)
            {
                double[] outputValues = network.Compute(testInputs[i]);
                if (DataManager.FormatOutputResult(outputValues) == DataManager.FormatOutputResult(testOutputs[i]))
                {
                    correct++;
                }
            }

            Console.WriteLine("Correct " + correct + "/" + SampleTestCount + ", " + Math.Round(((double)correct / (double)SampleTestCount * 100), 2) + "%");
            Console.Write("Press any key to quit ..");
            Console.ReadKey();
        }
    }
}
