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
      switch (segmentedParallaxController.GetDirection()) {
        case SegmentedParallaxController.Direction.Down:
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
          break;
        case SegmentedParallaxController.Direction.Up:
          if (transform.position.y >= _boundsSize.y) {
            if (segmentState == SegmentState.Scrolling || segmentState == SegmentState.PendingDeactivation) {
              Shift(new Vector3(0f, -_boundsSize.y, 0f));

              if (segmentState == SegmentState.PendingDeactivation) {
                PendingDeactivation();
              }
            }
            else if (segmentState == SegmentState.Deactivated) {
              Deactivate();
            }
          }
          break;
      }
    }

    private void ProcessStaticSegment() {
      switch (segmentedParallaxController.GetDirection()) {
        case SegmentedParallaxController.Direction.Down:
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
          break;
        case SegmentedParallaxController.Direction.Up:
          if (transform.localPosition.y > 0f) {
            if (!scrollPast) {
              UpdateState(SegmentState.Idle);

              transform.localPosition = Vector3.zero;
            }
            else if (segmentState != SegmentState.Deactivated) {
              PendingDeactivation();
            }
          }

          if (transform.position.y >= _boundsSize.y) {
            if (segmentState == SegmentState.Deactivated) {
              Deactivate();
            }
          }
          break;
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

        var repeatableSegmentSpriteRenderer = segment.repeatableSegment.GetComponent<SpriteRenderer>();
        
        repeatableSegmentSpriteRenderer.sprite = _sprite;

        RefreshSegment(segment.leadingSegment, new Vector3(0f, _sprite.bounds.size.y, 0f));
          
        RefreshSegment(segment.trailingSegment, new Vector3(0f, -_sprite.bounds.size.y, 0f));
      }

      EditorGUI.BeginChangeCheck();

      _segmentType = (SegmentType)EditorGUILayout.EnumPopup("Segment Type", segment.segmentType);

      if (EditorGUI.EndChangeCheck()) {
        segment.segmentType = _segmentType;

        switch (_segmentType) {
          case SegmentType.Loopable:
            var repeatableSegmentSpriteRenderer = segment.repeatableSegment.GetComponent<SpriteRenderer>();
            
            // Leading Segment
            segment.leadingSegment = ParallaxControllerUtil.CreateSegmentGameObject(segment, "Leading Segment",
              segment.sprite, new Vector3(0f, 1f, 0f));

            UpdateSegmentSpritRenderer(repeatableSegmentSpriteRenderer, segment.leadingSegment);
            
            segment.leadingSegment.transform.SetAsFirstSibling();

            // Trailing Segment
            segment.trailingSegment = ParallaxControllerUtil.CreateSegmentGameObject(segment, "Trailing Segment",
              segment.sprite, new Vector3(0f, -1f, 0f));
            
            UpdateSegmentSpritRenderer(repeatableSegmentSpriteRenderer, segment.trailingSegment);
            
            segment.scrollPast = false;

            break;
          case SegmentType.Static:
            if (segment.leadingSegment)
              DestroyImmediate(segment.leadingSegment);

            segment.leadingSegment = null;

            if (segment.trailingSegment)
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

    private void RefreshSegment(GameObject segmentToRefresh, Vector3 localPosition) {
      if (segmentToRefresh) {
        var segmentSpriteRenderer = segmentToRefresh.GetComponent<SpriteRenderer>();

        if (segmentSpriteRenderer) {
          segmentSpriteRenderer.sprite = _sprite;

          segmentToRefresh.transform.localPosition = localPosition;
        }
      }
    }

    private void UpdateSegmentSpritRenderer(SpriteRenderer baseSegmentSpriteRenderer, GameObject segmentToUpdate) {
      var segmentToUpdateSpriteRenderer = segmentToUpdate.GetComponent<SpriteRenderer>();

      segmentToUpdateSpriteRenderer.sortingLayerName = baseSegmentSpriteRenderer.sortingLayerName;

      segmentToUpdateSpriteRenderer.sortingOrder = baseSegmentSpriteRenderer.sortingOrder;
    }
  }
#endif
}