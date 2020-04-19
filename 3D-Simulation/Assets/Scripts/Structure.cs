﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

// TODO: Implement N point crossover

public class Structure: MonoBehaviour, IComparable<Structure>
{
    private int[] LAYER_SIZES = new int[] { 4, 7, 4 };
    private int GENOME_LENGTH = 71;
    private const string WEIGHTS_PATH = "Assets/Scripts/NN-Weights/";
    private string SAVE_PATH = WEIGHTS_PATH + DateTime.Now.Month + DateTime.Now.Day + DateTime.Now.Minute + DateTime.Now.Second + ".txt";

    private List<float> genome;
    private CarController car;
    private NeuralNetwork neuralNetwork;

    //Has access to car prefab and spawns it
    public Structure(List<float> _genome)
    {
        genome = _genome;

        // Instantiate the car
        GameObject instance = Resources.Load("HatchBack") as GameObject;
        car = (Instantiate(instance)).GetComponent<CarController>();
        GameObject startingLine = GameObject.Find("Starting Line");
        car.transform.position = startingLine.gameObject.transform.position;

        // Instantiate the Neural Network
        neuralNetwork = new NeuralNetwork(LAYER_SIZES);

        // If the genome is invalid, create a new random genome
        if (genome.Count != GENOME_LENGTH)
        {
            for (int i = 0; i < GENOME_LENGTH; i++)
            {
                genome.Add(UnityEngine.Random.Range(-0.5f, 0.5f));
            }
        }
    }

    public CarController GetCar()
    {
        return car;
    }

    public void Evaluate()
    {
        neuralNetwork.ConfigureNeuralNetwork(genome);
        car.SetNeuralNetwork(neuralNetwork);
    }

    public bool IsAlive()
    {
        if (car.hitWall)
        {
            return false;
        }

        return true;
    }

    public int GetFitness()
    {
        return car.GetFitness();
    }

    public int CompareTo(Structure other)
    {
        if (car.GetFitness() > other.car.GetFitness())
        {
            return 1;
        }
        else if (car.GetFitness() < other.car.GetFitness())
        {
            return -1;
        }
        else
        {
            return 0;
        }
    }

    //used as a simple mutation function for any genetic implementations.
    public void Mutate(int mutationRate, float mutationRadius)
    {
        for (int i = 0; i < genome.Count; i++)
        {
            genome[i] = (UnityEngine.Random.Range(0, 100) <= mutationRate) ? genome[i] += UnityEngine.Random.Range(-mutationRadius, mutationRadius) : genome[i];
        }
    }

    public List<float> deepCopyGenome()
    {
        List<float> genomeCopy = new List<float>();
        for (int i = 0; i < genome.Count; i++)
        {
            genomeCopy.Add(genome[i]);
        }

        return genomeCopy;
    }

    public void LoadGenomeFromFile(string fileName)//this loads the biases and weights from within a file into the neural network.
    {
        string filePath = WEIGHTS_PATH + fileName;

        TextReader tr = new StreamReader(filePath);
        int NumberOfLines = (int)new FileInfo(filePath).Length;
        string[] ListLines = new string[NumberOfLines];
        int index = 1;
        for (int i = 1; i < NumberOfLines; i++)
        {
            ListLines[i] = tr.ReadLine();
        }
        tr.Close();

        genome = new List<float>();
        if (new FileInfo(filePath).Length > 0)
        {
            for  (int i = 0; i < GENOME_LENGTH; i++)
            {
                genome.Add(float.Parse(ListLines[index]));
                index++;
            }
        }
    }

    public void SaveGenomeToFile()//this is used for saving the biases and weights within the network to a file.
    {
        File.Create(SAVE_PATH).Close();
        StreamWriter writer = new StreamWriter(SAVE_PATH, true);

        for (int i = 0; i < GENOME_LENGTH; i++)
        {
            writer.WriteLine(genome[i]);
        }
        
        writer.Close();
    }
}