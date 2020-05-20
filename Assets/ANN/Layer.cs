using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*Code inspired by Dr Penny dy Byl's Artifical Neural Network from:
https://www.udemy.com/course/machine-learning-with-unity/learn/lecture/13882694#overview
*/

public class Layer {

	public int numNeurons;
	public List<Neuron> neurons = new List<Neuron>();

	public Layer(int nNeurons, int numNeuronInputs)
	{
		//Creates a layer with the number of Nuerons specified. This is done at the start of the program.
		numNeurons = nNeurons;
		for(int i = 0; i < nNeurons; i++)
		{
			neurons.Add(new Neuron(numNeuronInputs));
		}
	}
}
