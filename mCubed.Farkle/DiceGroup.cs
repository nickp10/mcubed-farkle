using System;
using System.Collections.Generic;
using System.Linq;

namespace mCubed.Farkle
{
	public delegate DiceGroup DiceCheck(IEnumerable<Dice> dice);
	public enum DiceGroups { Straight, ThreePairs, SixOfAKind, FiveOfAKind, FourOfAKind, ThreeOfAKind, SingleAce, SingleFive };
	public enum FarkleStatus { Farkle, Roll, SaveDice, ThresholdUnreached, UnscoredDice, GameOver };
	public class DiceGroup : IComparable
	{
		#region Properties

		public DiceGroups Group { get; set; }
		public int GroupNumber { get; set; }
		public bool IsValid { get; set; }
		public bool IsScoreSaved { get { return IsValid && (Group == DiceGroups.SixOfAKind || Group == DiceGroups.Straight); } }
		public int Score { get { return GetScore(); } }
		public string ScoreText { get { return GetScoreText(); } }
		public IEnumerable<Dice> GroupDice { get; set; }

		#endregion

		#region Members

		private int FaceValue()
		{
			return (GroupDice.First().Value == 1) ? 10 : GroupDice.First().Value;
		}

		private int GetScore()
		{
			if (IsValid)
			{
				switch (Group)
				{
					case DiceGroups.Straight: return 1500;
					case DiceGroups.ThreePairs: return 750;
					case DiceGroups.SixOfAKind: return (100 * FaceValue()) * 4;
					case DiceGroups.FiveOfAKind: return (100 * FaceValue()) * 3;
					case DiceGroups.FourOfAKind: return (100 * FaceValue()) * 2;
					case DiceGroups.ThreeOfAKind: return (100 * FaceValue());
					case DiceGroups.SingleAce: return 100;
					case DiceGroups.SingleFive: return 50;
				}
			}
			return 0;
		}

		private string GetScoreText()
		{
			if (IsValid)
			{
				switch (Group)
				{
					case DiceGroups.Straight: return "Straight";
					case DiceGroups.ThreePairs: return "Three Pairs";
					case DiceGroups.SixOfAKind: return "Six of a Kind";
					case DiceGroups.FiveOfAKind: return "Five of a Kind";
					case DiceGroups.FourOfAKind: return "Four of a Kind";
					case DiceGroups.ThreeOfAKind: return "Three of a Kind";
					case DiceGroups.SingleAce: return "Single ace";
					case DiceGroups.SingleFive: return "Single five";
				}
			}
			return string.Empty;
		}

		public void Apply(int groupNumber)
		{
			if (IsValid)
			{
				GroupNumber = groupNumber;
				GroupDice = GroupDice.ToArray();
				foreach (Dice d in GroupDice)
					d.Group = this;
			}
		}
		public override string ToString()
		{
			return ScoreText;
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object obj)
		{
			DiceGroup dg = obj as DiceGroup;
			if (dg == null) return 1;
			int compare = Group.CompareTo(dg.Group);
			return (compare == 0) ? GroupNumber.CompareTo(dg.GroupNumber) : compare;
		}

		#endregion
	}

	public static class DiceCollectionExtensions
	{
		public static bool IsSequence(this IEnumerable<int> items, int count)
		{
			if (items.Count() != count) return false;
			int prev = items.Min();
			foreach (int item in items.OrderBy(i => i))
				if (item != prev++) return false;
			return true;
		}

		public static DiceGroup CheckAnyOfAValue(this IEnumerable<Dice> dice, int value, int count, DiceGroups diceGroup)
		{
			return new DiceGroup() { Group = diceGroup, GroupDice = dice.Where(d => d.Value == value).Take(count), IsValid = dice.Count(d => d.Value == value) >= count };
		}

		public static DiceGroup CheckStraight(this IEnumerable<Dice> dice)
		{
			return new DiceGroup() { Group = DiceGroups.Straight, GroupDice = dice, IsValid = dice.Select(d => d.Value).IsSequence(MainWindow.DiceCount) };
		}

		public static DiceGroup CheckThreePairs(this IEnumerable<Dice> dice)
		{
			return new DiceGroup() { Group = DiceGroups.ThreePairs, GroupDice = dice, IsValid = dice.Count() == MainWindow.DiceCount && dice.GroupBy(d => d.Value).All(g => g.Count() == 2) };
		}

		public static DiceGroup CheckOfAKind(this IEnumerable<Dice> dice, int kind, DiceGroups diceGroup)
		{
			IEnumerable<Dice> maxGroup = dice.GroupBy(d => d.Value).OrderByDescending(g => g.Count()).FirstOrDefault();
			return new DiceGroup() { Group = diceGroup, GroupDice = maxGroup, IsValid = maxGroup != null && maxGroup.Count() >= kind };
		}

		public static DiceGroup CheckOfAKind(this IEnumerable<Dice> dice)
		{
			DiceGroup dg = CheckOfAKind(dice, 6, DiceGroups.SixOfAKind);
			if (dg.IsValid) return dg;
			dg = CheckOfAKind(dice, 5, DiceGroups.FiveOfAKind);
			if (dg.IsValid) return dg;
			dg = CheckOfAKind(dice, 4, DiceGroups.FourOfAKind);
			if (dg.IsValid) return dg;
			return CheckOfAKind(dice, 3, DiceGroups.ThreeOfAKind);
		}

		public static DiceGroup CheckSingleValue(this IEnumerable<Dice> dice)
		{
			DiceGroup dg = CheckAnyOfAValue(dice, 1, 1, DiceGroups.SingleAce);
			return dg.IsValid ? dg : CheckAnyOfAValue(dice, 5, 1, DiceGroups.SingleFive);
		}
	}
}
