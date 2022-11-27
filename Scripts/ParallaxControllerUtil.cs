using System.Collections.Generic;
using UnityEngine;

namespace SegmentedParallaxPackage {
  public static class ParallaxControllerUtil {
    public static void CreateEmptySegment(List<Segment> segments, Transform parent,
      SegmentedParallaxController segmentedParallaxController) {
      var segmentParent = new GameObject($"Segment {segments.Count + 1}") {
        transform = {
          parent = parent
        }
      };

      var segment = segmentParent.AddComponent<Segment>();

      segment.segmentedParallaxController = segmentedParallaxController;

      segment.segmentState = segments.Count == 0 ? SegmentState.Scrolling : SegmentState.Idle;

      segment.segmentType = SegmentType.Loopable;

      segment.leadingSegment = CreateSegmentGameObject(segment, "Leading Segment");

      CreateRepeatableSegmentGameObject(segment);

      segment.trailingSegment = CreateSegmentGameObject(segment, "Trailing Segment");

      segments.Add(segment);
    }

    public static GameObject CreateSegmentGameObject(Segment segment, string name) {
      return CreateSegmentGameObject(segment, name, null, Vector3.zero);
    }

    public static GameObject CreateSegmentGameObject(Segment segment, string name, Sprite sprite) {
      return CreateSegmentGameObject(segment, name, sprite, Vector3.zero);
    }

    public static GameObject CreateSegmentGameObject(Segment segment, string name, Sprite sprite,
      Vector3 localPositionModifier) {
      var segmentObj = new GameObject(name) {
        transform = {
          parent = segment.transform
        }
      };

      var spriteRenderer = segmentObj.AddComponent<SpriteRenderer>();

      if (sprite) {
        spriteRenderer.sprite = sprite;

        var boundsSize = spriteRenderer.bounds.size;

        segmentObj.transform.localPosition = new Vector3(
          boundsSize.x * localPositionModifier.x,
          boundsSize.y * localPositionModifier.y,
          0f);
      }

      return segmentObj;
    }

    public static void CreateRepeatableSegmentGameObject(Segment segment) {
      var repeatableSegmentObj = new GameObject("Repeatable Segment") {
        transform = {
          parent = segment.transform
        }
      };

      repeatableSegmentObj.AddComponent<SpriteRenderer>();

      var repeatableSegment = repeatableSegmentObj.AddComponent<RepeatableSegment>();

      segment.repeatableSegment = repeatableSegment;
    }
  }
}