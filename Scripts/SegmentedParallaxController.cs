using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SegmentedParallax {
  public class SegmentedParallaxController : MonoBehaviour {

    public float scrollSpeed = 1;

    public List<Segment> segments;

    private int _segmentIndex = 0;

    /* Scroll Speed Lerping */

    private float _originalScrollSpeed = 1;

    private float _lerpTime;

    private float _lerpDuration;

    private float _newScrollSpeed = 0f;

    private bool _lerpToNewScrollSpeed = false;

    public void EnableNextSegment(Segment currentSegment) {
      _segmentIndex++;

      if (_segmentIndex < segments.Count) {
        segments[_segmentIndex].transform.localPosition = currentSegment.transform.localPosition +
                                                          new Vector3(0f, currentSegment.GetBoundsSize().y, 0f);

        segments[_segmentIndex].UpdateState(SegmentState.Scrolling);
      }
    }

    public void NextSegment() {
      if (segments[_segmentIndex].segmentType == SegmentType.Static && !segments[_segmentIndex].scrollPast) {
        segments[_segmentIndex].scrollPast = true;

        segments[_segmentIndex].UpdateState(SegmentState.Deactivated);

        EnableNextSegment(segments[_segmentIndex]);
      }
      else {
        segments[_segmentIndex].UpdateState(SegmentState.PendingDeactivation);
      }
    }

    public void UpdateScrollSpeed(float newScrollSpeed) {
      if (_lerpToNewScrollSpeed) {
        _lerpToNewScrollSpeed = false;
      }

      scrollSpeed = newScrollSpeed;
    }

    public void LerpScrollSpeed(float newScrollSpeed, float duration = 1f) {
      _newScrollSpeed = newScrollSpeed;

      _lerpDuration = duration;

      _lerpTime = 0;

      _originalScrollSpeed = scrollSpeed;

      _lerpToNewScrollSpeed = true;
    }

    private void Update() {
      foreach (var segment in segments) {
        if (segment && segment.segmentState != SegmentState.Idle) {
          segment.Scroll(scrollSpeed);
        }
      }

      if (_lerpToNewScrollSpeed) {
        scrollSpeed = Mathf.Lerp(_originalScrollSpeed, _newScrollSpeed, _lerpTime / _lerpDuration);

        if (_lerpTime >= _lerpDuration) {
          _lerpToNewScrollSpeed = false;
        }

        _lerpTime += Time.deltaTime;
      }
    }

    /* Editor Helper Functions */

    public void RefreshSegmentsList() {
      segments = segments.Where((segment) => segment != null).ToList();
    }

    public void CreateEmptySegment() {
      var segmentParent = new GameObject($"Segment {segments.Count + 1}") {
        transform = {
          parent = transform
        }
      };

      var segment = segmentParent.AddComponent<Segment>();

      segment.segmentedParallaxController = this;

      segment.segmentState = segments.Count == 0 ? SegmentState.Scrolling : SegmentState.Idle;

      segment.segmentType = SegmentType.Loopable;

      var leadingSegmentObj = new GameObject("Leading Segment") {
        transform = {
          parent = segment.transform
        }
      };

      leadingSegmentObj.AddComponent<SpriteRenderer>();

      segment.leadingSegment = leadingSegmentObj;

      var repeatableSegmentObj = new GameObject("Repeatable Segment") {
        transform = {
          parent = segment.transform
        }
      };

      repeatableSegmentObj.AddComponent<SpriteRenderer>();

      var repeatableSegment = repeatableSegmentObj.AddComponent<RepeatableSegment>();

      segment.repeatableSegment = repeatableSegment;

      var trailingSegmentObj = new GameObject("Trailing Segment") {
        transform = {
          parent = segment.transform
        }
      };

      trailingSegmentObj.AddComponent<SpriteRenderer>();

      segment.trailingSegment = trailingSegmentObj;

      segments.Add(segment);
    }

  }

#if UNITY_EDITOR
  [CustomEditor(typeof(SegmentedParallaxController))]
  public class SegmentedParallaxControllerEditor : Editor {
    public override void OnInspectorGUI() {
      base.OnInspectorGUI();

      SegmentedParallaxController segmentedParallaxController = (SegmentedParallaxController)target;

      if (GUILayout.Button("Move to Next Segment")) {
        segmentedParallaxController.NextSegment();
      }

      if (GUILayout.Button("New Segment")) {
        ParallaxControllerUtil.CreateEmptySegment(segmentedParallaxController.segments,
          segmentedParallaxController.transform, segmentedParallaxController);
      }

      // keeps segments list up to date (removes any null items)
      segmentedParallaxController.RefreshSegmentsList();
    }
  }
#endif
}