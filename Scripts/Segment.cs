using UnityEditor;
using UnityEngine;

namespace SegmentedParallax {
  public enum SegmentState {
    Idle,
    Scrolling,
    PendingDeactivation,
    Deactivated
  }

  public enum SegmentType {
    Static,
    Loopable,
  }

  public class Segment : MonoBehaviour {

    public SegmentedParallaxController segmentedParallaxController;

    public RepeatableSegment repeatableSegment;

    public GameObject leadingSegment;

    public GameObject trailingSegment;

    public SegmentState segmentState = SegmentState.Idle;

    [HideInInspector] public SegmentType segmentType = SegmentType.Loopable;

    [HideInInspector] public Sprite sprite;

    [HideInInspector] // only available if SegmentType is set to Static
    public bool scrollPast = false;

    private Vector3 _boundsSize;

    private void Start() {
      UpdateBoundSize();
    }

    private void UpdateBoundSize() {
      _boundsSize = repeatableSegment.GetSpriteRenderer().bounds.size;
    }

    public void UpdateState(SegmentState newState) {
      segmentState = newState;
    }

    // TODO: make this capable of traversing in either direction
    // maybe reach out to the controller to see which direction we're headed based on velocity or scrollSpeed
    public void Scroll(float scrollSpeed) {
      var speed = Time.deltaTime * scrollSpeed;

      transform.position -= new Vector3(0f, speed, 0f);

      switch (segmentType) {
        case SegmentType.Loopable:
          ProcessLoopableSegment();
          break;
        case SegmentType.Static:
          ProcessStaticSegment();
          break;
      }
    }

    private void ProcessLoopableSegment() {
      if (transform.position.y <= -_boundsSize.y) {
        if (segmentState == SegmentState.Scrolling || segmentState == SegmentState.PendingDeactivation) {
          Shift(new Vector3(0f, _boundsSize.y, 0f));

          if (segmentState == SegmentState.PendingDeactivation) {
            PendingDeactivation();
          }
        }
        else if (segmentState == SegmentState.Deactivated) {
          Deactivate();
        }
      }
    }

    private void ProcessStaticSegment() {
      if (transform.localPosition.y < 0f) {
        if (!scrollPast) {
          UpdateState(SegmentState.Idle);

          transform.localPosition = Vector3.zero;
        }
        else if (segmentState != SegmentState.Deactivated) {
          PendingDeactivation();
        }
      }

      if (transform.position.y <= -_boundsSize.y) {
        if (segmentState == SegmentState.Deactivated) {
          Deactivate();
        }
      }
    }

    private void PendingDeactivation() {
      if (leadingSegment) {
        leadingSegment.SetActive(false);
      }

      segmentedParallaxController.EnableNextSegment(this);

      UpdateState(SegmentState.Deactivated);
    }

    private void Deactivate() {
      UpdateState(SegmentState.Idle);

      gameObject.SetActive(false);
    }

    private void Shift(Vector3 amount) {
      transform.position += amount;
    }

    public Vector3 GetBoundsSize() {
      return _boundsSize;
    }

  }

#if UNITY_EDITOR
  [CustomEditor(typeof(Segment))]
  public class SegmentEditor : Editor {
    private Sprite _sprite;

    private SegmentType _segmentType;

    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      Segment segment = (Segment)target;

      EditorGUI.BeginChangeCheck();

      _sprite = EditorGUILayout.ObjectField("Sprite", segment.sprite, typeof(Sprite)) as Sprite;

      if (EditorGUI.EndChangeCheck()) {
        segment.sprite = _sprite;

        foreach (var componentsInChild in segment.gameObject.GetComponentsInChildren<SpriteRenderer>()) {
          if (componentsInChild) {
            componentsInChild.sprite = _sprite;
          }
        }
      }

      EditorGUI.BeginChangeCheck();

      _segmentType = (SegmentType)EditorGUILayout.EnumPopup("Segment Type", segment.segmentType);

      if (EditorGUI.EndChangeCheck()) {
        segment.segmentType = _segmentType;

        switch (_segmentType) {
          case SegmentType.Loopable:
            segment.leadingSegment = ParallaxControllerUtil.CreateSegmentGameObject(segment, "Leading Segment",
              segment.sprite, new Vector3(0f, 1f, 0f));

            segment.leadingSegment.transform.SetAsFirstSibling();

            segment.trailingSegment = ParallaxControllerUtil.CreateSegmentGameObject(segment, "Trailing Segment",
              segment.sprite, new Vector3(0f, -1f, 0f));

            segment.scrollPast = false;

            break;
          case SegmentType.Static:
            DestroyImmediate(segment.leadingSegment);

            segment.leadingSegment = null;

            DestroyImmediate(segment.trailingSegment);

            segment.trailingSegment = null;

            break;
        }
      }

      if (_segmentType == SegmentType.Static) {
        var scrollPast = EditorGUILayout.Toggle("Scroll Past", segment.scrollPast);

        segment.scrollPast = scrollPast;
      }

      /* TODO:
       * - create menu item that sets up basic Segmented Parallax Controller
       */

      EditorUtility.SetDirty(target);
    }
  }
#endif
}