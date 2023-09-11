using System;
using TLP.UdonUtils.Sync.SyncedEvents;
using UdonSharp;
using VRC.SDKBase;

namespace TLP.UdonUtils.Player
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PlayerList : SyncedEventIntArray
    {
        public int[] Players
        {
            get => Values;
            set => Values = value;
        }

        #region public

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <returns>true if player was added and false if already in the list</returns>
        public bool AddPlayer(VRCPlayerApi player)
        {
            int validPlayers = ConsolidatePlayerIds(Players);

            if (!Assert(Utilities.IsValid(player), "Player invalid", this))
            {
                ResizePlayerArray(validPlayers);
                return false;
            }

            if (Players == null || Players.Length == 0)
            {
                Players = new[]
                {
                    player.playerId
                };
                return true;
            }

            if (Array.BinarySearch(Players, player.playerId) >= 0)
            {
                Warn($"Player {player.playerId} already in list");
                ResizePlayerArray(validPlayers);
                return false;
            }

            int[] tempArray = new int[validPlayers + 1];
            tempArray[0] = player.playerId;
            Array.ConstrainedCopy(Players, 0, tempArray, 1, validPlayers);
            Players = tempArray;
            Sort(Players);
            return true;
        }

        public bool RemovePlayer(VRCPlayerApi player)
        {
            int count = ConsolidatePlayerIds(Players);

            if (!Utilities.IsValid(player)
                || DiscardInvalid() == 0)
            {
                ResizePlayerArray(count);
                return false;
            }

            int playerIndex = Array.BinarySearch(Players, player.playerId);
            if (playerIndex < 0)
            {
                ResizePlayerArray(count);
                return false;
            }

            int[] tempArray = new int[count - 1];

            if (playerIndex > 0)
            {
                Array.ConstrainedCopy(Players, 0, tempArray, 0, playerIndex);
            }

            if (tempArray.Length - playerIndex > 0)
            {
                Array.ConstrainedCopy(Players, playerIndex + 1, tempArray, playerIndex, tempArray.Length - playerIndex);
            }

            Players = tempArray;
            return true;
        }

        public bool Contains(VRCPlayerApi playerApi)
        {
#if TLP_DEBUG
            DebugLog(nameof(Contains));
#endif
            if (!Utilities.IsValid(playerApi)
                || DiscardInvalid() == 0)
            {
                return false;
            }

            return Array.BinarySearch(Players, playerApi.playerId) > -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>number of valid players in the list after disposing of all invalid ids</returns>
        public int DiscardInvalid()
        {
            #region TLP_DEBUG

#if TLP_DEBUG
            DebugLog(nameof(DiscardInvalid));
#endif

            #endregion

            int validPlayers = ConsolidatePlayerIds(Players);
            ResizePlayerArray(validPlayers);
            return validPlayers;
        }

        public void Clear()
        {
#if TLP_DEBUG
            DebugLog(nameof(Clear));
#endif
            Players = new int[0];
        }

        #endregion

        #region Internal

        internal bool ResizePlayerArray(int length)
        {
            if (length < 0)
            {
                return false;
            }

            if (Players == null || Players.Length == 0)
            {
                Players = new int[length];
                for (int i = 0; i < Players.Length; i++)
                {
                    Players[i] = int.MaxValue;
                }

                return true;
            }

            int copyLength = length > Players.Length ? Players.Length : length;
            int[] temp = new int[length];
            Array.ConstrainedCopy(Players, 0, temp, 0, copyLength);
            Players = temp;
            for (int i = copyLength; i < Players.Length; i++)
            {
                Players[i] = int.MaxValue;
            }

            return true;
        }

        internal void Sort(int[] array)
        {
            if (array == null || array.Length < 2)
            {
                return;
            }

            // BubbleSort(array);
            // IterativeQuickSort(array, 0, array.Length - 1);
            //MergeSort(array);
            HeapSort(array);
        }

        #region BubbleSort

        internal void BubbleSort(int[] array)
        {
            int arrayLength = array.Length;
            for (int i = 0; i < arrayLength; i++)
            {
                for (int j = 0; j < arrayLength - 1; j++)
                {
                    int next = j + 1;

                    if (array[j] > array[next])
                    {
                        int tmp = array[j];
                        array[j] = array[next];
                        array[next] = tmp;
                    }
                }
            }
        }

        #endregion

        #region Heap Sort

        public static void HeapSort(int[] arr)
        {
            int n = arr.Length;

            // Build heap (rearrange array)
            for (int i = n / 2 - 1; i >= 0; i--)
            {
                Heapify(arr, n, i);
            }

            // One by one extract an element from heap
            for (int i = n - 1; i >= 0; i--)
            {
                // Move current root to end
                Swap(arr, 0, i);

                // call max heapify on the reduced heap
                Heapify(arr, i, 0);
            }
        }

        [RecursiveMethod]
        private static void Heapify(int[] arr, int n, int i)
        {
            int largest = i; // Initialize largest as root
            int l = 2 * i + 1; // left = 2*i + 1
            int r = 2 * i + 2; // right = 2*i + 2

            // If left child is larger than root
            if (l < n && arr[l] > arr[largest])
            {
                largest = l;
            }

            // If right child is larger than largest so far
            if (r < n && arr[r] > arr[largest])
            {
                largest = r;
            }

            // If largest is not root
            if (largest != i)
            {
                HsSwap(arr, i, largest);

                // Recursively heapify the affected sub-tree
                Heapify(arr, n, largest);
            }
        }

        private static void HsSwap(int[] arr, int i, int j)
        {
            int temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }

        #endregion


        #region Merge Sort

        public static void MergeSort(int[] array)
        {
            int n = array.Length;
            int[] tempArray = new int[n];

            for (int size = 1; size < n; size *= 2)
            {
                for (int left = 0; left < n - size; left += 2 * size)
                {
                    int mid = left + size - 1;
                    int right = Math.Min(left + 2 * size - 1, n - 1);

                    Merge(array, tempArray, left, mid, right);
                }
            }
        }

        private static void Merge(int[] array, int[] tempArray, int left, int mid, int right)
        {
            int i = left;
            int j = mid + 1;
            int k = 0;

            while (i <= mid && j <= right)
            {
                if (array[i] < array[j])
                {
                    tempArray[k++] = array[i++];
                }
                else
                {
                    tempArray[k++] = array[j++];
                }
            }

            while (i <= mid)
            {
                tempArray[k++] = array[i++];
            }

            while (j <= right)
            {
                tempArray[k++] = array[j++];
            }

            for (i = left, k = 0; i <= right; i++, k++)
            {
                array[i] = tempArray[k];
            }
        }

        #endregion

        #region Iterative QuickSort

        private static void IterativeQuickSort(int[] arr, int left, int right)
        {
            int[] stack = new int[right - left + 1];
            int top = -1;

            stack[++top] = left;
            stack[++top] = right;

            while (top >= 0)
            {
                right = stack[top--];
                left = stack[top--];

                int pivotIndex = Partition(arr, left, right);

                if (pivotIndex - 1 > left)
                {
                    stack[++top] = left;
                    stack[++top] = pivotIndex - 1;
                }

                if (pivotIndex + 1 < right)
                {
                    stack[++top] = pivotIndex + 1;
                    stack[++top] = right;
                }
            }
        }

        private static int Partition(int[] arr, int left, int right)
        {
            int pivot = arr[right];
            int i = left - 1;

            for (int j = left; j <= right - 1; j++)
            {
                if (arr[j] < pivot)
                {
                    i++;
                    Swap(arr, i, j);
                }
            }

            Swap(arr, i + 1, right);

            return i + 1;
        }

        private static void Swap(int[] arr, int i, int j)
        {
            int temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;
        }

        #endregion


        internal int ConsolidatePlayerIds(int[] list)
        {
            if (list == null)
            {
                return 0;
            }

            int valid = 0;
            int moveIndex = -1;
            for (int i = 0; i < list.Length; i++)
            {
                var vrcPlayerApi = VRCPlayerApi.GetPlayerById(list[i]);
                if (Utilities.IsValid(vrcPlayerApi))
                {
                    ++valid;
                    if (moveIndex != -1)
                    {
                        list[moveIndex] = list[i];
                        list[i] = int.MaxValue;
                        ++moveIndex;
                    }
                }
                else
                {
                    // ensure that the entry no longer references a valid player
                    list[i] = int.MaxValue;
                    if (moveIndex == -1)
                    {
                        moveIndex = i;
                    }
                }
            }

            return valid;
        }

        #endregion

       
    }
}