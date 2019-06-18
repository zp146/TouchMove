using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Card
{
    public enum EState
    {
        eNone = 0,
        ePreSelect,//预选择
        eSelect,//选择
        ePreCancel,//预取消
        eCancel,//取消
        eMax
    }
    public enum eMoveState
    {
        eNone = 0,
        eLeft,
        eRight,
        eMax
    }
    public Transform transform = null;
    public bool IsSelect = false;
    public string Name = string.Empty;
    public eMoveState MoveState = eMoveState.eNone;//是否右移
    public Image image = null;
    public Card(Transform transform, string name, Image image = null)
    {
        this.transform = transform;
        this.Name = name;
        this.image = image;
    }
    public void ReSetState()
    {
        MoveState = eMoveState.eNone;
    }
}
public class TouchTest : MonoBehaviour
{

    private GameObject GoElement = null;
    private Vector2 VecBegin = Vector2.zero, VecEnd = Vector2.zero, VecMove = Vector2.zero, VecLastMove = Vector2.zero, CardSize = Vector2.zero, CardHalfSize = Vector2.zero, MoveDirector = Vector2.zero;
    private LinkedList<Card> HandCards = new LinkedList<Card>();
    private LinkedListNode<Card> SelNode = null;
    private List<Card> lstSelCards = new List<Card>();
    private GameObject goButton;
    public float OffsetHeight = 20.0f;
    #region 线段与矩形的相关判断
    Vector2 leftDown(Rect rect)
    {
        return new Vector2(rect.xMin, rect.yMin);
    }
    Vector2 leftUp(Rect rect)
    {
        return new Vector2(rect.xMin, rect.yMax);
    }
    Vector2 RigtDown(Rect rect)
    {
        return new Vector2(rect.xMax, rect.yMin);
    }
    Vector2 RightUp(Rect rect)
    {
        return new Vector2(rect.xMax, rect.yMax);
    }
    // 线是否在矩形内
    bool LineInRect(Vector2 lineStart, Vector2 lineEnd, Rect rect)
    {
        return rect.Contains(lineStart) || rect.Contains(lineEnd);
    }

    // 线与矩形是否相交
    bool LineIntersectRect(Vector2 lineStart, Vector2 lineEnd, Rect rect)
    {
        if (LineIntersectLine(lineStart, lineEnd, leftDown(rect), leftUp(rect)))
            return true;
        if (LineIntersectLine(lineStart, lineEnd, leftUp(rect), RightUp(rect)))
            return true;
        if (LineIntersectLine(lineStart, lineEnd, RightUp(rect), RigtDown(rect)))
            return true;
        if (LineIntersectLine(lineStart, lineEnd, RigtDown(rect), leftDown(rect)))
            return true;

        return false;
    }
    // 线与线是否相交
    bool LineIntersectLine(Vector2 l1Start, Vector2 l1End, Vector2 l2Start, Vector2 l2End)
    {
        return QuickReject(l1Start, l1End, l2Start, l2End) && Straddle(l1Start, l1End, l2Start, l2End);
    }
    // 跨立实验
    bool Straddle(Vector2 l1Start, Vector2 l1End, Vector2 l2Start, Vector2 l2End)
    {
        float l1x1 = l1Start.x;
        float l1x2 = l1End.x;
        float l1y1 = l1Start.y;
        float l1y2 = l1End.y;
        float l2x1 = l2Start.x;
        float l2x2 = l2End.x;
        float l2y1 = l2Start.y;
        float l2y2 = l2End.y;

        if ((((l1x1 - l2x1) * (l2y2 - l2y1) - (l1y1 - l2y1) * (l2x2 - l2x1)) *
             ((l1x2 - l2x1) * (l2y2 - l2y1) - (l1y2 - l2y1) * (l2x2 - l2x1))) > 0 ||
            (((l2x1 - l1x1) * (l1y2 - l1y1) - (l2y1 - l1y1) * (l1x2 - l1x1)) *
             ((l2x2 - l1x1) * (l1y2 - l1y1) - (l2y2 - l1y1) * (l1x2 - l1x1))) > 0)
        {
            return false;
        }

        return true;
    }

