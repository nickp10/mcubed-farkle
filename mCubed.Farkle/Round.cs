using System.ComponentModel;
using System.Linq;

namespace mCubed.Farkle
{
	public class Round : INotifyPropertyChanged
	{
		#region Properties

		public int RoundScore { get { return IsFarkle ? SaveScore : SaveScore + RiskScore; } }

		private int _riskScore;
		public int RiskScore
		{
			get { return _riskScore; }
			set { _riskScore = value; OnPropertyChanged("RiskScore", "RoundScore"); }
		}
		private int _saveScore;
		public int SaveScore
		{
			get { return _saveScore; }
			set { _saveScore = value; OnPropertyChanged("SaveScore", "RoundScore"); }
		}
		private int _roundNumber;
		public int RoundNumber
		{
			get { return _roundNumber; }
			set { _roundNumber = value; OnPropertyChanged("RoundNumber"); }
		}
		private bool _isFarkle;
		public bool IsFarkle
		{
			get { return _isFarkle; }
			set { _isFarkle = value; OnPropertyChanged("IsFarkle", "RoundScore"); }
		}
		private bool _isCurrentRound;
		public bool IsCurrentRound
		{
			get { return _isCurrentRound; }
			set { _isCurrentRound = value; OnPropertyChanged("IsCurrentRound"); }
		}
		private bool _isFinal;
		public bool IsFinal
		{
			get { return _isFinal; }
			set { _isFinal = value; OnPropertyChanged("IsFinal"); }
		}

		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(params string[] properties)
		{
			if (PropertyChanged != null)
				properties.ToList().ForEach(p => PropertyChanged(this, new PropertyChangedEventArgs(p)));
		}

		#endregion
	}
}
