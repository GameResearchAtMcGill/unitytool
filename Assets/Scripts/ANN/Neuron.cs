//------------------------------------------------------------------------------
// <auto-generated>
//     이 코드는 도구를 사용하여 생성되었습니다.
//     런타임 버전:4.0.30319.34014
//
//     파일 내용을 변경하면 잘못된 동작이 발생할 수 있으며, 코드를 다시 생성하면
//     이러한 변경 내용이 손실됩니다.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;


namespace ANN
{
		[Serializable]
		public class Neuron
		{
		public List<Synapse> InputSynapses { get; set; }
		public List<Synapse> OutputSynapses { get; set; }
		public double Bias { get; set; }
		public double BiasDelta { get; set; }
		public double Gradient { get; set; }
		public double Value { get; set; }
		public bool sigmoid = false;
		
		public Neuron( )
		{
			InputSynapses = new List<Synapse>();
			OutputSynapses = new List<Synapse>();
			//Bias = NeuralNetwork.NextRandom();
		}
		
		
		public Neuron(List<Neuron> inputNeurons, bool _sigmoid) : this()
		{
			foreach (var inputNeuron in inputNeurons)
			{
				var synapse = new Synapse(inputNeuron, this);
				inputNeuron.OutputSynapses.Add(synapse);
				InputSynapses.Add(synapse);
			}
			if( _sigmoid )
				sigmoid = _sigmoid;
		
		}
		
		public virtual double CalculateValue(  )
		{
			if( sigmoid )
				return Value = NeuralNetwork.SigmoidFunction(InputSynapses.Sum(a => a.Weight * a.InputNeuron.Value) );//+ Bias);
			else
				return Value = NeuralNetwork.IdentityFunction(InputSynapses.Sum(a => a.Weight * a.InputNeuron.Value) );//+ Bias);
		}
		
		public virtual double CalculateDerivative(  )
		{
			if( sigmoid )
				return NeuralNetwork.SigmoidDerivative(Value);
			else 
				return NeuralNetwork.IdentityDerivative(Value);
			
		}
		
		public double CalculateError(double target)
		{
			return target - Value;
//			return Math.Pow( target - Value, 2 );
		}
		
		public double CalculateGradient(double target)
		{
			return Gradient = CalculateError(target) * (-1) * CalculateDerivative( );
		}
		
		public double CalculateGradient( )
		{
			return Gradient = OutputSynapses.Sum(a => a.OutputNeuron.Gradient * a.Weight) * CalculateDerivative( );
		}
		
		public void UpdateWeights(double learnRate, double momentum )
		{
			var prevDelta = BiasDelta;
			BiasDelta = learnRate * Gradient; // * 1
			Bias += BiasDelta + momentum * prevDelta;
		
			
			foreach (var s in InputSynapses)
			{
				prevDelta = s.WeightDelta;
				s.WeightDelta = (-1) * learnRate * Gradient * s.InputNeuron.Value;
				s.Weight += s.WeightDelta;// + momentum * prevDelta;
			}
			
		}
		}
}