    // 快速排序。  true=通过， false=不通过
    bool QuickReject(Vector2 l1Start, Vector2 l1End, Vector2 l2Start, Vector2 l2End)
    {
        float l1xMax = Mathf.Max(l1Start.x, l1End.x);
        float l1yMax = Mathf.Max(l1Start.y, l1End.y);
        float l1xMin = Mathf.Min(l1Start.x, l1End.x);
        float l1yMin = Mathf.Min(l1Start.y, l1End.y);

        float l2xMax = Mathf.Max(l2Start.x, l2End.x);
        float l2yMax = Mathf.Max(l2Start.y, l2End.y);
        float l2xMin = Mathf.Min(l2Start.x, l2End.x);
        float l2yMin = Mathf.Min(l2Start.y, l2End.y);

        if (l1xMax < l2xMin || l1yMax < l2yMin || l2xMax < l1xMin || l2yMax < l1yMin)
            return false;

        return true;
    }
    #endregion
    // Use this for initialization
    void Start()
    {
        if (GoElement == null)
        {
            GoElement = GameObject.Find("Canvas/CardPool/Element");
        }
        if (goButton == null)
        {
            goButton = GameObject.Find("Canvas/Button");
            if (goButton != null)
                goButton.GetComponent<Button>().onClick.AddListener(ChickButton);
        }

        GoElement.name = "0";
        RectTransform recttran = GoElement.transform.GetComponent<RectTransform>();
        float fSpacing = 0.0f;
        if (GoElement.transform.parent != null)
            fSpacing = GoElement.transform.parent.GetComponent<HorizontalLayoutGroup>().spacing;
        CardSize = new Vector2(recttran.rect.width, recttran.rect.height);
        CardHalfSize = new Vector2(recttran.rect.width + fSpacing, recttran.rect.height);
        Card card1 = new Card(GoElement.transform, GoElement.name, GoElement.transform.GetComponent<Image>());
        HandCards.AddFirst(card1);
        //SelNode.Next
        //创建多张牌
        for (int n = 1; n != 17; n++)
        {
            GameObject go = UnityEngine.Object.Instantiate(GoElement, GoElement.transform.parent.transform);
            go.name = n.ToString();
            go.transform.SetParent(GoElement.transform.parent.transform);
            go.SetActive(true);
            Card card = new Card(go.transform, go.name, go.transform.GetComponent<Image>());
            HandCards.AddLast(card);
        }
    }

    void ChickButton()
    {
        if (lstSelCards.Count > 0)
        {
            for (int n = 0; n != lstSelCards.Count; ++n)
            {
                GameObject.DestroyObject(lstSelCards[n].transform.gameObject);
                HandCards.Remove(lstSelCards[n]);
            }
            lstSelCards.Clear();
        }
    }
    /// <summary>
    /// 回折线处理<预选中的，取消预选中>
    /// </summary>
    void ProcessMovedByLoopLine()
    {
        if (SelNode != null)
        {
            Vector2 vecdic = VecMove - VecBegin;
            if (MoveDirector.x > 0)
            {
                //→→→→→→→→→→→→→→→→→→→→→
                if (SelNode.Next != null && SelNode.Next.Value != null)
                {
                    if (SelNode.Next.Value.MoveState == Card.eMoveState.eRight)
                    {
                        SelNode.Next.Value.MoveState = Card.eMoveState.eNone;
                        SelNode.Next.Value.image.color = Color.white;
                    }
                }
            }
            else
            {
                //←←←←←←←←←←←←←←←←←←←←←
                if (SelNode.Previous != null && SelNode.Previous.Value != null)
                {
                    if (SelNode.Previous.Value.MoveState == Card.eMoveState.eLeft)
                    {
                        SelNode.Previous.Value.MoveState = Card.eMoveState.eNone;
                        SelNode.Previous.Value.image.color = Color.white;
                    }
                }
            }
        }
    }
    /// <summary>
    /// 滑动中的处理
    /// </summary>
    void ProcessMoved()
    {
        //Debug.Log($"Mouse VecBegin:{VecBegin}|VecMove:{VecMove}");
        MoveDirector = VecMove - VecBegin;
        ProcessMoved_Previous();
        //回折线处理---如果有的情况下
        ProcessMovedByLoopLine();
        ProcessMoved_Suffix(VecBegin, VecMove);
    }

