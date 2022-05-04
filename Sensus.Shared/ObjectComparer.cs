using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Sensus
{
	public enum ComparisonOperators
	{
		Default,
		Equal,                    // IComparable comparisons
		NotEqual,                 // IComparable comparisons
		GreaterThan,              // IComparable comparisons
		GreaterThanOrEqual,       // IComparable comparisons
		LessThan,                 // IComparable comparisons
		LessThanOrEqual,          // IComparable comparisons
		Intersect,                // IEnumerable comparisons
		Disjoint,                 // IEnumerable comparisons
		ContainsAll,              // IEnumerable comparisons
		ContainsNone,             // IEnumerable comparisons
		AllContained,             // IEnumerable comparisons
		NoneContained,            // IEnumerable comparisons
		LengthEqual,              // IEnumerable comparisons
		LengthNotEqual,           // IEnumerable comparisons
		LengthGreaterThan,        // IEnumerable comparisons
		LengthGreaterThanOrEqual, // IEnumerable comparisons
		LengthLessThan,           // IEnumerable comparisons
		LengthLessThanOrEqual,    // IEnumerable comparisons

		StrictEqual,
		StrictNotEqual
	}

	public static class ObjectComparer
	{
		private static bool CompareIComparables(IComparable left, IComparable right, ComparisonOperators compareWith)
		{
			if (compareWith == ComparisonOperators.Default)
			{
				compareWith = ComparisonOperators.Equal;
			}

			if (left.GetType() == right.GetType())
			{
				int result = left.CompareTo(right);

				if (compareWith == ComparisonOperators.Equal && result == 0)
				{
					return true;
				}
				else if (compareWith == ComparisonOperators.NotEqual && result != 0)
				{
					return true;
				}
				else if (compareWith == ComparisonOperators.GreaterThanOrEqual && result >= 0)
				{
					return true;
				}
				else if (compareWith == ComparisonOperators.GreaterThan && result > 0)
				{
					return true;
				}
				else if (compareWith == ComparisonOperators.LessThanOrEqual && result <= 0)
				{
					return true;
				}
				else if (compareWith == ComparisonOperators.LessThan && result < 0)
				{
					return true;
				}
				else if (left is string leftString && right is string rightString)
				{
					if (compareWith == ComparisonOperators.Intersect)
					{
						return leftString.Intersect(rightString).Any();
					}
					else if (compareWith == ComparisonOperators.Disjoint)
					{
						return leftString.Intersect(rightString).Any() == false;
					}
					else if (compareWith == ComparisonOperators.ContainsAll)
					{
						return leftString.Contains(rightString);
					}
					else if (compareWith == ComparisonOperators.ContainsNone)
					{
						return leftString.Contains(rightString) == false;
					}
					else if (compareWith == ComparisonOperators.AllContained)
					{
						return rightString.Contains(leftString);
					}
					else if (compareWith == ComparisonOperators.NoneContained)
					{
						return rightString.Contains(leftString) == false;
					}
				}
			}
			else if (compareWith >= ComparisonOperators.LengthEqual && compareWith <= ComparisonOperators.LengthLessThanOrEqual)
			{
				if (left is string leftString && right is int rightLength)
				{
					return CompareIEnumerableAndIComparable(leftString, rightLength, compareWith);
				}
				else if (left is int leftLength && right is string rightString)
				{
					return CompareIComparableAndIEnumerable(leftLength, rightString, compareWith);
				}
			}
			else
			{
				TypeConverter leftConverter = TypeDescriptor.GetConverter(left.GetType());
				IComparable convertedLeft = left;
				IComparable convertedRight = right;

				if (leftConverter.CanConvertTo(right.GetType()))
				{
					convertedLeft = (IComparable)leftConverter.ConvertTo(left, right.GetType());
				}
				else if (leftConverter.CanConvertFrom(right.GetType()))
				{
					convertedRight = (IComparable)leftConverter.ConvertFrom(right);
				}
				else
				{
					TypeConverter rightConverter = TypeDescriptor.GetConverter(right.GetType());

					if (rightConverter.CanConvertTo(left.GetType()))
					{
						convertedRight = (IComparable)rightConverter.ConvertTo(right, left.GetType());
					}
					else if (rightConverter.CanConvertFrom(left.GetType()))
					{
						convertedLeft = (IComparable)rightConverter.ConvertFrom(left);
					}
				}

				return CompareIComparables(convertedLeft, convertedRight, compareWith);
			}

			return false;
		}
		private static bool CompareIEnumerables(IEnumerable left, IEnumerable right, ComparisonOperators compareWith)
		{
			IEnumerable<object> leftObjects = left.OfType<object>();
			IEnumerable<object> rightObjects = right.OfType<object>();
			IEnumerable<object> intersection = leftObjects.Intersect(rightObjects);
			int intersectionLength = intersection.Count();

			if (compareWith == ComparisonOperators.Default)
			{
				compareWith = ComparisonOperators.Intersect;
			}

			if (compareWith == ComparisonOperators.Equal)
			{
				return leftObjects.Count() == intersectionLength && rightObjects.Count() == intersectionLength;
			}
			else if (compareWith == ComparisonOperators.NotEqual)
			{
				return leftObjects.Count() != intersectionLength || rightObjects.Count() == intersectionLength;
			}
			else if (compareWith == ComparisonOperators.GreaterThan)
			{
				IComparable leftComparable = leftObjects.OfType<IComparable>().Min();
				IComparable rightComparable = rightObjects.OfType<IComparable>().Max();

				return leftComparable.CompareTo(rightComparable) > 0;
			}
			else if (compareWith == ComparisonOperators.GreaterThanOrEqual)
			{
				IComparable leftComparable = leftObjects.OfType<IComparable>().Min();
				IComparable rightComparable = rightObjects.OfType<IComparable>().Max();

				return leftComparable.CompareTo(rightComparable) >= 0;
			}
			else if (compareWith == ComparisonOperators.LessThan)
			{
				IComparable leftComparable = leftObjects.OfType<IComparable>().Max();
				IComparable rightComparable = rightObjects.OfType<IComparable>().Min();

				return leftComparable.CompareTo(rightComparable) < 0;
			}
			else if (compareWith == ComparisonOperators.LessThanOrEqual)
			{
				IComparable leftComparable = leftObjects.OfType<IComparable>().Max();
				IComparable rightComparable = rightObjects.OfType<IComparable>().Min();

				return leftComparable.CompareTo(rightComparable) <= 0;
			}
			else if (compareWith == ComparisonOperators.Intersect)
			{
				return intersectionLength > 0;
			}
			else if (compareWith == ComparisonOperators.Disjoint)
			{
				return intersectionLength == 0;
			}
			else if (compareWith == ComparisonOperators.ContainsAll)
			{
				return intersectionLength == rightObjects.Count();
			}
			else if (compareWith == ComparisonOperators.AllContained)
			{
				return intersectionLength == leftObjects.Count();
			}

			return false;
		}
		private static bool CompareIComparableAndIEnumerable(IComparable left, IEnumerable right, ComparisonOperators compareWith)
		{
			IEnumerable<object> rightObjects = right.OfType<object>();
			int rightLength = rightObjects.Count();

			if (compareWith == ComparisonOperators.LengthEqual)
			{
				return left is int length && length == rightLength;
			}
			else if (compareWith == ComparisonOperators.LengthNotEqual)
			{
				return left is int length && length != rightLength;
			}
			else if (compareWith == ComparisonOperators.LengthGreaterThan)
			{
				return left is int length && length > rightLength;
			}
			else if (compareWith == ComparisonOperators.LengthGreaterThanOrEqual)
			{
				return left is int length && length >= rightLength;
			}
			else if (compareWith == ComparisonOperators.LengthLessThan)
			{
				return left is int length && length < rightLength;
			}
			else if (compareWith == ComparisonOperators.LengthLessThanOrEqual)
			{
				return left is int length && length <= rightLength;
			}
			else
			{
				IEnumerable<object> leftEnumerable = new IComparable[] { left };

				return CompareIEnumerables(leftEnumerable, right, compareWith);
			}
		}
		private static bool CompareIEnumerableAndIComparable(IEnumerable left, IComparable right, ComparisonOperators compareWith)
		{
			IEnumerable<object> leftObjects = left.OfType<object>();
			int leftLength = leftObjects.Count();

			if (compareWith == ComparisonOperators.LengthEqual)
			{
				return right is int length && length == leftLength;
			}
			else if (compareWith == ComparisonOperators.LengthNotEqual)
			{
				return right is int length && length != leftLength;
			}
			else if (compareWith == ComparisonOperators.LengthGreaterThan)
			{
				return right is int length && length > leftLength;
			}
			else if (compareWith == ComparisonOperators.LengthGreaterThanOrEqual)
			{
				return right is int length && length >= leftLength;
			}
			else if (compareWith == ComparisonOperators.LengthLessThan)
			{
				return right is int length && length < leftLength;
			}
			else if (compareWith == ComparisonOperators.LengthLessThanOrEqual)
			{
				return right is int length && length <= leftLength;
			}
			else
			{
				IEnumerable<object> rightEnumerable = new IComparable[] { right };

				return CompareIEnumerables(left, rightEnumerable, compareWith);
			}
		}

		public static bool Compare(object left, object right, ComparisonOperators compareWith)
		{
			if (compareWith == ComparisonOperators.StrictEqual)
			{
				return Equals(left, right);
			}
			else if (compareWith == ComparisonOperators.StrictNotEqual)
			{
				return Equals(left, right) == false;
			}

			if (left == null || right == null)
			{
				if (left == null && right == null && compareWith == ComparisonOperators.Equal)
				{
					return true;
				}
				else if (compareWith == ComparisonOperators.NotEqual)
				{
					return true;
				}

				return false;
			}
			else if (left is IComparable leftComparable && right is IComparable rightComparable)
			{
				return CompareIComparables(leftComparable, rightComparable, compareWith);
			}
			else if (left is IComparable leftComparable2 && right is IEnumerable rightEnumerable2)
			{
				return CompareIComparableAndIEnumerable(leftComparable2, rightEnumerable2, compareWith);
			}
			else if (left is IEnumerable leftEnumerable2 && right is IComparable rightComparable2)
			{
				return CompareIEnumerableAndIComparable(leftEnumerable2, rightComparable2, compareWith);
			}
			else if (left is IEnumerable leftEnumerable && right is IEnumerable rightEnumerable)
			{
				return CompareIEnumerables(leftEnumerable, rightEnumerable, compareWith);
			}
			else if (compareWith == ComparisonOperators.Equal && left == right)
			{
				return true;
			}
			else if (compareWith == ComparisonOperators.NotEqual && left != right)
			{
				return true;
			}

			return true;
		}
	}
}
