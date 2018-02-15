using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace mCubed.Farkle
{
	public enum DiceState { Scored, Saved, Roll };
	public class Dice : INotifyPropertyChanged
	{
		#region Static Members

		private static readonly Random RandomGenerator = new Random();
		private static readonly BitmapSource[] Images = new[]
		{
			Properties.Resources._1,
			Properties.Resources._2,
			Properties.Resources._3,
			Properties.Resources._4,
			Properties.Resources._5,
			Properties.Resources._6
		}.Select(b => Imaging.CreateBitmapSourceFromHBitmap(b.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(b.Width, b.Height))).ToArray();

		#endregion

		#region Properties

		private int _value;
		public int Value
		{
			get { return _value; }
			set { _value = value; OnPropertyChanged("Value", "Image"); }
		}
		private DiceState _state;
		public DiceState State
		{
			get { return _state; }
			set { _state = value; OnPropertyChanged("State"); }
		}
		private DiceGroup _group;
		public DiceGroup Group
		{
			get { return _group; }
			set { _group = value; OnPropertyChanged("Group"); }
		}
		private int _diceNumber;
		public int DiceNumber
		{
			get { return _diceNumber; }
			set { _diceNumber = value; OnPropertyChanged("DiceNumber"); }
		}
		public BitmapSource Image { get { return Images[Value - 1]; } }

		#endregion

		#region Constructor

		/// <summary>
		/// Create a new dice and roll it
		/// </summary>
		public Dice()
		{
			State = DiceState.Roll;
			Roll();
		}

		#endregion

		#region Members

		/// <summary>
		/// Roll the dice to generate a new value for the dice
		/// </summary>
		public void Roll()
		{
			Group = null;
			Value = RandomGenerator.Next(1, 7);
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
