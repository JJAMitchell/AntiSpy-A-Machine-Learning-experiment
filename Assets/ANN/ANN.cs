using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*Code inspired by Dr Penny dy Byl's Artifical Neural Network from:
https://www.udemy.com/course/machine-learning-with-unity/learn/lecture/13882694#overview
*/

public class ANN{

	public int numInputs;
	public int numOutputs;
	public int numHidden;
	public int numNPerHidden;
	public double alpha;
	List<Layer> layers = new List<Layer>();

	//Public function to set up the ANN with number of inputs, outputs, hidden layers and Nuerons per hidden
	public ANN(int nI, int nO, int nH, int nPH, double a)
	{
		numInputs = nI;
		numOutputs = nO;
		numHidden = nH;
		numNPerHidden = nPH;
		alpha = a;

		//If the number of hidden layers are more then zero, sets up each hidden layer with the specified number of Nuerons
		//Else just have the input layer and output layer.
		if(numHidden > 0)
		{
			layers.Add(new Layer(numNPerHidden, numInputs));

			for(int i = 0; i < numHidden-1; i++)
			{
				layers.Add(new Layer(numNPerHidden, numNPerHidden));
			}

			layers.Add(new Layer(numOutputs, numNPerHidden));
		}
		else
		{
			layers.Add(new Layer(numOutputs, numInputs));
		}
	}

	//Training function that gets the value from calcOutput and uses the returned value to update the weights.
	public List<double> Train(List<double> inputValues, List<double> desiredOutput)
	{
		List<double> outputValues = new List<double>();
		outputValues = CalcOutput(inputValues, desiredOutput);
		UpdateWeights(outputValues, desiredOutput);
		return outputValues;
	}

	//Calcout function handles the calculations from the input values and the desiered output
	public List<double> CalcOutput(List<double> inputValues, List<double> desiredOutput)
	{
		List<double> inputs = new List<double>();
		List<double> outputValues = new List<double>();
		int currentInput = 0;

		if(inputValues.Count != numInputs)
		{
			Debug.Log("ERROR: Number of Inputs must be " + numInputs);
			return outputValues;
		}

		inputs = new List<double>(inputValues);
		for(int i = 0; i < numHidden + 1; i++)
		{
				if(i > 0)
				{
					inputs = new List<double>(outputValues);
				}
				outputValues.Clear();

				for(int j = 0; j < layers[i].numNeurons; j++)
				{
					double N = 0;
					layers[i].neurons[j].inputs.Clear();

					for(int k = 0; k < layers[i].neurons[j].numInputs; k++)
					{
					    layers[i].neurons[j].inputs.Add(inputs[currentInput]);
						N += layers[i].neurons[j].weights[k] * inputs[currentInput];
						currentInput++;
					}

					N -= layers[i].neurons[j].bias;

					if(i == numHidden)
						layers[i].neurons[j].output = ActivationFunctionO(N);
					else
						layers[i].neurons[j].output = ActivationFunction(N);
					
					outputValues.Add(layers[i].neurons[j].output);
					currentInput = 0;
				}
		}
		return outputValues;
	}

	//Same as the first calcoutput however this calculation uses just the input values.
	public List<double> CalcOutput(List<double> inputValues)
	{
		List<double> inputs = new List<double>();
		List<double> outputValues = new List<double>();
		int currentInput = 0;

		if(inputValues.Count != numInputs)
		{
			Debug.Log("ERROR: Number of Inputs must be " + numInputs);
			return outputValues;
		}

		inputs = new List<double>(inputValues);
		for(int i = 0; i < numHidden + 1; i++)
		{
				if(i > 0)
				{
					inputs = new List<double>(outputValues);
				}
				outputValues.Clear();

				for(int j = 0; j < layers[i].numNeurons; j++)
				{
					double N = 0;
					layers[i].neurons[j].inputs.Clear();

					for(int k = 0; k < layers[i].neurons[j].numInputs; k++)
					{
					    layers[i].neurons[j].inputs.Add(inputs[currentInput]);
						N += layers[i].neurons[j].weights[k] * inputs[currentInput];
						currentInput++;
					}

					N -= layers[i].neurons[j].bias;

					if(i == numHidden)
						layers[i].neurons[j].output = ActivationFunctionO(N);
					else
						layers[i].neurons[j].output = ActivationFunction(N);
					
					outputValues.Add(layers[i].neurons[j].output);
					currentInput = 0;
				}
		}
		return outputValues;
	}


