using UnityEngine;

namespace UnityRecyclingFlexListView
{
    /// <summary>
    ///     You should subclass this to provide fast access to any data you need to populate
    ///     this item on demand.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class RecyclingFlexListViewItem : MonoBehaviour
    {
        private RecyclingFlexListView _parentList;

        private int _currentRow;

        private RectTransform _rectTransform;

        public RecyclingFlexListView ParentList => _parentList;

        public int CurrentRow => _currentRow;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }

                return _rectTransform;
            }
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void NotifyCurrentAssignment(RecyclingFlexListView v, int row)
        {
            _parentList = v;
            _currentRow = row;
        }

        public abstract float CalculateHeight(string content);

        public abstract float GetHeight();
    }
}