using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Windows.Data;

namespace mCubed.Farkle
{
	public class HighScore : INotifyPropertyChanged
	{
		#region Properties

		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; OnPropertyChanged("Name"); }
		}
		private int _score;
		public int Score
		{
			get { return _score; }
			set { _score = value; OnPropertyChanged("Score"); }
		}
		private DateTime _date;
		public DateTime Date
		{
			get { return _date; }
			set { _date = value; OnPropertyChanged("Date"); }
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
