using UnityEngine.UI;

namespace UnityRecyclingFlexListView
{
    public class TestFlexChildItem : RecyclingFlexListViewItem {
        public Text leftText;
        public Text rightText1;
        public Text rightText2;

        private TestFlexChildData _childData;
        public TestFlexChildData ChildData {
            get { return _childData; }
            set {
                _childData = value;
                leftText.text = _childData.Title;
                rightText1.text = _childData.Note1;
                rightText2.text = _childData.Note2;
            }
        }

        public override float CalculateHeight(string content)
        {
            leftText.text = content;
            leftText.Rebuild(CanvasUpdate.PreRender);
            return leftText.preferredHeight;
        }

        public override float GetHeight()
        {
            leftText.Rebuild(CanvasUpdate.PreRender);
            return leftText.preferredHeight;
        }
    }
}