	//Prints all current weights into a string.
	public string PrintWeights()
	{
		string weightStr = "";
		foreach(Layer l in layers)
		{
			foreach(Neuron n in l.neurons)
			{
				foreach(double w in n.weights)
				{
					weightStr += w + ",";
				}
				weightStr += n.bias + ",";
			}
		}
		return weightStr;
	}

	//Takes a string, if the layout is correct then it will load in each value as a weight.
	public void LoadWeights(string weightStr)
	{
		if(weightStr == "") return;
		string[] weightValues = weightStr.Split(',');
		int w = 0;
		foreach(Layer l in layers)
		{
			foreach(Neuron n in l.neurons)
			{
				for(int i = 0; i < n.weights.Count; i++)
				{
					n.weights[i] = System.Convert.ToDouble(weightValues[w]);
					w++;
				}
				n.bias = System.Convert.ToDouble(weightValues[w]);
				w++;
			}
		}
	}

	// Private function that updates the weights of each nueron based on what the calc output returned as.
	void UpdateWeights(List<double> outputs, List<double> desiredOutput)
	{
		double error;
		for(int i = numHidden; i >= 0; i--)
		{
			for(int j = 0; j < layers[i].numNeurons; j++)
			{
				if(i == numHidden)
				{
					error = desiredOutput[j] - outputs[j];
					layers[i].neurons[j].errorGradient = outputs[j] * (1-outputs[j]) * error;
				}
				else
				{
					layers[i].neurons[j].errorGradient = layers[i].neurons[j].output * (1-layers[i].neurons[j].output);
					double errorGradSum = 0;
					for(int p = 0; p < layers[i+1].numNeurons; p++)
					{
						errorGradSum += layers[i+1].neurons[p].errorGradient * layers[i+1].neurons[p].weights[j];
					}
					layers[i].neurons[j].errorGradient *= errorGradSum;
				}	
				for(int k = 0; k < layers[i].neurons[j].numInputs; k++)
				{
					if(i == numHidden)
					{
						error = desiredOutput[j] - outputs[j];
						layers[i].neurons[j].weights[k] += alpha * layers[i].neurons[j].inputs[k] * error;
					}
					else
					{
						layers[i].neurons[j].weights[k] += alpha * layers[i].neurons[j].inputs[k] * layers[i].neurons[j].errorGradient;
					}
				}
				layers[i].neurons[j].bias += alpha * -1 * layers[i].neurons[j].errorGradient;
			}

		}

	}


	//The activation function for the hidden layers
	double ActivationFunction(double value)
	{
		return TanH(value);
	}

	//Activation function for the output layer
	double ActivationFunctionO(double value)
	{
		return Sigmoid(value);
	}

	//Between -1 and 1
	double TanH(double value)
	{
		double k = (double) System.Math.Exp(-2*value);
    	return 2 / (1.0f + k) - 1;
	}

	//Above 0, else return 0
	double ReLu(double value)
	{
		if(value > 0) return value;
		else return 0;
	}

	//Returns the exact value
	double Linear(double value)
	{
		return value;
	}

	//If value is below 0, add a slight gradient, else return value
	double LeakyReLu(double value)
	{
		if(value < 0) return 0.01*value;
   		else return value;
	}

	//Between 0 and 1
	double Sigmoid(double value) 
	{
    	double k = (double) System.Math.Exp(value);
    	return k / (1.0f + k);
	}
}
