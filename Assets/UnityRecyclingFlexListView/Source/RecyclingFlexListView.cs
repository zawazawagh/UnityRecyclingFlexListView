using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityRecyclingFlexListView
{
    [RequireComponent(typeof(ScrollRect))]
    public class RecyclingFlexListView : MonoBehaviour
    {
        public delegate void ItemDelegate(RecyclingFlexListViewItem item, int rowIndex);

        protected const int RowsAboveBelow = 1;

        [SerializeField]
        private RecyclingFlexListViewItem childPrefab;

        public float RowPadding = 15f;
        public ItemDelegate ItemCallback;

        protected ScrollRect ScrollRect;
        protected RecyclingFlexListViewItem[] ChildItems;
        protected int ChildBufferStart;
        protected int SourceDataRowStart;
        protected bool IgnoreScrollChange;
        protected float PreviousBuildHeight;

        private int _rowCount;
        private readonly Dictionary<int, float> _itemHeightDict = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _itemYTopPosDict = new Dictionary<int, float>();
        private int _currentFirstVisibleRowIndex;
        private float _currentContentHeight;
        private RecyclingFlexListViewItem _heightCalculator;

        public float VerticalNormalizedPosition
        {
            get => ScrollRect.verticalNormalizedPosition;
            set => ScrollRect.verticalNormalizedPosition = value;
        }

        public int RowCount
        {
            get => _rowCount;
            private set
            {
                if (_rowCount != value)
                {
                    _rowCount = value;
                    IgnoreScrollChange = true;
                    UpdateContentHeight();
                    IgnoreScrollChange = false;
                    ReorganiseContent(true);
                }
            }
        }

        public float ItemMinHeight => childPrefab.RectTransform.rect.height;

        protected virtual void Awake()
        {
            ScrollRect = GetComponent<ScrollRect>();
            _heightCalculator = Instantiate(childPrefab, ScrollRect.content.transform)
                .GetComponent<RecyclingFlexListViewItem>();
            _heightCalculator.name = "heightCalculator";
            _heightCalculator.gameObject.SetActive(false);
        }

        protected virtual void OnEnable()
        {
            ScrollRect.onValueChanged.AddListener(OnScrollChanged);
            IgnoreScrollChange = false;
        }

        protected virtual void OnDisable()
        {
            ScrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }

        public void AddItems(List<ChildData> data)
        {
        }

        public void AddItem(ChildData data)
        {
            var height = Mathf.Max(_heightCalculator.CalculateHeight(data.FlexibleItem()),
                childPrefab.RectTransform.rect.height);
            _itemHeightDict[_rowCount] = height;
            _itemYTopPosDict[_rowCount] = _currentContentHeight;
            _currentContentHeight += height + RowPadding;
            RowCount++;
        }

        public void RemoveAt(int row)
        {
            _currentContentHeight -= _itemHeightDict[row];
            _itemHeightDict.Remove(row);
            _itemYTopPosDict.Remove(row);
            RowCount--;
        }

        // public void UpdateItemHeight(int row, float height)
        // {
        //     var diff = height - itemHeightDict[row];
        //     Debug.Log($"row{row} diff{diff}");
        //     itemHeightDict[row] = height;
        //     for (int i = row + 1; i < rowCount; i++)
        //     {
        //         itemYMinPosDict[i] += diff;
        //     }
        // }

        public float GetHeightAt(int row) => _itemHeightDict[row];

        public virtual void Refresh()
        {
            ReorganiseContent(true);
        }

        public void UpdateItem(int row, ChildData data)
        {
            UpdateHeightIfNeeded(row, data);
        }

        public virtual void Refresh(int rowStart, int count)
        {
            var sourceDataLimit = SourceDataRowStart + ChildItems.Length;
            for (var i = 0; i < count; ++i)
            {
                var row = rowStart + i;
                if (row < SourceDataRowStart || row >= sourceDataLimit)
                    continue;

                var bufIdx = WrapChildIndex(ChildBufferStart + row - SourceDataRowStart);
                if (ChildItems[bufIdx] != null) UpdateChild(ChildItems[bufIdx], row);
            }

            ReorganiseContent(true);
        }

        public virtual void Refresh(RecyclingFlexListViewItem item)
        {
            for (var i = 0; i < ChildItems.Length; ++i)
            {
                var idx = WrapChildIndex(ChildBufferStart + i);
                if (ChildItems[idx] != null && ChildItems[idx] == item)
                {
                    UpdateChild(ChildItems[i], SourceDataRowStart + i);
                    break;
                }
            }
        }

        public virtual void Clear()
        {
            _currentContentHeight = 0f;
            _itemHeightDict.Clear();
            _itemYTopPosDict.Clear();
            RowCount = 0;
        }

        public virtual void ScrollToRow(int row)
        {
            ScrollRect.verticalNormalizedPosition = GetRowScrollPosition(row);
        }

        public float GetRowScrollPosition(int row)
        {
            row = Mathf.Clamp(row, 0, _rowCount - 1);
            var rowCentre = _itemYTopPosDict[row] + _itemHeightDict[row] * 0.5f;

            var vpHeight = ViewportHeight();
            var halfVpHeight = vpHeight * 0.5f;
            var vpTop = Mathf.Max(0, rowCentre - halfVpHeight);
            var vpBottom = vpTop + vpHeight;
            var contentHeight = ScrollRect.content.sizeDelta.y;
            if (vpBottom > contentHeight) // if content is shorter than vp always stop at 0
                vpTop = Mathf.Max(0, vpTop - (vpBottom - contentHeight));

            return Mathf.InverseLerp(contentHeight - vpHeight, 0, vpTop);
        }

        public RecyclingFlexListViewItem GetRowItem(int row)
        {
            if (ChildItems != null &&
                row >= SourceDataRowStart && row < SourceDataRowStart + ChildItems.Length && // within window 
                row < _rowCount) // within overall range

                return ChildItems[WrapChildIndex(ChildBufferStart + row - SourceDataRowStart)];

            return null;
        }

        protected virtual bool CheckChildItems()
        {
            var buildHeight = ViewportHeight();
            var rebuild = ChildItems == null || buildHeight > PreviousBuildHeight;
            if (rebuild)
            {
                var childCount = Mathf.RoundToInt(0.5f + buildHeight / ItemMinHeight);
                childCount += RowsAboveBelow * 2; // X before, X after

                if (ChildItems == null)
                    ChildItems = new RecyclingFlexListViewItem[childCount];
                else if (childCount > ChildItems.Length) Array.Resize(ref ChildItems, childCount);

                for (var i = 0; i < ChildItems.Length; ++i)
                {
                    if (ChildItems[i] == null) ChildItems[i] = Instantiate(childPrefab);
                    ChildItems[i].RectTransform.SetParent(ScrollRect.content, false);
                    ChildItems[i].gameObject.SetActive(false);
                }

                PreviousBuildHeight = buildHeight;
            }

            return rebuild;
        }

        protected virtual void OnScrollChanged(Vector2 normalisedPos)
        {
            if (!IgnoreScrollChange) ReorganiseContent(false);
        }

        protected virtual void ReorganiseContent(bool clearContents)
        {
            if (clearContents)
            {
                ScrollRect.StopMovement();
                ScrollRect.verticalNormalizedPosition = 1; // 1 == top
            }

            var childrenChanged = CheckChildItems();
            var populateAll = childrenChanged || clearContents;

            var ymin = ScrollRect.content.localPosition.y; //スクロールビューのうち、表示されているエリアの最小値（上辺のy座標）

            //int firstVisibleIndex = (int)(ymin / RowHeight());
            //同じサイズではないので、単純な割り算ができない
            //見えてる部分のy座標がどのアイテムの高さに相当するかを知っていないといけない
            //firstvisibleindex変更のロジック
            //TODO: refactor
            if (_rowCount == 0)
            {
                _currentFirstVisibleRowIndex = 0;
            }
            else if (_rowCount > _currentFirstVisibleRowIndex + 1)
            {
                if (ymin > _itemYTopPosDict[_currentFirstVisibleRowIndex + 1] +
                    _itemHeightDict[_currentFirstVisibleRowIndex + 1])
                    for (var i = _currentFirstVisibleRowIndex;
                        _currentFirstVisibleRowIndex + 1 < _itemYTopPosDict.Count && i < _rowCount;
                        i++)
                        if (ymin > _itemYTopPosDict[_currentFirstVisibleRowIndex + 1] +
                                _itemHeightDict[_currentFirstVisibleRowIndex + 1])
                            //firstvisibleindexのアイテムが完全に見えなくなったら、数字をひとつ上げる
                            _currentFirstVisibleRowIndex++;
                        else
                            break;
                else if (ymin < _itemYTopPosDict[_currentFirstVisibleRowIndex + 1])
                    for (var i = _currentFirstVisibleRowIndex; i > 0; i--)
                        if (ymin < _itemYTopPosDict[_currentFirstVisibleRowIndex + 1])
                            //firstvisibleindexのアイテムが完全に見えたら、数字をひとつ下げる
                            _currentFirstVisibleRowIndex = Mathf.Max(0, _currentFirstVisibleRowIndex - 1);
                        else
                            break;
            }

            var newRowStart = _currentFirstVisibleRowIndex - RowsAboveBelow;

            var diff = newRowStart - SourceDataRowStart;
            if (populateAll || Mathf.Abs(diff) >= ChildItems.Length)
            {
                SourceDataRowStart = newRowStart;
                ChildBufferStart = 0;
                var rowIdx = newRowStart;
                foreach (var item in ChildItems) UpdateChild(item, rowIdx++);
            }
            else if (diff != 0)
            {
                var newBufferStart = (ChildBufferStart + diff) % ChildItems.Length;

                if (diff < 0)
                {
                    for (var i = 1; i <= -diff; ++i)
                    {
                        var bufi = WrapChildIndex(ChildBufferStart - i);
                        var rowIdx = SourceDataRowStart - i;
                        UpdateChild(ChildItems[bufi], rowIdx);
                    }
                }
                else
                {
                    var prevLastBufIdx = ChildBufferStart + ChildItems.Length - 1;
                    var prevLastRowIdx = SourceDataRowStart + ChildItems.Length - 1;
                    for (var i = 1; i <= diff; ++i)
                    {
                        var bufi = WrapChildIndex(prevLastBufIdx + i);
                        var rowIdx = prevLastRowIdx + i;
                        UpdateChild(ChildItems[bufi], rowIdx);
                    }
                }

                SourceDataRowStart = newRowStart;
                ChildBufferStart = newBufferStart;
            }
        }

        //diffが発生した、つまり新たに表示するitemが発生したときに呼ばれる
        protected virtual void UpdateChild(RecyclingFlexListViewItem child, int rowIdx)
        {
            if (rowIdx < 0 || rowIdx >= _rowCount)
            {
                child.gameObject.SetActive(false);
            }
            else
            {
                if (ItemCallback == null)
                {
                    Debug.Log("RecyclingListView is missing an ItemCallback, cannot function", this);
                    return;
                }

                ItemCallback(child, rowIdx);

                var childRect = childPrefab.RectTransform.rect;
                var pivot = childPrefab.RectTransform.pivot;
                //itemの上辺のy座標はdictから取得
                var ytoppos = _itemYTopPosDict[rowIdx];
                var ypos = ytoppos + (1f - pivot.y) * childRect.height;
                var xpos = 0 + pivot.x * childRect.width;
                child.RectTransform.anchoredPosition = new Vector2(xpos, -ypos);
                child.NotifyCurrentAssignment(this, rowIdx);

                child.gameObject.SetActive(true);
            }
        }

        protected virtual void UpdateContentHeight()
        {
            //アイテム個別の高さを持たせる
            var height = _itemHeightDict.Values.Sum() + RowPadding * (_rowCount - 1);
            var sz = ScrollRect.content.sizeDelta;
            ScrollRect.content.sizeDelta = new Vector2(sz.x, height);
        }
    
        private int WrapChildIndex(int idx)
        {
            while (idx < 0)
                idx += ChildItems.Length;

            return idx % ChildItems.Length;
        }

        private float ViewportHeight()
        {
            return ScrollRect.viewport.rect.height;
        }

        private void UpdateHeightIfNeeded(int row, ChildData data)
        {
            //全要素の高さをチェック
            //変更があればdictを更新
            //変更があったdataのみ高さチェックを行う
            var currentHeight = _itemHeightDict[row];
            var calculatedHeight = Mathf.Max(ChildItems[row].CalculateHeight(data.FlexibleItem()),
                childPrefab.RectTransform.rect.height);
            if (Math.Abs(currentHeight - calculatedHeight) > 0.01f)
            {
                ChildItems[row].RectTransform.sizeDelta =
                    new Vector2(ChildItems[row].RectTransform.sizeDelta.x, calculatedHeight);
                _itemHeightDict[row] = calculatedHeight;
                var diff = calculatedHeight - currentHeight;
                for (var i = row + 1; i < RowCount; i++) _itemYTopPosDict[i] += diff;

                _currentContentHeight += diff;
                UpdateContentHeight();
            }
        }
    }
}