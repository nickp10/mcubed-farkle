using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace mCubed.Farkle
{
	public delegate void StatusChangedEventHandler(FarkleStatus status);
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		#region Static Members

		public static readonly RoutedCommand RollDiceCommand = new RoutedCommand("RollDiceCommand", typeof(MainWindow));
		public static readonly RoutedCommand ScoreRoundCommand = new RoutedCommand("ScoreRoundCommand", typeof(MainWindow));
		public static readonly int DiceCount = 6;
		public static readonly int RoundCount = 10;
		public static readonly IEnumerable<DiceCheck> DiceChecks =
			new DiceCheck[] {
				new DiceCheck(DiceCollectionExtensions.CheckStraight),
				new DiceCheck(DiceCollectionExtensions.CheckThreePairs),
				new DiceCheck(DiceCollectionExtensions.CheckOfAKind),
				new DiceCheck(DiceCollectionExtensions.CheckSingleValue)
			};

		#endregion

		#region Properties

		public string StatusText
		{
			get
			{
				switch (Status)
				{
					case FarkleStatus.Farkle: return "You have farkled";
					case FarkleStatus.Roll: return "Roll the remaining dice or save your round";
					case FarkleStatus.SaveDice: return "Score at least one dice to continue";
					case FarkleStatus.ThresholdUnreached: return "Score more dice or roll the remaining dice";
					case FarkleStatus.UnscoredDice: return "All saved dice must score before continuing";
					case FarkleStatus.GameOver: return "Game over! Final score: " + TotalScore.ToString();
				}
				return "";
			}
		}
		private FarkleStatus _status;
		public FarkleStatus Status
		{
			get { return _status; }
			set
			{
				if (Status != value)
				{
					_status = value;
					OnStatusChanged();
					OnPropertyChanged("Status", "StatusText");
				}
			}
		}
		private int _currentRoundIndex;
		public int CurrentRoundIndex
		{
			get { return _currentRoundIndex; }
			set
			{
				if (CurrentRoundExists)
					CurrentRound.IsCurrentRound = false;
				_currentRoundIndex = value;
				if (CurrentRoundIndex < MainWindow.RoundCount)
					Rounds[CurrentRoundIndex].IsCurrentRound = true;
				OnPropertyChanged("CurrentRound");
			}
		}
		public bool CurrentRoundExists { get { return Rounds.Count(r => r.IsCurrentRound) == 1; } }
		public Round CurrentRound { get { return Rounds.Single(r => r.IsCurrentRound); } }
		public int TotalScore { get { return Rounds.Select(r => r.RoundScore).Sum(); } }
		public Round[] Rounds { get; private set; }
		public Dice[] DiceItems { get; private set; }
		public CollectionViewSource DiceCVS { get { return Resources["DiceCollection"] as CollectionViewSource; } }
		public event StatusChangedEventHandler StatusChanged;
		private int _prevRisk;
		private int _prevSave;

		#endregion

		#region Constructor

		public MainWindow()
		{
			// Load the scores
			Closing += delegate { Serialization.Save(); };
			StatusChanged += new StatusChangedEventHandler(OnStatusChanged);
			Loaded += delegate { ((Storyboard)Resources["FarkleAnimation"]).Completed += delegate { FarkleLabel.Visibility = Visibility.Collapsed; }; };

			// Start the game
			DiceItems = new Dice[MainWindow.DiceCount];
			Rounds = new Round[MainWindow.RoundCount];
			StartGame();
		}

		#endregion

		#region Members

		/// <summary>
		/// Start a new game
		/// </summary>
		private void StartGame()
		{
			// Create the dice
			for (int i = 0; i < DiceItems.Length; i++)
			{
				if (DiceItems[i] == null)
					DiceItems[i] = new Dice() { DiceNumber = i + 1 };
				DiceItems[i].Group = null;
				DiceItems[i].State = DiceState.Roll;
				DiceItems[i].Roll();
			}

			// Create the rounds
			for (int i = 0; i < Rounds.Length; i++)
			{
				if (Rounds[i] == null)
					Rounds[i] = new Round() { RoundNumber = i + 1 };
				Rounds[i].IsCurrentRound = false;
				Rounds[i].IsFarkle = false;
				Rounds[i].IsFinal = false;
				Rounds[i].RiskScore = Rounds[i].SaveScore = 0;
			}
			CurrentRoundIndex = 0;

			// Initialize
			_prevRisk = _prevSave = 0;
			if (!IsInitialized)
				InitializeComponent();
			OnPropertyChanged("TotalScore");
			Update();
		}

		/// <summary>
		/// Get the first dice group that qualifies for a sequence of dice
		/// </summary>
		/// <param name="dice">The dice to retrieve the dice group for</param>
		/// <returns>The dice group that best describes the given dice</returns>
		private DiceGroup GetDiceCheckGroup(IEnumerable<Dice> dice)
		{
			return MainWindow.DiceChecks.Select(dc => dc(dice)).OrderByDescending(d => d.Score).FirstOrDefault(d => d.IsValid);
		}

		/// <summary>
		/// Check if the current total score qualifies for the high scores
		/// </summary>
		private void CheckHighScore()
		{
			if (Settings.Instance.CanAddScore(TotalScore))
			{
				new PromptNameWindow { Owner = this }.ShowDialog();
				Settings.Instance.AddScore(TotalScore);
			}
		}

		/// <summary>
		/// Update the display of the dice and groups, as well as re-determining the status of the game
		/// </summary>
		private void Update()
		{
			// Refresh the view state of the application
			if (DiceCVS != null && DiceCVS.View != null)
				DiceCVS.View.Refresh();

			// Check if the game is over
			if (CurrentRoundIndex >= MainWindow.RoundCount)
				Status = FarkleStatus.GameOver;

			// Check if any saved dice do not score
			else if (DiceItems.Any(d => d.State == DiceState.Saved && d.Group == null))
				Status = FarkleStatus.UnscoredDice;

			// Check if farkle
			else if (!DiceItems.Any(d => d.State == DiceState.Saved) && GetDiceCheckGroup(DiceItems.Where(d => d.State == DiceState.Roll)) == null)
				Status = FarkleStatus.Farkle;

			// Check if dice must be scored before continuing on
			else if (!DiceItems.Any(d => d.State == DiceState.Saved))
				Status = FarkleStatus.SaveDice;

			// Check if the threshold has been reached
			else if (Settings.Instance.ThresholdEnabled && CurrentRound.RoundScore < Settings.Instance.Threshold)
				Status = FarkleStatus.ThresholdUnreached;

			// Let the user roll
			else
				Status = FarkleStatus.Roll;
		}

		/// <summary>
		/// Recalcuate the score of the saved dice
		/// </summary>
		private void RecalculateSavedDice()
		{
			// Reset the dice groups
			DiceItems.Where(d => d.State == DiceState.Saved).ToList().ForEach(d => d.Group = null);

			// Reset the scores
			CurrentRound.SaveScore -= _prevSave;
			CurrentRound.RiskScore -= _prevRisk;
			_prevRisk = _prevSave = 0;

			// Declare temporary variables
			DiceGroup dg = null;
			int count = 1;

			// While groups of dice exist, continue to group them
			while ((dg = GetDiceCheckGroup(DiceItems.Where(d => d.State == DiceState.Saved && d.Group == null))) != null)
			{
				// Apply the grouping and add the appropriate scores
				dg.Apply(count++);
				if (dg.IsScoreSaved) _prevSave += dg.Score;
				else _prevRisk += dg.Score;
			}

			// Reset the scores
			CurrentRound.SaveScore += _prevSave;
			CurrentRound.RiskScore += _prevRisk;
			Update();
		}

		#endregion

		#region Command Handlers

		/// <summary>
		/// The command handler that determines if the dice can be rolled
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event arguments</param>
		private void RollDice_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = Status == FarkleStatus.Roll || Status == FarkleStatus.ThresholdUnreached;
		}

		/// <summary>
		/// The command handler that rolls the dice
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event arguments</param>
		private void RollDice(object sender, ExecutedRoutedEventArgs e)
		{
			// Update round information
			_prevRisk = _prevSave = 0;

			// Score all saved dice
			DiceItems.Where(d => d.State == DiceState.Saved).ToList().ForEach(d => d.State = DiceState.Scored);

			// Check if all dice scored
			if (DiceItems.All(d => d.State == DiceState.Scored))
				DiceItems.ToList().ForEach(delegate(Dice d) { d.State = DiceState.Roll; d.Group = null; });

			// Roll all the roll dice
			DiceItems.Where(d => d.State == DiceState.Roll).ToList().ForEach(d => d.Roll());
			Update();
		}

		/// <summary>
		/// The command handler that determines if a round can be scored
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event arguments</param>
		private void ScoreTurn_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = Status == FarkleStatus.Farkle || Status == FarkleStatus.Roll;
		}

		/// <summary>
		/// The command handler that scores a round
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event arguments</param>
		private void ScoreTurn(object sender, ExecutedRoutedEventArgs e)
		{
			// Update round information
			CurrentRound.IsFarkle = Status == FarkleStatus.Farkle;
			CurrentRound.IsFinal = true;
			CurrentRoundIndex++;
			OnPropertyChanged("TotalScore");

			// Update the dice and roll them
			if (CurrentRoundExists)
			{
				DiceItems.ToList().ForEach(delegate(Dice d) { d.State = DiceState.Roll; d.Group = null; });
				RollDice(sender, e);
			}
			else
				Update();
		}

		#endregion

		#region Event Handlers

		/// <summary>
		/// The event handler that handles when a dice has been clicked
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event arguments</param>
		private void DiceBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// Get the dice that was clicked
			Border border = sender as Border;
			Dice dice = (border == null) ? null : border.DataContext as Dice;

			// Check if the dice exists and has not been scored
			if (dice != null && dice.State != DiceState.Scored)
			{
				// Alternate the dice the state between saved and roll
				dice.State = dice.State == DiceState.Saved ? DiceState.Roll : DiceState.Saved;
				dice.Group = dice.State == DiceState.Saved ? dice.Group : null;

				// Calculate the new score
				RecalculateSavedDice();
			}
		}

		/// <summary>
		/// The event handler that handles when a new game should be started
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event arguments</param>
		private void NewGameClick(object sender, RoutedEventArgs e)
		{
			if (Status != FarkleStatus.GameOver)
				CheckHighScore();
			StartGame();
		}

		/// <summary>
		/// The event handler that handles when the settings should be shown
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event arguments</param>
		private void SettingsClick(object sender, RoutedEventArgs e)
		{
			new SettingsWindow { Owner = this }.ShowDialog();
		}

		/// <summary>
		/// The event handler that handles when the high scores should be shown
		/// </summary>
		/// <param name="sender">The sender object</param>
		/// <param name="e">The event arguments</param>
		private void ShowHighScores_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			new HighScoresWindow { Owner = this }.ShowDialog();
		}

		/// <summary>
		/// The event handler that notifies that the status of the game changed
		/// </summary>
		private void OnStatusChanged()
		{
			if (StatusChanged != null)
				StatusChanged(Status);
		}

		/// <summary>
		/// The event handler that handles when the status of the game changes
		/// </summary>
		/// <param name="status">The new status of the game</param>
		private void OnStatusChanged(FarkleStatus status)
		{
			if (status == FarkleStatus.GameOver)
				CheckHighScore();
			else if (status == FarkleStatus.Farkle && Settings.Instance.AnimationMilli > 0)
			{
				FarkleLabel.Visibility = Visibility.Visible;
				((Storyboard)Resources["FarkleAnimation"]).Begin();
			}
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
