using System.Collections.Generic;
using UnityEngine;

namespace UnityRecyclingFlexListView
{
    public class TestFlexChildData : ChildData
    {
        public string Title;
        public string Note1;
        public string Note2;

        public TestFlexChildData(string t, string n1, string n2)
        {
            Title = t;
            Note1 = n1;
            Note2 = n2;
        }

        public override string FlexibleItem() => Title;
    }


    public class TestFlexPanel : MonoBehaviour
    {
        [SerializeField]
        private RecyclingFlexListView theList;
        private List<TestFlexChildData> _data = new List<TestFlexChildData>();
        private int _itemCount = 50;
        private string[] randomTitles = new[]
        {
            "Hello World ",
            "Lorem ipsum dolor sit amet, \nconsectetur adipiscing elit, ",
            "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. \nUt enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. \n",
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. \nExcepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.\n"
        };
        private string _rowToJump;
        private string _rowToModify = "0";
        private string _modifyRangeStart;
        private string _modifyItemCount;

        private void Start()
        {
            theList.ItemCallback = PopulateItem;
            InitializeList();
            theList.Refresh();
        }

        private void InitializeList()
        {
            _data.Clear();

            for (int i = 0; i < _itemCount; ++i)
            {
                var newdata =
                    new TestFlexChildData(
                        randomTitles[Random.Range(0, randomTitles.Length)], $"Row {i}", Random.Range(0, 256).ToString());
                _data.Add(newdata);
                theList.AddItem(newdata);
            }
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Clear"))
            {
                CLear();
            }
    
            if (GUILayout.Button("Add"))
            {
                Add();
            }
    
            if (_itemCount > 0)
            {
                Delete();
            }
    
            GUILayout.Label("Input row index to jump");
            _rowToJump = GUILayout.TextField(_rowToJump);
            if (GUILayout.Button($"Jump To:{_rowToJump}"))
            {
                JumpTo(int.Parse(_rowToJump));
            }

            // if (GUILayout.Button("Refresh All"))
            // {
            //     RefreshAll();
            // }
    
            GUILayout.Label("Input row index to change value");
            _rowToModify = GUILayout.TextField(_rowToModify);
            if (GUILayout.Button($"Refresh item at:{_rowToModify}"))
            {
                var row = int.Parse(_rowToModify);
                Refresh(row);
            }
    
            // GUILayout.Label("Input row index and range to change value");
            // modifyRangeStart = GUILayout.TextField(modifyRangeStart);
            // modifyItemCount = GUILayout.TextField(modifyItemCount);
            // if (GUILayout.Button($"Refresh item from:{modifyRangeStart} count:{modifyItemCount}"))
            // {
            //     var startRow = int.Parse(modifyRangeStart);
            //     var count = int.Parse(modifyItemCount);
            //     Refresh(startRow, count);
            // }
        }

        private void CLear()
        {
            theList.Clear();
            _data.Clear();
            _itemCount = 0;
        }

        private void Add()
        {
            var newdata =
                new TestFlexChildData(
                    randomTitles[Random.Range(0, randomTitles.Length)], $"Row {theList.RowCount}", Random.Range(0, 256).ToString());
            _data.Add(newdata);
            theList.AddItem(newdata);
            _itemCount++;
        }

        private void Delete()
        {
            if (GUILayout.Button("Delete"))
            {
                var row = _itemCount - 1;
                theList.RemoveAt(row);
                _data = _data.GetRange(0, --_itemCount);
            }
        }

        private void JumpTo(int row)
        {
            theList.ScrollToRow(row);
        }

        private void RefreshAll() => Refresh(0, theList.RowCount - 1);

        private void Refresh(int row, int count = 1)
        {
            if (row >= 0 && row < _data.Count && count > 0 && row + count < _data.Count + 1)
            {
                for (int i = row; i < row + count; i++)
                {
                    _data[i].Title = randomTitles[Random.Range(0, randomTitles.Length)];
                    theList.UpdateItem(row, _data[i]);
                }

                theList.Refresh(row, count);
            }
            else
            {
                Debug.LogError("Input range must be in range of existing data count");
            }
        }

        private void PopulateItem(RecyclingFlexListViewItem item, int rowIndex)
        {
            var child = item as TestFlexChildItem;
            var height = theList.GetHeightAt(rowIndex);
            if (child != null)
            {
                child.ChildData = _data[rowIndex];
                child.RectTransform.sizeDelta = new Vector2(child.RectTransform.sizeDelta.x, height);
            }
        }
    }
}