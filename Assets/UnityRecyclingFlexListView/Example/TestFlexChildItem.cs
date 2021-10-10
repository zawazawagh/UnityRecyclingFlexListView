using UnityEngine;
using UnityEngine.UI;

namespace UnityRecyclingFlexListView.Example
{
    public class TestFlexChildItem : RecyclingFlexListViewItem
    {
        [SerializeField]
        private Text _leftText;

        [SerializeField]
        private Text _rightText1;

        [SerializeField]
        private Text _rightText2;

        private TestFlexChildData _childData;

        public TestFlexChildData ChildData
        {
            get => _childData;
            set
            {
                _childData = value;
                _leftText.text = _childData.Title;
                _rightText1.text = _childData.Note1;
                _rightText2.text = _childData.Note2;
            }
        }

        public override float CalculateHeight(string content)
        {
            _leftText.text = content;
            _leftText.Rebuild(CanvasUpdate.PreRender);
            return _leftText.preferredHeight;
        }

        public override float GetHeight()
        {
            _leftText.Rebuild(CanvasUpdate.PreRender);
            return _leftText.preferredHeight;
        }
    }
}