namespace Shinobytes.Linq
{
    public static partial class LinqExtensions
    {
        public abstract class EnumerableSorter<TElement>
        {
            public abstract void ComputeKeys(TElement[] elements, int count);

            public abstract int CompareKeys(int index1, int index2);

            public int[] Sort(TElement[] elements, int count)
            {
                ComputeKeys(elements, count);
                int[] map = new int[count];
                for (int i = 0; i < count; i++) map[i] = i;
                QuickSort(map, 0, count - 1);
                return map;
            }

            void QuickSort(int[] map, int left, int right)
            {
                do
                {
                    int i = left;
                    int j = right;
                    int x = map[i + ((j - i) >> 1)];
                    do
                    {
                        while (i < map.Length && CompareKeys(x, map[i]) > 0) i++;
                        while (j >= 0 && CompareKeys(x, map[j]) < 0) j--;
                        if (i > j) break;
                        if (i < j)
                        {
                            int temp = map[i];
                            map[i] = map[j];
                            map[j] = temp;
                        }
                        i++;
                        j--;
                    } while (i <= j);
                    if (j - left <= right - i)
                    {
                        if (left < j) QuickSort(map, left, j);
                        left = i;
                    }
                    else
                    {
                        if (i < right) QuickSort(map, i, right);
                        right = j;
                    }
                } while (left < right);
            }
        }

    }
}