    /// <summary>
    /// 滑动中的处理前缀
    /// </summary>
    void ProcessMoved_Previous()
    {
        Card curCard = GetCurCard(VecMove);
        if (curCard != null)
        {
            if (MoveDirector.x > 0)
            {
                if (curCard.MoveState == Card.eMoveState.eNone)
                    curCard.MoveState = Card.eMoveState.eRight;
            }
            else
            {
                if (curCard.MoveState == Card.eMoveState.eNone)
                    curCard.MoveState = Card.eMoveState.eLeft;
            }
            curCard.image.color = Color.gray;
            SelNode = HandCards.Find(curCard);
        }
    }
    /// <summary>
    /// 滑动中的处理后缀
    /// 如果有漏掉的没有选中的Card,再次遍历一次，选中那些漏掉的情况
    /// </summary>
    void ProcessMoved_Suffix(Vector2 vecBegin, Vector2 vecEnd)
    {
        if (!vecBegin.Equals(Vector2.zero) && !vecEnd.Equals(Vector2.zero))
        {
            if (HandCards.Count > 0)
            {
                foreach (var cur in HandCards)
                {
                    Vector2 vecCard = RectTransformUtility.WorldToScreenPoint(Camera.main, cur.transform.position);
                    vecCard -= CardSize / 2;
                    Vector2 tmpVec = Vector2.zero;
                    if (cur.Name.Equals(HandCards.Last.Value.Name))
                    {
                        tmpVec = CardSize;
                    }
                    else
                    {
                        tmpVec = CardHalfSize;
                    }
                    Rect rect = new Rect(vecCard, tmpVec);
                    if (LineIntersectRect(vecBegin, vecEnd, rect) || LineInRect(vecBegin, vecEnd, rect))
                    {
                        cur.image.color = Color.gray;
                    }
                }
            }
        }
    }
    /// <summary>
    /// 滑动结束最后一帧与移动中前一帧的处理
    /// </summary>
    void ProcessEndFrame()
    {
        //Debug.Log($"Mouse VecBegin:{VecBegin}|VecEnd:{VecEnd}");
        Vector2 dir = VecEnd - VecMove;
        if (SelNode != null)
        {
            if (dir.x > 0)
                if (SelNode.Next != null)
                    if (IsSel(VecBegin, VecEnd, SelNode.Next.Value))
                        SelNode = SelNode.Next;
                    else
                if (SelNode.Previous != null)
                        if (IsSel(VecBegin, VecEnd, SelNode.Previous.Value))
                            SelNode = SelNode.Previous;
            if (SelNode != null)
            {
                //Debug.LogWarning($"PreMoveCard:{SelNode.Value.Name}");
                SelNode.Value.image.color = Color.gray;
            }
        }
        SelNode = null;
        if (HandCards.Count > 0)
        {
            foreach (var cur in HandCards)
            {
                cur?.ReSetState();
            }
        }
    }
    /// <summary>
    /// 滑动结束处理
    /// </summary>
    void PorcessMoveCard()
    {
        //lstSelCards.Clear();
        if (!VecBegin.Equals(Vector2.zero) && !VecEnd.Equals(Vector2.zero))
        {
            if (HandCards.Count > 0)
            {
                int nHandCardsCountBySelect = 0;
                foreach (var cur in HandCards)
                {
                    //恢复moved里面nextValue 可能会造成的isSel--Color.gray
                    cur.image.color = Color.white;
                    //这里要复原预移动的Pos,然后再次获取Vector2
                    Vector2 vecCard = RectTransformUtility.WorldToScreenPoint(Camera.main, cur.transform.position);
                    vecCard -= CardSize / 2;
                    Vector2 tmpVec = Vector2.zero;
                    if (cur.Name.Equals(HandCards.Last.Value.Name))
                    {
                        tmpVec = CardSize;
                    }
                    else
                    {
                        tmpVec = CardHalfSize;
                    }
                    Rect rect = new Rect(vecCard, tmpVec);
                    if (LineIntersectRect(VecBegin, VecEnd, rect) || LineInRect(VecBegin, VecEnd, rect))
                    {
                        lstSelCards.Add(cur);
                        nHandCardsCountBySelect++;
                        cur.IsSelect = !cur.IsSelect;
                        cur.image.color = Color.white;
                        if (cur.IsSelect)
                            cur.transform.localPosition += OffsetHeight * Vector3.up;
                        else
                            cur.transform.localPosition -= OffsetHeight * Vector3.up;
                    }
                }
                if (nHandCardsCountBySelect <= 0)
                {
                    foreach (var cur in HandCards)
                    {
                        if (cur.IsSelect)
                        {
                            cur.transform.localPosition -= OffsetHeight * Vector3.up;
                            cur.IsSelect = false;
                            cur.image.color = Color.white;
                        }
                    }
                }
            }
            VecBegin = Vector2.zero;
            VecEnd = Vector2.zero;
        }
    }

