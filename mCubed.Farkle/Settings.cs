using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace mCubed.Farkle
{
	public class Settings : INotifyPropertyChanged
	{
		#region Static Members

		private static Settings _instance;
		public static Settings Instance { get { return _instance = _instance ?? Serialization.InitializeAndLoad("mCubed.Farkle.xml", new Settings()); } }

		#endregion

		#region Properties

		private double _animationMilli = 2000;
		public double AnimationMilli
		{
			get { return _animationMilli; }
			set { if (value >= 0) _animationMilli = value; OnPropertyChanged("AnimationMilli"); }
		}
		private bool _showImages = true;
		public bool ShowImages
		{
			get { return _showImages; }
			set { _showImages = value; OnPropertyChanged("ShowImages"); }
		}
		private bool _thresholdEnabled = true;
		public bool ThresholdEnabled
		{
			get { return _thresholdEnabled; }
			set { _thresholdEnabled = value; OnPropertyChanged("ThresholdEnabled"); }
		}
		private int _threshold = 300;
		public int Threshold
		{
			get { return _threshold; }
			set { if (value >= 0) _threshold = value; OnPropertyChanged("Threshold"); }
		}
		private int _highScoreCount = 10;
		public int HighScoreCount
		{
			get { return _highScoreCount; }
			set { if (value > 0) _highScoreCount = value; HighScores = HighScores; OnPropertyChanged("HighScoreCount"); }
		}
		private IEnumerable<HighScore> _highScores = Enumerable.Empty<HighScore>();
		public IEnumerable<HighScore> HighScores
		{
			get { return _highScores; }
			set { _highScores = value.OrderByDescending(hs => hs.Score).ThenByDescending(hs => hs.Date).Take(HighScoreCount).ToList(); OnPropertyChanged("HighScores"); }
		}
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; OnPropertyChanged("Name"); }
		}

		#endregion

		#region Members

		public bool CanAddScore(int score)
		{
			var scores = HighScores;
			AddScore(score);
			try { return !HighScores.SequenceEqual(scores); }
			finally { HighScores = scores; }
		}

		public void AddScore(int score)
		{
			HighScores = HighScores.Concat(new[] { new HighScore { Name = Name, Score = score, Date = DateTime.Now } });
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