    void Process()
    {
        ProcessEndFrame();
        PorcessMoveCard();
    }
    /// <summary>
    /// 得到当前Pos的Card
    /// </summary>
    /// <param name="vecPos"></param>
    /// <returns></returns>
    Card GetCurCard(Vector2 vecPos)
    {
        if (HandCards.Count > 0)
        {
            foreach (var cur in HandCards)
            {
                if (IsSel(vecPos, vecPos + 0.01f * Vector2.right, cur))
                    return cur;
            }
        }
        return null;
    }
    bool IsSel(Vector2 vecbegin, Vector2 vecend, Card card)
    {
        Vector2 vecCard = RectTransformUtility.WorldToScreenPoint(Camera.main, card.transform.position);
        vecCard -= CardSize / 2;
        Vector2 tmpVec = Vector2.zero;
        Card LastCard = HandCards.Last.Value;
        if (card.Name.Equals(LastCard.Name))
        {
            tmpVec = CardSize;
        }
        else
        {
            tmpVec = CardHalfSize;
        }
        Rect rect = new Rect(vecCard, tmpVec);
        //Debug.Log($"InSel-Rect:{rect}");
        if (LineIntersectRect(vecbegin, vecend, rect) || LineInRect(vecbegin, vecend, rect))
            return true;
        else
            return false;
    }
    // Update is called once per frame
    void Update()
    {
        Touch[] touches = Input.touches;
        if (touches.Length > 0)
        {
            switch (touches[0].phase)
            {
                case TouchPhase.Began:
                    {
                        VecBegin = touches[0].position;
                    }
                    break;
                case TouchPhase.Moved:
                    {
                        VecMove = touches[0].position;
                        ProcessMoved();
                    }
                    break;
                case TouchPhase.Stationary:
                    {
                    }
                    break;
                case TouchPhase.Ended:
                    {
                        VecEnd = touches[0].position;
                        Process();
                    }
                    break;
                case TouchPhase.Canceled:
                    {
                        VecBegin = Vector3.zero;
                        VecEnd = Vector3.zero;
                    }
                    break;
            }
        }
    }
#if UNITY_EDITOR
    void OnGUI()   // 滑动方法02  
    {
        if (Event.current != null)
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    {
                        VecBegin = Event.current.mousePosition;
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        VecMove = Event.current.mousePosition;
                        ProcessMoved();
                    }
                    break;

                case EventType.MouseUp:
                    {
                        VecEnd = Event.current.mousePosition;
                        Process();
                    }
                    break;
            }
    }
#endif

}
